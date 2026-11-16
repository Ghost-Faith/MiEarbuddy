using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using MiBudsApp.Bluetooth;
using MiBudsApp.Common;

namespace MiBudsApp.UI
{
    public class TrayManager : IDisposable
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private readonly NotifyIcon notifyIcon;
        private readonly ContextMenuStrip contextMenu;
        private readonly XiaomiMmaClient client;
        private readonly Form mainForm;

        private ToolStripMenuItem itemDeviceTitle;
        private TrayBatteryMenuItem itemBatteryInfo;
        private ToolStripMenuItem lblModoHeader;
        private ToolStripMenuItem itemModeAnc;
        private ToolStripMenuItem itemModeOff;
        private ToolStripMenuItem itemModeTrans;
        private ToolStripMenuItem itemOpen;
        private ToolStripMenuItem itemAutoStart;
        private ToolStripMenuItem itemToggleLanguage;
        private ToolStripMenuItem itemExit;

        public ToolStripMenuItem ItemDeviceTitle { get { return itemDeviceTitle; } }
        public ToolStripMenuItem ItemBatteryInfo { get { return itemBatteryInfo; } }
        public ToolStripMenuItem LblModoHeader { get { return lblModoHeader; } }
        public ToolStripMenuItem ItemAutoStart { get { return itemAutoStart; } }
        public ToolStripMenuItem ItemExit { get { return itemExit; } }

        private const string RUN_REG_KEY = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string APP_NAME = "MiEarbuddy";
        private const string OLD_APP_NAME = "XiaomiEarBuddy";
        private const string OLD_APP_NAME_LEGACY = "RedmiBuds5ProControl";

