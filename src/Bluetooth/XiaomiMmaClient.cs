using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32;

namespace MiBudsApp.Bluetooth
{
    public enum NoiseMode
    {
        Off = 0,
        NoiseCancellation = 1,
        Transparency = 2
    }

    public class BatteryState
    {
        public int Left { get; set; }
        public bool LeftCharging { get; set; }
        public int Right { get; set; }
        public bool RightCharging { get; set; }
        public int Case { get; set; }
        public bool CaseCharging { get; set; }

        public BatteryState()
        {
            Left = -1;
            Right = -1;
            Case = -1;
        }

        public bool HasData
        {
            get { return Left >= 0 || Right >= 0 || Case >= 0; }
        }
    }

    public class XiaomiMmaClient
    {
        [StructLayout(LayoutKind.Explicit, Size = 30)]
        private struct SOCKADDR_BTH_PACKED
        {
            [FieldOffset(0)] public ushort addressFamily;
            [FieldOffset(2)] public ulong btAddr;
            [FieldOffset(10)] public Guid serviceClassId;
            [FieldOffset(26)] public uint port;
        }

        [DllImport("ws2_32.dll", SetLastError = true)]
        private static extern IntPtr socket(int af, int type, int protocol);
        [DllImport("ws2_32.dll", SetLastError = true)]
        private static extern int connect(IntPtr s, ref SOCKADDR_BTH_PACKED name, int namelen);
        [DllImport("ws2_32.dll", SetLastError = true)]
        private static extern int recv(IntPtr s, byte[] buf, int len, int flags);
        [DllImport("ws2_32.dll", SetLastError = true)]
        private static extern int send(IntPtr s, byte[] buf, int len, int flags);
        [DllImport("ws2_32.dll", SetLastError = true)]
        private static extern int closesocket(IntPtr s);
        [DllImport("ws2_32.dll", SetLastError = true)]
        private static extern int ioctlsocket(IntPtr s, int cmd, ref uint argp);
        [DllImport("ws2_32.dll", SetLastError = true)]
        private static extern int WSAStartup(ushort wVersionRequested, IntPtr lpWSAData);

        private static readonly Guid FASTPAIR_UUID = new Guid("DF21FE2C-2515-4FDB-8886-F12C4D67927C");
        private static readonly Guid XIAOAI_UUID = new Guid("0000FD2D-0000-1000-8000-00805F9B34FB");

        public event Action<string> Connected;
        public event Action Disconnected;
        public event Action<BatteryState> BatteryUpdated;
        public event Action<NoiseMode> NoiseModeChanged;
        public event Action<string> StatusChanged;

        private void FireConnected(string name)
        {
            Action<string> h = Connected;
            if (h != null) h(name);
        }

        private void FireDisconnected()
        {
            Action h = Disconnected;
            if (h != null) h();
        }

        private void FireBatteryUpdated(BatteryState state)
        {
            Action<BatteryState> h = BatteryUpdated;
            if (h != null) h(state);
        }

        private void FireNoiseModeChanged(NoiseMode mode)
        {
            Action<NoiseMode> h = NoiseModeChanged;
            if (h != null) h(mode);
        }

        private void FireStatusChanged(string status)
        {
            Action<string> h = StatusChanged;
            if (h != null) h(status);
        }

        private Thread workerThread;
        private volatile bool shouldRun = false;
        private IntPtr activeSocket = IntPtr.Zero;
        private int sequenceNumber = 1;
        private readonly object sendLock = new object();
        private DateTime lastUserModeTime = DateTime.MinValue;
        private NoiseMode pendingUserMode = NoiseMode.Off;
        private NoiseMode previousNoiseMode = NoiseMode.Off;
        private DateTime lastNoiseTransitionTime = DateTime.MinValue;

        public BatteryState Battery { get; private set; }
        public NoiseMode CurrentMode { get; private set; }
        public bool IsConnected { get; private set; }
        public string DeviceName { get; private set; }
        public ulong DeviceAddress { get; private set; }

        private static void UpdateBudBattery(byte raw, ref int level, ref bool charging, ref bool changed)
        {
            if (raw == 0xFF) return;
            bool chg = (raw & 0x80) != 0;
            int val = raw & 0x7F;

            if (val <= 100 && val >= 0)
            {
                if (level != val || charging != chg)
                {
                    level = val;
                    charging = chg;
                    changed = true;
                }
            }
            else if (val == 0x7F) // Sentinela Bluetooth: no estojo / sem percentagem direta
            {
                if (charging != chg)
                {
                    charging = chg;
                    changed = true;
                }
            }
        }

