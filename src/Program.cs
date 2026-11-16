using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MiBudsApp.Bluetooth;
using MiBudsApp.UI;

namespace MiBudsApp
{
    public static class AppLog
    {
        public static void Log(string msg)
        {
            try
            {
                string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
                File.AppendAllText(p, "[" + DateTime.Now.ToString("HH:mm:ss.fff") + "] " + msg + Environment.NewLine);
            }
            catch { }
        }
    }

    internal static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ChangeWindowMessageFilter(uint message, uint dwFlag);

        private const int SW_RESTORE = 9;
        private const uint MSGFLT_ADD = 1;
        private const string MUTEX_NAME = "MiEarbuddy_SingleInstance";
        private const string WINDOW_TITLE = "Mi Earbuddy";
        public const string SHOW_MSG_NAME = "MI_EARBUDDY_ACTIVATE_WINDOW";

        public static uint WM_ACTIVATE_APP_MSG = 0;

        [STAThread]
        private static void Main(string[] args)
        {
            AppLog.Log("Main started with args: " + string.Join(" ", args));

            // DPI Awareness para ecrãs de alta densidade (4K / Portáteis 125%-150%)
            try
            {
                if (Environment.OSVersion.Version.Major >= 6)
                {
                    SetProcessDPIAware();
                }
            }
            catch (Exception ex) { AppLog.Log("DPI awareness ex: " + ex.Message); }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                AppLog.Log("UnhandledException: " + e.ExceptionObject);
            };

            Application.ThreadException += (s, e) =>
            {
                AppLog.Log("ThreadException: " + e.Exception);
            };

            WM_ACTIVATE_APP_MSG = RegisterWindowMessage(SHOW_MSG_NAME);
            try
            {
                ChangeWindowMessageFilter(WM_ACTIVATE_APP_MSG, MSGFLT_ADD);
            }
            catch { }

            bool startInTray = false;
            foreach (string arg in args)
            {
                if (arg.Equals("-tray", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("/tray", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("--tray", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("-minimized", StringComparison.OrdinalIgnoreCase))
                {
                    startInTray = true;
                    break;
                }
            }

            bool createdNew;
            using (var mutex = new Mutex(true, MUTEX_NAME, out createdNew))
            {
                AppLog.Log("Mutex acquired: " + createdNew);
                if (!createdNew)
                {
                    AppLog.Log("App already running. Activating existing instance window...");
                    IntPtr existingHwnd = FindWindow(null, WINDOW_TITLE);
                    AppLog.Log("FindWindow returned: " + existingHwnd);
                    if (existingHwnd != IntPtr.Zero)
                    {
                        ShowWindow(existingHwnd, SW_RESTORE);
                        SetForegroundWindow(existingHwnd);
                        if (WM_ACTIVATE_APP_MSG != 0)
                        {
                            PostMessage(existingHwnd, WM_ACTIVATE_APP_MSG, IntPtr.Zero, IntPtr.Zero);
                        }
                    }
                    if (WM_ACTIVATE_APP_MSG != 0)
                    {
                        PostMessage((IntPtr)0xFFFF, WM_ACTIVATE_APP_MSG, IntPtr.Zero, IntPtr.Zero);
                    }
                    return;
                }

                AppLog.Log("Instantiating MiBudsAppContext (startInTray=" + startInTray + ")...");
                try
                {
                    var context = new MiBudsAppContext(startInTray);
                    AppLog.Log("Calling Application.Run(context)...");
                    Application.Run(context);
                    AppLog.Log("Application.Run(context) exited!");
                }
                catch (Exception ex)
                {
                    AppLog.Log("FATAL in Main: " + ex.ToString());
                }
            }
        }
    }

    internal class MiBudsAppContext : ApplicationContext
    {
        private readonly XiaomiMmaClient client;
        private readonly MainForm form;
        private readonly TrayManager tray;

        public MiBudsAppContext(bool startInTray)
        {
            AppLog.Log("MiBudsAppContext constructor started");
            client = new XiaomiMmaClient();
            AppLog.Log("XiaomiMmaClient created");
            form = new MainForm(client);
            AppLog.Log("MainForm created");
            // Não definir this.MainForm para manter o loop do ApplicationContext ativo mesmo quando a janela está oculta (modo -tray)
            tray = new TrayManager(client, form);
            AppLog.Log("TrayManager created");
            form.SetTrayManager(tray);

            // Iniciar o cliente Bluetooth assíncrono em background
            client.Start();
            AppLog.Log("client.Start() called");

            if (!startInTray)
            {
                AppLog.Log("Showing form");
                form.Show();
                form.WindowState = FormWindowState.Normal;
                form.TopMost = true;
                form.BringToFront();
                form.Activate();
                form.TopMost = false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            AppLog.Log("MiBudsAppContext Dispose called (disposing=" + disposing + ")");
            if (disposing)
            {
                try { if (client != null) client.Stop(); } catch { }
                try { if (tray != null) tray.Dispose(); } catch { }
                try { if (form != null) form.Dispose(); } catch { }
            }
            base.Dispose(disposing);
        }
    }
}