        public TrayManager(XiaomiMmaClient client, Form mainForm)
        {
            this.client = client;
            this.mainForm = mainForm;

            contextMenu = new ContextMenuStrip
            {
                Renderer = new ModernDarkMenuRenderer(this, client),
                BackColor = Color.FromArgb(22, 23, 28),
                ShowImageMargin = false,
                ShowCheckMargin = false,
                DropShadowEnabled = true,
                MinimumSize = new Size(275, 0)
            };

            BuildContextMenu();

            contextMenu.Opening += (s, e) =>
            {
                // NÃO executar Refresh síncrono para garantir abertura 100% instantânea sem congelar a UI
                UpdateTooltip();
                UpdateNoiseModeChecked();
                UpdateAutoStartItem();
                UpdateBatteryItem();
                UpdateDeviceTitle();

                try
                {
                    // Cantos arredondados nativos do Windows 11
                    int round = 2; // DWMWCP_ROUND
                    DwmSetWindowAttribute(contextMenu.Handle, 33, ref round, sizeof(int));
                }
                catch { }
            };

            notifyIcon = new NotifyIcon
            {
                ContextMenuStrip = contextMenu,
                Icon = CreateEarbudIcon(),
                Text = Localization.AppName,
                Visible = true
            };

            DateTime lastHoverRefresh = DateTime.MinValue;
            notifyIcon.MouseMove += (s, e) =>
            {
                // Atualização rápida de bateria quando o utilizador passa o rato (máx 1x a cada 3s)
                if ((DateTime.UtcNow - lastHoverRefresh).TotalSeconds >= 3)
                {
                    lastHoverRefresh = DateTime.UtcNow;
                    client.RequestFastRefresh();
                }
            };

            notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ToggleWindow();
                }
            };

            notifyIcon.DoubleClick += (s, e) =>
            {
                ShowWindow();
            };

            // Eventos do cliente Bluetooth
            client.Connected += OnDeviceConnected;
            client.Disconnected += OnDeviceDisconnected;
            client.BatteryUpdated += OnBatteryUpdated;
            client.NoiseModeChanged += OnNoiseModeChanged;

            // Evento do sistema de localização
            Localization.LanguageChanged += OnLanguageChanged;

            UpdateDeviceTitle();
            UpdateBatteryItem();
            UpdateAutoStartItem();
            UpdateTooltip();
        }

        private void BuildContextMenu()
        {
            contextMenu.Items.Clear();

            // 1. Título do dispositivo: Centralizado e destacado (10.5pt Bold)
            itemDeviceTitle = new ToolStripMenuItem(Localization.DeviceTitlePlaceholder)
            {
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 245, 247),
                Enabled = false,
                Padding = new Padding(10, 8, 10, 4)
            };
            contextMenu.Items.Add(itemDeviceTitle);

            // 2. Indicador Prominente de Bateria: Ordem Esq (Left) | Caixa (Case) | Dir (Right), altura fixa 46px
            itemBatteryInfo = new TrayBatteryMenuItem(client);
            contextMenu.Items.Add(itemBatteryInfo);

            contextMenu.Items.Add(new ToolStripSeparator());

            // 3. Cabeçalho de Modos de Ruído
            lblModoHeader = new ToolStripMenuItem(Localization.TrayNoiseHeader)
            {
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(120, 120, 128),
                Enabled = false,
                Padding = new Padding(10, 4, 10, 2)
            };
            contextMenu.Items.Add(lblModoHeader);

            // 4. Modos de Ruído
            itemModeTrans = new ToolStripMenuItem(Localization.TransparencyMenuText, null, (s, e) => client.SetNoiseMode(NoiseMode.Transparency))
            {
                Font = new Font("Segoe UI", 9f),
                Padding = new Padding(10, 3, 10, 3)
            };
            itemModeAnc = new ToolStripMenuItem(Localization.NoiseCancellationMenuText, null, (s, e) => client.SetNoiseMode(NoiseMode.NoiseCancellation))
            {
                Font = new Font("Segoe UI", 9f),
                Padding = new Padding(10, 3, 10, 3)
            };
            itemModeOff = new ToolStripMenuItem(Localization.OffMenuText, null, (s, e) => client.SetNoiseMode(NoiseMode.Off))
            {
                Font = new Font("Segoe UI", 9f),
                Padding = new Padding(10, 3, 10, 3)
            };

            contextMenu.Items.Add(itemModeTrans);
            contextMenu.Items.Add(itemModeAnc);
            contextMenu.Items.Add(itemModeOff);

            contextMenu.Items.Add(new ToolStripSeparator());

            // 5. Abrir Painel Principal
            itemOpen = new ToolStripMenuItem(Localization.TrayOpenPanel, null, (s, e) => ShowWindow())
            {
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Padding = new Padding(10, 3, 10, 3)
            };
            contextMenu.Items.Add(itemOpen);

            // 6. Iniciar com o Windows (com badge explícito [ ON ] / [ OFF ])
            itemAutoStart = new ToolStripMenuItem("", null, ToggleAutoStart)
            {
                Font = new Font("Segoe UI", 9f),
                Padding = new Padding(10, 3, 10, 3)
            };
            contextMenu.Items.Add(itemAutoStart);

            // 7. Alternar Idioma (PT / EN)
            itemToggleLanguage = new ToolStripMenuItem("", null, (s, e) => Localization.ToggleLanguage())
            {
                Font = new Font("Segoe UI", 9f),
                Padding = new Padding(10, 3, 10, 3)
            };
            contextMenu.Items.Add(itemToggleLanguage);

            contextMenu.Items.Add(new ToolStripSeparator());

            // 8. Sair da Aplicação
            itemExit = new ToolStripMenuItem(Localization.TrayExit, null, (s, e) =>
            {
                notifyIcon.Visible = false;
                client.Stop();
                Application.Exit();
            })
            {
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(255, 69, 58),
                Padding = new Padding(10, 3, 10, 5)
            };
            contextMenu.Items.Add(itemExit);

            UpdateAllLocalizedTexts();
        }

        public void UpdateAllLocalizedTexts()
        {
            if (lblModoHeader != null) lblModoHeader.Text = Localization.TrayNoiseHeader;
            if (itemModeTrans != null) itemModeTrans.Text = Localization.TransparencyMenuText;
            if (itemModeAnc != null) itemModeAnc.Text = Localization.NoiseCancellationMenuText;
            if (itemModeOff != null) itemModeOff.Text = Localization.OffMenuText;
            if (itemOpen != null) itemOpen.Text = Localization.TrayOpenPanel;
            if (itemExit != null) itemExit.Text = Localization.TrayExit;

            if (itemToggleLanguage != null)
            {
                itemToggleLanguage.Text = Localization.IsPortuguese
                    ? "🌐 Idioma: Português (PT)"
                    : "🌐 Language: English (EN)";
            }

            UpdateAutoStartItem();
            UpdateDeviceTitle();
            UpdateBatteryItem();
            UpdateTooltip();
            UpdateNoiseModeChecked();
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            SafeInvoke(new Action(() =>
            {
                UpdateAllLocalizedTexts();
            }));
        }

        private void ToggleAutoStart(object sender, EventArgs e)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RUN_REG_KEY, true))
                {
                    if (key != null)
                    {
                        bool currentlyOn = IsAutoStartEnabled();
                        if (currentlyOn)
                        {
                            key.DeleteValue(APP_NAME, false);
                            key.DeleteValue(OLD_APP_NAME, false);
                            key.DeleteValue(OLD_APP_NAME_LEGACY, false);
                        }
                        else
                        {
                            string exePath = Application.ExecutablePath;
                            key.SetValue(APP_NAME, "\"" + exePath + "\" -tray");
                            key.DeleteValue(OLD_APP_NAME, false);
                            key.DeleteValue(OLD_APP_NAME_LEGACY, false);
                        }
                    }
                }
            }
            catch { }

            UpdateAutoStartItem();
        }

        public bool IsAutoStartEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RUN_REG_KEY, false))
                {
                    if (key != null)
                    {
                        return key.GetValue(APP_NAME) != null || key.GetValue(OLD_APP_NAME) != null || key.GetValue(OLD_APP_NAME_LEGACY) != null;
                    }
                }
            }
            catch { }
            return false;
        }

        private void UpdateAutoStartItem()
        {
            if (itemAutoStart == null) return;
            bool isAuto = IsAutoStartEnabled();
            itemAutoStart.Checked = isAuto;
            itemAutoStart.Text = string.Format("🚀 {0}   {1}", Localization.TrayStartWithWindows, isAuto ? "ON" : "OFF");
        }

        private void SafeInvoke(Action action)
        {
            try
            {
                if (mainForm != null && mainForm.IsHandleCreated && !mainForm.IsDisposed)
                {
                    mainForm.BeginInvoke(action);
                }
                else
                {
                    action();
                }
            }
            catch { }
        }

        private void OnDeviceConnected(string name)
        {
            SafeInvoke(new Action(() =>
            {
                UpdateDeviceTitle();
                UpdateBatteryItem();
                UpdateTooltip();
                UpdateNoiseModeChecked();
            }));
        }

        private void OnDeviceDisconnected()
        {
            SafeInvoke(new Action(() =>
            {
                UpdateDeviceTitle();
                UpdateBatteryItem();
                UpdateTooltip();
            }));
        }

        private void OnBatteryUpdated(BatteryState state)
        {
            SafeInvoke(new Action(() =>
            {
                UpdateBatteryItem();
                UpdateTooltip();
            }));
        }

        private void OnNoiseModeChanged(NoiseMode mode)
        {
            SafeInvoke(new Action(() =>
            {
                UpdateNoiseModeChecked();
                UpdateTooltip();
            }));
        }

        private void UpdateDeviceTitle()
        {
            if (itemDeviceTitle == null) return;

            string name = client.DeviceName;
            if (string.IsNullOrEmpty(name))
            {
                name = Localization.DeviceTitlePlaceholder;
            }

            if (client.IsConnected)
            {
                itemDeviceTitle.Text = name;
            }
            else
            {
                itemDeviceTitle.Text = name + " " + Localization.StatusDisconnectedSuffix;
            }
        }

        private void UpdateBatteryItem()
        {
            if (itemBatteryInfo == null) return;

            BatteryState state = client.Battery;
            if (state == null || !state.HasData)
            {
                itemBatteryInfo.Text = Localization.TrayBatteryReadingText;
            }
            else
            {
                // Ordem OBRIGATÓRIA: Esq (Left) | Caixa (Case) | Dir (Right)
                string lVal = (state.Left >= 0 && state.Left <= 100) ? state.Left + "%" + (state.LeftCharging ? "⚡" : "") : "--";
                string cVal = (state.Case >= 0 && state.Case <= 100) ? state.Case + "%" + (state.CaseCharging ? "⚡" : "") : "--";
                string rVal = (state.Right >= 0 && state.Right <= 100) ? state.Right + "%" + (state.RightCharging ? "⚡" : "") : "--";

                itemBatteryInfo.Text = string.Format("{0}: {1} | {2}: {3} | {4}: {5}",
                    Localization.BatteryLeft, lVal,
                    Localization.BatteryCase, cVal,
                    Localization.BatteryRight, rVal);
            }

            itemBatteryInfo.Invalidate();
        }

        public static Color GetBatteryColor(int level, bool charging)
        {
            if (charging) return Color.FromArgb(52, 199, 89);    // Verde com ⚡
            if (level > 50) return Color.FromArgb(52, 199, 89);  // Verde (> 50%)
            if (level > 20) return Color.FromArgb(255, 159, 10); // Laranja (21% - 50%)
            if (level >= 0) return Color.FromArgb(255, 69, 58);  // Vermelho (<= 20%)
            return Color.FromArgb(130, 130, 140);                // Desconectado / Indisponível
        }

        public static void DrawMiniBattery(Graphics g, int x, int y, int w, int h, int percent, bool charging, Color col)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int bodyW = w - 3;
            int bodyH = h;

            // Corpo externo da bateria com cantos arredondados
            using (var pen = new Pen(col, 1f))
            {
                GraphicsExtensions.DrawRoundedRectangle(g, pen, x, y, bodyW, bodyH, 2);
            }

            // Polo positivo na extremidade direita
            using (var brush = new SolidBrush(col))
            {
                int poleH = Math.Max(3, bodyH / 2);
                int poleY = y + (bodyH - poleH) / 2;
                g.FillRectangle(brush, x + bodyW, poleY, 2, poleH);

                // Nível interno preenchido com folga segura de 2px
                if (percent > 0)
                {
                    int fillMaxW = bodyW - 4;
                    int fillH = Math.Max(2, bodyH - 4);
                    int fillW = Math.Max(2, (int)Math.Round((double)fillMaxW * Math.Min(100, Math.Max(0, percent)) / 100.0));
                    g.FillRectangle(brush, x + 2, y + 2, fillW, fillH);
                }
            }
        }

        public void RenderBatteryItem(Graphics g, Rectangle bounds, Font font)
        {
            BatteryState state = client.Battery;
            if (state == null || !state.HasData)
            {
                TextRenderer.DrawText(g, Localization.TrayBatteryReadingText, font, bounds,
                    Color.FromArgb(140, 140, 150),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
                return;
            }

            Rectangle cardRect = new Rectangle(bounds.Left + 4, bounds.Top + 2, bounds.Width - 8, bounds.Height - 4);
            int colW = cardRect.Width / 3;

            using (var penDivider = new Pen(Color.FromArgb(46, 48, 60), 1f))
            using (var fontLbl = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (var fontVal = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            {
                for (int i = 0; i < 3; i++)
                {
                    int x = cardRect.Left + i * colW;
                    int w = (i == 2) ? (cardRect.Right - x) : colW;
                    Rectangle colRect = new Rectangle(x, cardRect.Top, w, cardRect.Height);

                    string lbl = (i == 0) ? Localization.BatteryLeft : (i == 1 ? Localization.BatteryCase : Localization.BatteryRight);
                    int level = (i == 0) ? state.Left : (i == 1 ? state.Case : state.Right);
                    bool chg = (i == 0) ? state.LeftCharging : (i == 1 ? state.CaseCharging : state.RightCharging);
                    Color col = GetBatteryColor(level, chg);

                    // Linha Superior: Rótulo (Esq / Caixa / Dir)
                    Rectangle topRect = new Rectangle(colRect.Left, colRect.Top + 3, colRect.Width, 14);
                    TextRenderer.DrawText(g, lbl, fontLbl, topRect, Color.FromArgb(135, 135, 145),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.SingleLine);

                    // Linha Inferior: Símbolo de bateria + Percentagem destacada
                    string valStr;
                    if (level >= 0 && level <= 100)
                    {
                        valStr = level.ToString() + "%" + (chg ? "⚡" : "");
                    }
                    else if (chg)
                    {
                        valStr = "⚡";
                    }
                    else
                    {
                        valStr = "--";
                    }

                    int iconW = 12;
                    int iconH = 8;
                    int gap = 3;
                    Size txtSize = TextRenderer.MeasureText(g, valStr, fontVal);
                    int totalContentW = (level >= 0 && level <= 100) ? (iconW + gap + txtSize.Width) : txtSize.Width;
                    int startX = colRect.Left + (colRect.Width - totalContentW) / 2;
                    int botY = colRect.Top + 18;

                    if (level >= 0 && level <= 100)
                    {
                        DrawMiniBattery(g, startX, botY + (txtSize.Height - iconH) / 2, iconW, iconH, level, chg, col);
                        Rectangle valRect = new Rectangle(startX + iconW + gap, botY, txtSize.Width, txtSize.Height);
                        TextRenderer.DrawText(g, valStr, fontVal, valRect, col,
                            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
                    }
                    else
                    {
                        Rectangle valRect = new Rectangle(colRect.Left, botY, colRect.Width, txtSize.Height);
                        TextRenderer.DrawText(g, valStr, fontVal, valRect, col,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
                    }

                    // Divisor vertical entre colunas
                    if (i < 2)
                    {
                        int divX = colRect.Right;
                        g.DrawLine(penDivider, divX, cardRect.Top + 6, divX, cardRect.Bottom - 6);
                    }
                }
            }
        }

        private void UpdateNoiseModeChecked()
        {
            if (itemModeAnc == null || itemModeOff == null || itemModeTrans == null) return;
            itemModeAnc.Checked = (client.CurrentMode == NoiseMode.NoiseCancellation);
            itemModeOff.Checked = (client.CurrentMode == NoiseMode.Off);
            itemModeTrans.Checked = (client.CurrentMode == NoiseMode.Transparency);
        }

        public void UpdateTooltip()
        {
            try
            {
                string name = client.DeviceName;
                if (string.IsNullOrEmpty(name))
                {
                    name = Localization.DeviceTitlePlaceholder;
                }

                if (!client.IsConnected)
                {
                    // Desconectado: "Xiaomi EarBuddy\nRedmi Buds 5 Pro\n● Desconectado"
                    string tipDisc = string.Format("{0}\n{1}\n{2}",
                        Localization.AppName,
                        name,
                        Localization.StatusDisconnected);
                    if (tipDisc.Length > 63) tipDisc = tipDisc.Substring(0, 63);
                    notifyIcon.Text = tipDisc;
                    return;
                }

                // Conectado:
                // Linha 1: Nome do auricular (ex: "Redmi Buds 5 Pro")
                // Linha 2: Bateria L:.. | Caixa:.. | R:.. (Ordem: Esq | Caixa | Dir)
                // Linha 3: Modo de ruído (ex: "Modo: Cancelamento")
                BatteryState bat = client.Battery;
                string lStr = (bat != null && bat.Left >= 0 && bat.Left <= 100) ? bat.Left.ToString() + "%" + (bat.LeftCharging ? "⚡" : "") : "--";
                string cStr = (bat != null && bat.Case >= 0 && bat.Case <= 100) ? bat.Case.ToString() + "%" + (bat.CaseCharging ? "⚡" : "") : "--";
                string rStr = (bat != null && bat.Right >= 0 && bat.Right <= 100) ? bat.Right.ToString() + "%" + (bat.RightCharging ? "⚡" : "") : "--";

                string modeShort = Localization.GetNoiseModeShort(client.CurrentMode);
                string modeLong = Localization.GetNoiseModeTitle(client.CurrentMode);
                string modeLbl = Localization.TrayModeLabel;

                // Formato 1: Com nome da caixa completo (Caixa/Case) e modo completo
                string batLine1 = string.Format("L:{0} | {1}:{2} | R:{3}", lStr, Localization.BatteryCase, cStr, rStr);
                string tip = string.Format("{0}\n{1}\n{2}: {3}", name, batLine1, modeLbl, modeLong);

                if (tip.Length > 63)
                {
                    // Formato 2: Com modo abreviado (ANC / Transp. / Desat.)
                    tip = string.Format("{0}\n{1}\n{2}: {3}", name, batLine1, modeLbl, modeShort);
                }

                if (tip.Length > 63)
                {
                    // Formato 3: Caixa abreviada "C" e modo abreviado
                    string batLine2 = string.Format("L:{0} | C:{1} | R:{2}", lStr, cStr, rStr);
                    tip = string.Format("{0}\n{1}\n{2}: {3}", name, batLine2, modeLbl, modeShort);
                }

                if (tip.Length > 63)
                {
                    tip = tip.Substring(0, 63);
                }

                notifyIcon.Text = tip;
            }
            catch { }
        }

        public void ShowWindow()
        {
            mainForm.Show();
            if (mainForm.WindowState == FormWindowState.Minimized)
            {
                mainForm.WindowState = FormWindowState.Normal;
            }
            mainForm.TopMost = true;
            mainForm.BringToFront();
            mainForm.Activate();
            mainForm.TopMost = false;
        }

        public void ToggleWindow()
        {
            if (mainForm.Visible && mainForm.WindowState != FormWindowState.Minimized)
            {
                mainForm.Hide();
            }
            else
            {
                ShowWindow();
            }
        }

        /// <summary>
        /// Ícone minimalista para o System Tray: Fundo preto puro (#000000) e fone branco (#FFFFFF).
        /// </summary>
        public static Icon CreateEarbudIcon()
        {
            using (var bmp = new Bitmap(32, 32))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                // Fundo circular preto sólido puro (#000000)
                using (var brushBg = new SolidBrush(Color.Black))
                {
                    g.FillEllipse(brushBg, 1, 1, 30, 30);
                }

                // Silhueta estilizada de auscultador em branco puro (#FFFFFF)
                using (var earBrush = new SolidBrush(Color.White))
                {
                    // Cabeça do auricular (ovóide ergonómico suave)
                    g.FillEllipse(earBrush, 8, 7, 13, 11);

                    // Almofada / ponta intra-auricular
                    g.FillEllipse(earBrush, 6, 9, 6, 7);

                    // Haste elegante do auricular (stem)
                    GraphicsExtensions.FillRoundedRectangle(g, earBrush, 14, 13, 5, 12, 2);
                }

                IntPtr hIcon = bmp.GetHicon();
                Icon icon = Icon.FromHandle(hIcon);
                Icon cloned = (Icon)icon.Clone();
                DestroyIcon(hIcon);
                return cloned;
            }
        }

        public void Dispose()
        {
            try
            {
                client.Connected -= OnDeviceConnected;
                client.Disconnected -= OnDeviceDisconnected;
                client.BatteryUpdated -= OnBatteryUpdated;
                client.NoiseModeChanged -= OnNoiseModeChanged;
                Localization.LanguageChanged -= OnLanguageChanged;
            }
            catch { }

            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            contextMenu.Dispose();
        }
    }

    /// <summary>
    /// Item de menu dedicado com renderização personalizada do cartão de bateria em 3 colunas (Esq | Caixa | Dir).
    /// Garante que o layout nunca é esmagado ou truncado pelo ContextMenuStrip.
    /// </summary>
    public class TrayBatteryMenuItem : ToolStripMenuItem
    {
        private readonly XiaomiMmaClient client;

        public TrayBatteryMenuItem(XiaomiMmaClient client)
        {
            this.client = client;
            this.Text = Localization.TrayBatteryReadingText;
            this.AutoSize = true;
            this.Margin = new Padding(0);
            this.Padding = new Padding(0);
            this.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        }

        public override bool CanSelect
        {
            get { return false; }
        }

        public override Size GetPreferredSize(Size constrainingSize)
        {
            int w = 275;
            if (constrainingSize.Width > 0 && constrainingSize.Width < 10000)
            {
                w = Math.Max(275, constrainingSize.Width);
            }
            return new Size(w, 64);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Fundo do item preenchido com a cor do menu para margens perfeitas
            using (var bgBrush = new SolidBrush(Color.FromArgb(22, 23, 28)))
            {
                g.FillRectangle(bgBrush, 0, 0, this.Width, this.Height);
            }

            int menuW = (this.Owner != null && this.Owner.ClientSize.Width > 0)
                ? this.Owner.ClientSize.Width
                : this.Width;

            int marginX = 10;
            int cardW = menuW - (marginX * 2);
            cardW = (cardW / 3) * 3;
            int actualMarginX = (menuW - cardW) / 2;
            int marginY = 4;
            Rectangle cardRect = new Rectangle(actualMarginX, marginY, cardW, this.Height - (marginY * 2));

            // Fundo do cartão arredondado (#1C1E26) e borda 1px (#2E303C)
            using (var cardBrush = new SolidBrush(Color.FromArgb(28, 30, 38)))
            using (var borderPen = new Pen(Color.FromArgb(46, 48, 60), 1f))
            {
                GraphicsExtensions.FillRoundedRectangle(g, cardBrush, cardRect.X, cardRect.Y, cardRect.Width, cardRect.Height, 8);
                GraphicsExtensions.DrawRoundedRectangle(g, borderPen, cardRect.X, cardRect.Y, cardRect.Width, cardRect.Height, 8);
            }

            int colW = cardRect.Width / 3;

            // Divisores verticais (#2E303C) entre colunas com folga superior e inferior
            using (var divPen = new Pen(Color.FromArgb(46, 48, 60), 1f))
            {
                g.DrawLine(divPen, cardRect.Left + colW, cardRect.Top + 8, cardRect.Left + colW, cardRect.Bottom - 8);
                g.DrawLine(divPen, cardRect.Left + colW * 2, cardRect.Top + 8, cardRect.Left + colW * 2, cardRect.Bottom - 8);
            }

            BatteryState state = client != null ? client.Battery : null;
            bool hasData = (state != null && state.HasData);

            using (var fontLbl = new Font("Segoe UI", 8f, FontStyle.Bold))
            using (var fontVal = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            {
                for (int i = 0; i < 3; i++)
                {
                    int colLeft = cardRect.Left + i * colW;
                    int colWidth = (i == 2) ? (cardRect.Right - colLeft) : colW;
                    Rectangle colRect = new Rectangle(colLeft, cardRect.Top, colWidth, cardRect.Height);

                    string lbl;
                    int level;
                    bool chg;

                    if (i == 0)
                    {
                        lbl = Localization.BatteryLeft;
                        level = hasData ? state.Left : -1;
                        chg = hasData && state.LeftCharging;
                    }
                    else if (i == 1)
                    {
                        lbl = Localization.BatteryCase;
                        level = hasData ? state.Case : -1;
                        chg = hasData && state.CaseCharging;
                    }
                    else
                    {
                        lbl = Localization.BatteryRight;
                        level = hasData ? state.Right : -1;
                        chg = hasData && state.RightCharging;
                    }

                    // Linha Superior: Rótulo (Esq / Caixa / Dir), Segoe UI 8pt Bold, sem corte
                    Rectangle topRect = new Rectangle(colRect.Left, cardRect.Top + 6, colRect.Width, 18);
                    TextRenderer.DrawText(g, lbl, fontLbl, topRect, Color.FromArgb(142, 142, 147),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoClipping);

                    // Linha Inferior: Mini ícone de bateria + percentagem com cor dinâmica e NoClipping
                    int botY = cardRect.Top + 26;
                    int valH = 22;
                    bool hasValue = (level >= 0 && level <= 100);

                    if (hasValue)
                    {
                        Color col = TrayManager.GetBatteryColor(level, chg);
                        string valStr = level.ToString() + "%" + (chg ? "⚡" : "");

                        int iconW = 15;
                        int iconH = 10;
                        int gap = 4;
                        Size txtSize = TextRenderer.MeasureText(g, valStr, fontVal, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
                        int totalContentW = iconW + gap + txtSize.Width;

                        if (totalContentW <= colRect.Width)
                        {
                            int startX = colRect.Left + (colRect.Width - totalContentW) / 2;
                            int iconY = botY + (valH - iconH) / 2;

                            TrayManager.DrawMiniBattery(g, startX, iconY, iconW, iconH, level, chg, col);

                            Rectangle valRect = new Rectangle(startX + iconW + gap, botY, txtSize.Width, valH);
                            TextRenderer.DrawText(g, valStr, fontVal, valRect, col,
                                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoClipping);
                        }
                        else
                        {
                            Rectangle valRect = new Rectangle(colRect.Left, botY, colRect.Width, valH);
                            TextRenderer.DrawText(g, valStr, fontVal, valRect, col,
                                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoClipping);
                        }
                    }
                    else if (chg)
                    {
                        Color col = Color.FromArgb(52, 199, 89);
                        Rectangle valRect = new Rectangle(colRect.Left, botY, colRect.Width, valH);
                        TextRenderer.DrawText(g, "⚡", fontVal, valRect, col,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoClipping);
                    }
                    else
                    {
                        Color col = Color.FromArgb(130, 130, 140);
                        Rectangle valRect = new Rectangle(colRect.Left, botY, colRect.Width, valH);
                        TextRenderer.DrawText(g, "--", fontVal, valRect, col,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoClipping);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Renderizador do menu de contexto escuro, arredondado e moderno estilo HyperOS.
    /// </summary>
    public class ModernDarkMenuRenderer : ToolStripProfessionalRenderer
    {
        private static readonly Color colBg = Color.FromArgb(22, 23, 28);
        private static readonly Color colBorder = Color.FromArgb(45, 46, 54);
        private static readonly Color colHover = Color.FromArgb(36, 38, 46);
        private static readonly Color colTextNormal = Color.FromArgb(240, 240, 245);
        private static readonly Color colTextMuted = Color.FromArgb(135, 135, 142);
        private static readonly Color colAccent = Color.FromArgb(28, 140, 255);       // Modern Vibrant Blue

        private readonly TrayManager trayManager;
        private readonly XiaomiMmaClient client;

        public ModernDarkMenuRenderer(TrayManager trayManager, XiaomiMmaClient client) : base(new DarkMenuColorTable())
        {
            this.trayManager = trayManager;
            this.client = client;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (var brush = new SolidBrush(colBg))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (var pen = new Pen(colBorder, 1f))
            {
                Rectangle r = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
                e.Graphics.DrawRectangle(pen, r);
            }
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            // Não destacar fundo para cabeçalhos fixos
            if (e.Item == trayManager.ItemDeviceTitle || e.Item == trayManager.LblModoHeader)
            {
                return;
            }

            // Cartão de bateria com fundo de dashboard exclusivo e cantos arredondados (gerido pelo próprio item)
            if (e.Item == trayManager.ItemBatteryInfo || e.Item is TrayBatteryMenuItem)
            {
                return;
            }

            if (!e.Item.Enabled) return;

            Rectangle rect = new Rectangle(4, 1, e.Item.Width - 8, e.Item.Height - 2);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (e.Item.Selected)
            {
                using (var brush = new SolidBrush(colHover))
                {
                    GraphicsExtensions.FillRoundedRectangle(e.Graphics, brush, rect.X, rect.Y, rect.Width, rect.Height, 6);
                }
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            // 1. Título do dispositivo: Centralizado e em destaque (10.5pt Bold)
            if (e.Item == trayManager.ItemDeviceTitle)
            {
                Rectangle titleRect = new Rectangle(0, 0, e.Item.Width, e.Item.Height);
                TextRenderer.DrawText(e.Graphics, e.Text, e.TextFont, titleRect,
                    Color.FromArgb(245, 245, 247),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
                return;
            }

            // 2. Cartão de bateria: Renderização gerida pelo próprio TrayBatteryMenuItem.OnPaint
            if (e.Item == trayManager.ItemBatteryInfo || e.Item is TrayBatteryMenuItem)
            {
                return;
            }

            // 3. Iniciar com o Windows: Badge [ ON ] / [ OFF ] sleek modern pill imediatamente a seguir ao texto
            if (e.Item == trayManager.ItemAutoStart)
            {
                bool isAuto = trayManager.IsAutoStartEnabled();
                string label = "🚀 " + Localization.TrayStartWithWindows;
                string badgeText = isAuto ? "ON" : "OFF";

                int startX = Math.Max(10, e.TextRectangle.Left);
                Size labelSize = TextRenderer.MeasureText(e.Graphics, label, e.TextFont, new Size(1000, e.Item.Height),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

                Rectangle labelRect = new Rectangle(startX, 0, labelSize.Width, e.Item.Height);
                TextRenderer.DrawText(e.Graphics, label, e.TextFont, labelRect, colTextNormal,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

                int badgeGap = 8;
                int badgeX = labelRect.Right + badgeGap;

                using (var fontBadge = new Font("Segoe UI", 8f, FontStyle.Bold))
                {
                    Size badgeTextSize = TextRenderer.MeasureText(e.Graphics, badgeText, fontBadge, Size.Empty,
                        TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

                    int pillW = Math.Max(34, badgeTextSize.Width + 14);
                    int pillH = 18;
                    int pillY = (e.Item.Height - pillH) / 2;
                    Rectangle pillRect = new Rectangle(badgeX, pillY, pillW, pillH);

                    Color pillBg = isAuto ? Color.FromArgb(27, 77, 43) : Color.FromArgb(38, 40, 50);
                    Color pillBorder = isAuto ? Color.FromArgb(52, 199, 89) : Color.FromArgb(60, 62, 75);
                    Color pillText = isAuto ? Color.FromArgb(52, 199, 89) : Color.FromArgb(140, 140, 150);

                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var bgBrush = new SolidBrush(pillBg))
                    using (var borderPen = new Pen(pillBorder, 1f))
                    {
                        GraphicsExtensions.FillRoundedRectangle(e.Graphics, bgBrush, pillRect.X, pillRect.Y, pillRect.Width, pillRect.Height, 4);
                        GraphicsExtensions.DrawRoundedRectangle(e.Graphics, borderPen, pillRect.X, pillRect.Y, pillRect.Width, pillRect.Height, 4);
                    }

                    TextRenderer.DrawText(e.Graphics, badgeText, fontBadge, pillRect, pillText,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
                }
                return;
            }

            Color textColor = colTextNormal;
            if (!e.Item.Enabled)
            {
                textColor = colTextMuted;
            }
            else if (e.Item == trayManager.ItemExit)
            {
                textColor = Color.FromArgb(255, 69, 58);
            }
            else
            {
                ToolStripMenuItem mi = e.Item as ToolStripMenuItem;
                if (mi != null && mi.Checked)
                {
                    textColor = colAccent;
                }
            }

            TextRenderer.DrawText(e.Graphics, e.Text, e.TextFont, e.TextRectangle, textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            // Indicador de seleção circular azul vibrante estilo HyperOS
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = e.ImageRectangle;
            using (var brush = new SolidBrush(colAccent))
            {
                e.Graphics.FillEllipse(brush, r.X + (r.Width - 8) / 2, r.Y + (r.Height - 8) / 2, 8, 8);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            using (var pen = new Pen(Color.FromArgb(38, 40, 48), 1f))
            {
                e.Graphics.DrawLine(pen, 12, y, e.Item.Width - 12, y);
            }
        }
    }

    public class DarkMenuColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return Color.FromArgb(22, 23, 28); } }
        public override Color MenuBorder { get { return Color.FromArgb(45, 46, 54); } }
        public override Color MenuItemBorder { get { return Color.Transparent; } }
        public override Color MenuItemSelected { get { return Color.FromArgb(36, 38, 46); } }
        public override Color ImageMarginGradientBegin { get { return Color.FromArgb(22, 23, 28); } }
        public override Color ImageMarginGradientMiddle { get { return Color.FromArgb(22, 23, 28); } }
        public override Color ImageMarginGradientEnd { get { return Color.FromArgb(22, 23, 28); } }
        public override Color SeparatorDark { get { return Color.FromArgb(38, 40, 48); } }
        public override Color SeparatorLight { get { return Color.FromArgb(38, 40, 48); } }
    }

    public static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics g, Brush brush, int x, int y, int width, int height, int radius)
        {
            using (GraphicsPath path = RoundedRect(new Rectangle(x, y, width, height), radius))
            {
                g.FillPath(brush, path);
            }
        }

        public static void DrawRoundedRectangle(this Graphics g, Pen pen, int x, int y, int width, int height, int radius)
        {
            using (GraphicsPath path = RoundedRect(new Rectangle(x, y, width, height), radius))
            {
                g.DrawPath(pen, path);
            }
        }

        public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