        public XiaomiMmaClient()
        {
            Battery = new BatteryState();
            CurrentMode = NoiseMode.Off;
            IsConnected = false;
            DeviceName = "Redmi Buds 5 Pro";
            DeviceAddress = 0x4873CBFDA87D;

            IntPtr pWSA = Marshal.AllocHGlobal(1024);
            WSAStartup(0x0202, pWSA);
        }

        public void Start()
        {
            if (shouldRun) return;
            shouldRun = true;
            workerThread = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = "XiaomiMmaWorker"
            };
            workerThread.Start();
        }

        public void Stop()
        {
            shouldRun = false;
            CloseActiveSocket();
            if (workerThread != null && workerThread.IsAlive)
            {
                workerThread.Join(1000);
            }
        }

        private void CloseActiveSocket()
        {
            lock (sendLock)
            {
                if (activeSocket != IntPtr.Zero)
                {
                    try { closesocket(activeSocket); } catch { }
                    activeSocket = IntPtr.Zero;
                }
            }
        }

        private void DetectDeviceAddress()
        {
            try
            {
                // 1. Prioridade: dispositivos PnP ativos em Enum\BTHENUM
                using (var root = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\BTHENUM"))
                {
                    if (root != null)
                    {
                        foreach (string subKeyName in root.GetSubKeyNames())
                        {
                            if (subKeyName.StartsWith("Dev_", StringComparison.OrdinalIgnoreCase))
                            {
                                string hexMac = subKeyName.Substring(4);
                                using (var devKey = root.OpenSubKey(subKeyName))
                                {
                                    if (devKey != null)
                                    {
                                        foreach (string childName in devKey.GetSubKeyNames())
                                        {
                                            using (var childKey = devKey.OpenSubKey(childName))
                                            {
                                                if (childKey != null)
                                                {
                                                    string nameStr = childKey.GetValue("FriendlyName") as string;
                                                    if (!string.IsNullOrEmpty(nameStr) &&
                                                        (nameStr.IndexOf("Redmi Buds", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                         nameStr.IndexOf("Xiaomi", StringComparison.OrdinalIgnoreCase) >= 0))
                                                    {
                                                        ulong parsedMac;
                                                        if (ulong.TryParse(hexMac, System.Globalization.NumberStyles.HexNumber, null, out parsedMac))
                                                        {
                                                            DeviceAddress = parsedMac;
                                                            DeviceName = nameStr;
                                                            return;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // 2. Fallback: BTHPORT\Parameters\Devices
                using (var root = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\BTHPORT\Parameters\Devices"))
                {
                    if (root != null)
                    {
                        foreach (string subKeyName in root.GetSubKeyNames())
                        {
                            using (var devKey = root.OpenSubKey(subKeyName))
                            {
                                if (devKey != null)
                                {
                                    object nameVal = devKey.GetValue("Name");
                                    string devName = "";
                                    byte[] bytes = nameVal as byte[];
                                    if (bytes != null)
                                    {
                                        int len = 0;
                                        while (len < bytes.Length && bytes[len] != 0) len++;
                                        devName = Encoding.UTF8.GetString(bytes, 0, len);
                                    }
                                    else
                                    {
                                        string str = nameVal as string;
                                        if (str != null)
                                        {
                                            devName = str;
                                        }
                                    }

                                    if (!string.IsNullOrEmpty(devName) &&
                                        (devName.IndexOf("Redmi Buds", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         devName.IndexOf("Xiaomi", StringComparison.OrdinalIgnoreCase) >= 0))
                                    {
                                        ulong parsedMac;
                                        if (ulong.TryParse(subKeyName, System.Globalization.NumberStyles.HexNumber, null, out parsedMac))
                                        {
                                            DeviceAddress = parsedMac;
                                            DeviceName = devName;
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void WorkerLoop()
        {
            AppLog.Log("XiaomiMmaClient WorkerLoop starting");
            try
            {
                while (shouldRun)
                {
                    DetectDeviceAddress();
                    AppLog.Log("Detected device: " + DeviceName + " (0x" + DeviceAddress.ToString("X") + ")");
                    FireStatusChanged("A procurar auscultadores...");

                    // 1. Tentar ler bateria da caixa e earbuds pelo FastPair primeiro
                    try
                    {
                        FetchFastPairBattery();
                    }
                    catch (Exception ex)
                    {
                        AppLog.Log("FetchFastPairBattery error: " + ex.Message);
                    }

                    // 2. Conectar ao serviço XiaoAi para controlo total de ANC e notificações
                    bool connected = false;
                    try
                    {
                        connected = ConnectXiaoAi();
                    }
                    catch (Exception ex)
                    {
                        AppLog.Log("ConnectXiaoAi error: " + ex.Message);
                    }

                    AppLog.Log("ConnectXiaoAi returned: " + connected);
                    if (!connected)
                    {
                        IsConnected = false;
                        Battery = new BatteryState();
                        FireBatteryUpdated(Battery);
                        FireDisconnected();
                        FireStatusChanged("Aguardando ligação dos auscultadores...");

                        for (int i = 0; i < 30 && shouldRun; i++)
                        {
                            Thread.Sleep(100);
                        }
                        continue;
                    }

                    // 3. Loop de escuta de notificações
                    AppLog.Log("Starting RunXiaoAiSession...");
                    try
                    {
                        RunXiaoAiSession();
                    }
                    catch (Exception ex)
                    {
                        AppLog.Log("RunXiaoAiSession error: " + ex.Message);
                    }
                    AppLog.Log("RunXiaoAiSession finished");

                    IsConnected = false;
                    Battery = new BatteryState();
                    FireBatteryUpdated(Battery);
                    FireDisconnected();
                    FireStatusChanged("Ligação perdida. A reconectar...");
                    Thread.Sleep(2000);
                }
            }
            catch (Exception ex)
            {
                AppLog.Log("WorkerLoop FATAL: " + ex);
            }
            AppLog.Log("WorkerLoop exiting (shouldRun=" + shouldRun + ")");
        }

        private void FetchFastPairBattery()
        {
            IntPtr s = socket(32, 1, 3);
            if (s == IntPtr.Zero || s.ToInt64() == -1) return;

            SOCKADDR_BTH_PACKED addr = new SOCKADDR_BTH_PACKED
            {
                addressFamily = 32,
                btAddr = DeviceAddress,
                serviceClassId = FASTPAIR_UUID,
                port = 0
            };

            if (connect(s, ref addr, 30) == 0)
            {
                uint nb = 1;
                ioctlsocket(s, -2147195266, ref nb);
                byte[] buf = new byte[1024];

                for (int i = 0; i < 15 && shouldRun; i++)
                {
                    int r = recv(s, buf, buf.Length, 0);
                    if (r > 0)
                    {
                        for (int j = 0; j < r - 6; j++)
                        {
                            if (buf[j] == 0x03 && buf[j + 1] == 0x03 && buf[j + 2] == 0x00 && buf[j + 3] == 0x03)
                            {
                                byte bL = buf[j + 4];
                                byte bR = buf[j + 5];
                                byte bC = buf[j + 6];

                                bool changed = false;
                                int l = Battery.Left; bool lC = Battery.LeftCharging;
                                UpdateBudBattery(bL, ref l, ref lC, ref changed);
                                Battery.Left = l; Battery.LeftCharging = lC;

                                int rVal = Battery.Right; bool rC = Battery.RightCharging;
                                UpdateBudBattery(bR, ref rVal, ref rC, ref changed);
                                Battery.Right = rVal; Battery.RightCharging = rC;

                                int cVal = Battery.Case; bool cC = Battery.CaseCharging;
                                UpdateBudBattery(bC, ref cVal, ref cC, ref changed);
                                Battery.Case = cVal; Battery.CaseCharging = cC;

                                if (Battery.HasData)
                                {
                                    FireBatteryUpdated(Battery);
                                }
                                break;
                            }
                        }
                        if (Battery.Case >= 0) break;
                    }
                    Thread.Sleep(50);
                }
                closesocket(s);
            }
            else
            {
                closesocket(s);
            }
        }

        private bool ConnectXiaoAi()
        {
            CloseActiveSocket();

            IntPtr s = socket(32, 1, 3);
            if (s == IntPtr.Zero || s.ToInt64() == -1) return false;

            SOCKADDR_BTH_PACKED addr = new SOCKADDR_BTH_PACKED
            {
                addressFamily = 32,
                btAddr = DeviceAddress,
                serviceClassId = XIAOAI_UUID,
                port = 0
            };

            int res = connect(s, ref addr, 30);
            if (res != 0)
            {
                closesocket(s);
                return false;
            }

            uint nb = 1;
            ioctlsocket(s, -2147195266, ref nb);

            lock (sendLock)
            {
                activeSocket = s;
            }

            return true;
        }

        private void RunXiaoAiSession()
        {
            byte[] buf = new byte[2048];
            bool authed = false;
            DateTime lastBatteryPoll = DateTime.MinValue;

            // Iniciar autenticação E21
            byte[] myChallenge = new byte[16];
            new Random().NextBytes(myChallenge);
            byte[] authParams = new byte[17];
            authParams[0] = 0x01;
            Array.Copy(myChallenge, 0, authParams, 1, 16);

            SendRaw(BuildCommand(0x50, 1, 0, authParams));
            Thread.Sleep(150);
            SendRaw(BuildCommand(0x51, 1, 1, new byte[] { 0x01, 0x00 }));

            while (shouldRun && activeSocket != IntPtr.Zero)
            {
                int r = recv(activeSocket, buf, buf.Length, 0);
                if (r > 0)
                {
                    ProcessIncomingFrames(buf, r, ref authed);
                }
                else if (r == 0)
                {
                    // Socket fechado pelo dispositivo
                    break;
                }

                if (authed)
                {
                    if ((DateTime.Now - lastBatteryPoll).TotalSeconds >= 4)
                    {
                        lastBatteryPoll = DateTime.Now;
                        QueryBatteryInternal();
                    }
                }

                Thread.Sleep(40);
            }
        }

        private void ProcessIncomingFrames(byte[] buf, int len, ref bool authed)
        {
            try
            {
                int pos = 0;
                while (pos + 8 <= len)
                {
                    if (buf[pos] == 0xFE && buf[pos + 1] == 0xDC && buf[pos + 2] == 0xBA)
                    {
                        int flags = buf[pos + 3];
                        int opcode = buf[pos + 4];
                        int bodyLen = (buf[pos + 5] << 8) | buf[pos + 6];

                        if (pos + 8 + bodyLen - 1 <= len && buf[pos + 7 + bodyLen] == 0xEF)
                        {
                            bool isCmd = (flags & 0x80) != 0;
                            int serial = isCmd ? buf[pos + 7] : buf[pos + 8];
                            AppLog.Log(string.Format("Received frame: opcode=0x{0:X2}, isCmd={1}, bodyLen={2}", opcode, isCmd, bodyLen));

                            if (isCmd && opcode == 0x50) // Desafio enviado pelos auscultadores
                            {
                                if (bodyLen >= 18 && buf[pos + 8] == 0x01)
                                {
                                    byte[] devCh = new byte[16];
                                    Array.Copy(buf, pos + 9, devCh, 0, 16);
                                    byte[] transform = XiaomiAuth.E21(devCh);
                                    byte[] respData = new byte[17];
                                    respData[0] = 0x01;
                                    Array.Copy(transform, 0, respData, 1, 16);
                                    SendRaw(BuildResponse(0x50, 1, serial, 0, respData));
                                    AppLog.Log("Sent E21 challenge response");
                                }
                            }
                            else if (isCmd && opcode == 0x51) // Fim de autenticação
                            {
                                SendRaw(BuildResponse(0x51, 1, serial, 0, new byte[] { 0x01 }));
                                AppLog.Log("Sent auth complete confirmation");
                                if (!authed)
                                {
                                    authed = true;
                                    IsConnected = true;
                                    FireConnected(DeviceName);
                                    FireStatusChanged("Conectado");
                                    if (Battery != null && Battery.HasData)
                                    {
                                        FireBatteryUpdated(Battery);
                                    }

                                    // Consultar estado inicial
                                    QueryBatteryInternal();
                                    QueryNoiseInternal();
                                }
                            }
                            else if (opcode == 0x02) // Resposta de bateria / info
                            {
                                if (bodyLen >= 2)
                                {
                                    ParseTargetInfo(buf, pos + 9, bodyLen - 2);
                                }
                            }
                            else if (opcode == 0xF3 || opcode == 0xF4) // Configuração / Notificação de ruído
                            {
                                int overhead = isCmd ? 1 : 2;
                                if (bodyLen >= overhead)
                                {
                                    ParseConfigNotification(buf, pos + (isCmd ? 8 : 9), bodyLen - overhead);
                                }
                            }

                            pos += 8 + bodyLen;
                            continue;
                        }
                    }
                    pos++;
                }
            }
            catch (Exception ex)
            {
                AppLog.Log("ProcessIncomingFrames error: " + ex);
            }
        }

        private void ParseTargetInfo(byte[] buf, int offset, int length)
        {
            try
            {
                int p = offset;
                int end = offset + length;
                while (p + 2 <= end)
                {
                    int recordLen = buf[p];
                    if (recordLen < 1 || p + recordLen + 1 > end) break;
                    int type = buf[p + 1];

                    if (type == 7 && recordLen == 4) // Bateria L, R, Case
                    {
                        byte bL = buf[p + 2];
                        byte bR = buf[p + 3];
                        byte bC = buf[p + 4];

                        bool changed = false;
                        int l = Battery.Left; bool lC = Battery.LeftCharging;
                        UpdateBudBattery(bL, ref l, ref lC, ref changed);
                        Battery.Left = l; Battery.LeftCharging = lC;

                        int rVal = Battery.Right; bool rC = Battery.RightCharging;
                        UpdateBudBattery(bR, ref rVal, ref rC, ref changed);
                        Battery.Right = rVal; Battery.RightCharging = rC;

                        int cVal = Battery.Case; bool cC = Battery.CaseCharging;
                        UpdateBudBattery(bC, ref cVal, ref cC, ref changed);
                        Battery.Case = cVal; Battery.CaseCharging = cC;

                        AppLog.Log(string.Format("Parsed Battery: L={0}% (chg:{1}), R={2}% (chg:{3}), Case={4}% (chg:{5})",
                            Battery.Left, Battery.LeftCharging, Battery.Right, Battery.RightCharging, Battery.Case, Battery.CaseCharging));
                        FireBatteryUpdated(Battery);
                    }
                    else if (type == 0 && recordLen > 1) // Nome do dispositivo
                    {
                        string name = Encoding.UTF8.GetString(buf, p + 2, recordLen - 1);
                        if (!string.IsNullOrEmpty(name))
                        {
                            DeviceName = name;
                        }
                    }

                    p += recordLen + 1;
                }
            }
            catch { }
        }

        private void ParseConfigNotification(byte[] buf, int offset, int length)
        {
            try
            {
                int p = offset;
                int end = offset + length;
                while (p + 3 <= end)
                {
                    int recLen = buf[p];
                    if (recLen < 2 || p + recLen + 1 > end) break;
                    int type = (buf[p + 1] << 8) | buf[p + 2];

                    if (type == 11 && recLen >= 4) // Configuração de Ruído (00 0B)
                    {
                        int mode = buf[p + 3];
                        if (mode >= 0 && mode <= 2)
                        {
                            NoiseMode incomingMode = (NoiseMode)mode;
                            // 1. Se o utilizador alternou de modo pela aplicação recentemente (< 4s), ignorar notificações desatualizadas
                            if ((DateTime.UtcNow - lastUserModeTime).TotalMilliseconds < 4000)
                            {
                                if (incomingMode != pendingUserMode)
                                {
                                    AppLog.Log(string.Format("Ignoring stale noise notification: received={0}, pending={1}", incomingMode, pendingUserMode));
                                    p += recLen + 1;
                                    continue;
                                }
                            }

                            // 2. Se a troca foi feita nos próprios auscultadores, o auricular secundário ou o DSP envia frequentemente
                            // pacotes transitórios residuais com o modo anterior. Ignorar qualquer regressão para o modo anterior dentro de 2.5s.
                            if ((DateTime.UtcNow - lastNoiseTransitionTime).TotalMilliseconds < 2500)
                            {
                                if (incomingMode == previousNoiseMode)
                                {
                                    AppLog.Log(string.Format("Ignoring transient noise bounce: received={0}, previous={1}", incomingMode, previousNoiseMode));
                                    p += recLen + 1;
                                    continue;
                                }
                            }

                            if (CurrentMode != incomingMode)
                            {
                                previousNoiseMode = CurrentMode;
                                CurrentMode = incomingMode;
                                lastNoiseTransitionTime = DateTime.UtcNow;
                                AppLog.Log(string.Format("Parsed Noise Mode: {0} -> {1}", previousNoiseMode, CurrentMode));
                                FireNoiseModeChanged(CurrentMode);
                            }
                        }
                    }

                    p += recLen + 1;
                }
            }
            catch { }
        }

        private void QueryBatteryInternal()
        {
            int sn = Interlocked.Increment(ref sequenceNumber) & 0xFF;
            SendRaw(BuildCommand(0x02, 4, sn, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }));
        }

        private void QueryNoiseInternal()
        {
            // Não consultar se o modo foi alterado recentemente (esperar que o hardware transicione sem interferências)
            if ((DateTime.UtcNow - lastUserModeTime).TotalMilliseconds < 4000 ||
                (DateTime.UtcNow - lastNoiseTransitionTime).TotalMilliseconds < 3000)
            {
                return;
            }

            int sn = Interlocked.Increment(ref sequenceNumber) & 0xFF;
            SendRaw(BuildCommand(0xF3, 4, sn, new byte[] { 0, 11 }));
        }

        private int isRefreshing = 0;
        private DateTime lastFastRefreshTime = DateTime.MinValue;

        public void Refresh()
        {
            if (Interlocked.CompareExchange(ref isRefreshing, 1, 0) != 0)
            {
                return;
            }

            try
            {
                if (IsConnected)
                {
                    QueryBatteryInternal();
                    QueryNoiseInternal();
                }
                else
                {
                    FetchFastPairBattery();
                }
            }
            catch (Exception ex)
            {
                AppLog.Log("Refresh error: " + ex.Message);
            }
            finally
            {
                Interlocked.Exchange(ref isRefreshing, 0);
            }
        }

        public void RequestFastRefresh()
        {
            if ((DateTime.UtcNow - lastFastRefreshTime).TotalMilliseconds < 2000)
            {
                return;
            }
            lastFastRefreshTime = DateTime.UtcNow;

            ThreadPool.QueueUserWorkItem(state =>
            {
                Refresh();
            });
        }

        public void SetNoiseMode(NoiseMode mode)
        {
            // Debounce: ignorar se o modo selecionado já for o atual e tiver sido aplicado recentemente
            if (CurrentMode == mode && (DateTime.UtcNow - lastUserModeTime).TotalMilliseconds < 1500)
            {
                return;
            }

            previousNoiseMode = CurrentMode;
            CurrentMode = mode;
            pendingUserMode = mode;
            lastUserModeTime = DateTime.UtcNow;
            lastNoiseTransitionTime = DateTime.UtcNow;
            FireNoiseModeChanged(CurrentMode);

            int sn = Interlocked.Increment(ref sequenceNumber) & 0xFF;

            // Comando canónico Xiaomi MMA para Redmi Buds 5 Pro:
            // Opcode 0xF2 (XM_OPCODE_SET_DEVICE_CONFIG), App 4, TLV: len=4, type=0x000B (11 = Controlo de Ruído), mode, level=0
            byte[] tlv = new byte[] { 0x04, 0x00, 0x0B, (byte)mode, 0x00 };
            SendRaw(BuildCommand(0xF2, 4, sn, tlv));

            AppLog.Log(string.Format("SetNoiseMode applied (single command): mode={0} ({1})", (int)mode, mode));
        }

        private void SendRaw(byte[] data)
        {
            lock (sendLock)
            {
                if (activeSocket != IntPtr.Zero)
                {
                    try
                    {
                        send(activeSocket, data, data.Length, 0);
                    }
                    catch { }
                }
            }
        }

        private static byte[] BuildFrame(int flags, int opcode, byte[] body)
        {
            byte[] f = new byte[8 + body.Length];
            f[0] = 0xFE; f[1] = 0xDC; f[2] = 0xBA;
            f[3] = (byte)flags;
            f[4] = (byte)opcode;
            f[5] = (byte)(body.Length >> 8);
            f[6] = (byte)body.Length;
            Array.Copy(body, 0, f, 7, body.Length);
            f[f.Length - 1] = 0xEF;
            return f;
        }

        private static byte[] BuildCommand(int opcode, int app, int serial, byte[] prms)
        {
            byte[] body = new byte[prms.Length + 1];
            body[0] = (byte)serial;
            Array.Copy(prms, 0, body, 1, prms.Length);
            return BuildFrame(0xC0 | (app & 7), opcode, body);
        }

        private static byte[] BuildResponse(int opcode, int app, int serial, int status, byte[] data)
        {
            byte[] body = new byte[data.Length + 2];
            body[0] = (byte)status;
            body[1] = (byte)serial;
            Array.Copy(data, 0, body, 2, data.Length);
            return BuildFrame(app & 7, opcode, body);
        }
    }
}
