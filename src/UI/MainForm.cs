using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MiBudsApp.Bluetooth;
using MiBudsApp.Common;

namespace MiBudsApp.UI
{
    public class MainForm : Form
    {
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;
        private const int WM_GETMINMAXINFO = 0x0024;

        public const int DEFAULT_WIDTH = 480;
        public const int DEFAULT_HEIGHT = 688;

        private readonly XiaomiMmaClient client;
        private TrayManager trayManager;

        // Cores oficiais Xiaomi HyperOS Dark Theme (com destaque azul a pedido do utilizador)
        private readonly Color colBg = Color.FromArgb(14, 15, 18);
        private readonly Color colCard = Color.FromArgb(24, 25, 30);
        private readonly Color colCardBorder = Color.FromArgb(38, 40, 48);
        private readonly Color colAccent = Color.FromArgb(28, 140, 255);       // Modern Vibrant Blue
        private readonly Color colTextPrimary = Color.FromArgb(245, 245, 247);
        private readonly Color colTextSecondary = Color.FromArgb(142, 142, 147);
        private readonly Color colGreen = Color.FromArgb(52, 199, 89);

        // Controlos
        private Panel pnlHeader;
        private Label lblTitle;
        private Label lblStatusPill;
        private Button btnLanguage;
        private Button btnResetSize;
        private Button btnMin;
        private Button btnClose;
        private ToolTip toolTip;

        private EarbudsCaseHeroView heroView;

        private RoundedPanel pnlNoiseCard;
        private Label lblNoiseTitle;
        private CircularNoiseButton btnTrans;
        private CircularNoiseButton btnAnc;
        private CircularNoiseButton btnOff;

        private RoundedPanel pnlFooter;
        private Button btnMinimizeToTray;
        private Label lblDeviceInfo;
        private Label lblHint;

        public MainForm(XiaomiMmaClient client)
        {
            this.client = client;

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            InitializeComponent();
            ApplyDarkTheme();

            IntPtr force = this.Handle;

            // Ligar eventos do cliente Bluetooth
            client.Connected += OnConnected;
            client.Disconnected += OnDisconnected;
            client.BatteryUpdated += OnBatteryUpdated;
            client.NoiseModeChanged += OnNoiseModeChanged;
            client.StatusChanged += OnStatusChanged;

            // Ligar evento de troca de idioma
            Localization.LanguageChanged += OnLanguageChanged;

            UpdateLocalizedStrings();
        }

        public void SetTrayManager(TrayManager tray)
        {
            this.trayManager = tray;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x20000; // CS_DROPSHADOW
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED (evita flicker na hierarquia de controlos durante o resize)
                return cp;
            }
        }

        private string GetCurrentDeviceName()
        {
            if (client != null && !string.IsNullOrEmpty(client.DeviceName))
            {
                return client.DeviceName;
            }
            return Localization.DeviceTitlePlaceholder;
        }

        private void InitializeComponent()
        {
            this.Text = "Mi Earbuddy - " + GetCurrentDeviceName();
            this.AutoScaleMode = AutoScaleMode.None;
            this.Size = new Size(DEFAULT_WIDTH, DEFAULT_HEIGHT);
            this.MinimumSize = new Size(440, 674);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ShowInTaskbar = true;
            this.BackColor = colBg;
            this.Icon = TrayManager.CreateEarbudIcon();

            // 1. Cabeçalho / Barra Superior (Altura 76px) com DoubleBuffering ativado
            pnlHeader = new DoubleBufferedPanel
            {
                Dock = DockStyle.Top,
                Height = 76,
                BackColor = colBg
            };
            pnlHeader.MouseDown += Header_MouseDown;
            pnlHeader.Resize += (s, e) => LayoutHeaderButtons();
            pnlHeader.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(Color.FromArgb(45, 47, 56), 1f))
                using (var path = GraphicsExtensions.RoundedRect(new Rectangle(0, 0, this.Width - 1, this.Height - 1), 16))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };

            lblTitle = new Label
            {
                Text = GetCurrentDeviceName(),
                Font = new Font("Segoe UI", 14.5f, FontStyle.Bold),
                ForeColor = colTextPrimary,
                Location = new Point(18, 11),
                AutoSize = false,
                AutoEllipsis = true,
                Size = new Size(240, 36)
            };
            lblTitle.MouseDown += Header_MouseDown;

            lblStatusPill = new Label
            {
                Text = Localization.StatusConnecting,
                Font = new Font("Segoe UI", 9f),
                ForeColor = colAccent,
                Location = new Point(20, 49),
                AutoSize = true
            };
            lblStatusPill.MouseDown += Header_MouseDown;

            // Botão Fechar (✕) - Estilo Moderno com margem superior e cantos arredondados
            btnClose = new ModernCaptionButton
            {
                Text = "✕",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                NormalBackColor = Color.Transparent,
                HoverBackColor = Color.FromArgb(232, 17, 35),
                PressedBackColor = Color.FromArgb(196, 43, 28),
                NormalForeColor = Color.FromArgb(192, 192, 200),
                HoverForeColor = Color.White,
                CornerRadius = 6,
                Cursor = Cursors.Default
            };
            btnClose.Click += (s, e) => this.Hide();

            // Botão Minimizar (─)
            btnMin = new ModernCaptionButton
            {
                Text = "─",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Regular),
                NormalBackColor = Color.Transparent,
                HoverBackColor = Color.FromArgb(45, 48, 58),
                PressedBackColor = Color.FromArgb(36, 38, 48),
                NormalForeColor = Color.FromArgb(192, 192, 200),
                HoverForeColor = Color.White,
                CornerRadius = 6,
                Cursor = Cursors.Default
            };
            btnMin.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            // Botão Repor Tamanho (↺)
            btnResetSize = new ModernCaptionButton
            {
                Text = "↺",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Regular),
                NormalBackColor = Color.Transparent,
                HoverBackColor = Color.FromArgb(45, 48, 58),
                PressedBackColor = Color.FromArgb(36, 38, 48),
                NormalForeColor = colTextSecondary,
                HoverForeColor = Color.White,
                CornerRadius = 6,
                Cursor = Cursors.Hand
            };
            btnResetSize.Click += (s, e) =>
            {
                this.Size = new Size(DEFAULT_WIDTH, DEFAULT_HEIGHT);
                this.CenterToScreen();
            };

            // Botão de Idioma PT / EN com cantos arredondados e borda limpa (sem símbolo a pedido do utilizador)
            btnLanguage = new ModernCaptionButton
            {
                Text = Localization.IsPortuguese ? "PT" : "EN",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                NormalBackColor = Color.FromArgb(28, 30, 36),
                HoverBackColor = Color.FromArgb(45, 48, 58),
                PressedBackColor = Color.FromArgb(36, 38, 48),
                BorderColor = Color.FromArgb(48, 50, 62),
                NormalForeColor = colTextPrimary,
                HoverForeColor = Color.White,
                CornerRadius = 6,
                Cursor = Cursors.Hand
            };
            btnLanguage.Click += (s, e) => Localization.ToggleLanguage();

            toolTip = new ToolTip();
            toolTip.SetToolTip(btnClose, Localization.IsPortuguese ? "Fechar (Ocultar na bandeja)" : "Close (Hide to tray)");
            toolTip.SetToolTip(btnMin, Localization.IsPortuguese ? "Minimizar" : "Minimize");
            toolTip.SetToolTip(btnResetSize, Localization.BtnResetSizeTooltip);
            toolTip.SetToolTip(btnLanguage, Localization.IsPortuguese ? "Mudar idioma para Inglês (EN)" : "Switch language to Portuguese (PT)");

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblStatusPill);
            pnlHeader.Controls.Add(btnLanguage);
            pnlHeader.Controls.Add(btnResetSize);
            pnlHeader.Controls.Add(btnMin);
            pnlHeader.Controls.Add(btnClose);
            this.Controls.Add(pnlHeader);

            // 2. Cartão Visual dos Earbuds & Caixa (Hero Card)
            // Margens uniformes: X = 16, Top = 76 + 14 = 90, Altura = 216
            heroView = new EarbudsCaseHeroView
            {
                Location = new Point(16, 90),
                Size = new Size(this.ClientSize.Width - 32, 216),
                BackColor = colCard,
                BorderColor = colCardBorder,
                CornerRadius = 16
            };
            this.Controls.Add(heroView);
            if (client != null && client.Battery != null && client.Battery.HasData)
            {
                heroView.UpdateBattery(client.Battery);
            }

            // 3. Cartão de Cancelamento de Ruído (Controlo de Ruído)
            // Top = 306 + 14 = 320, Altura = 176
            pnlNoiseCard = new RoundedPanel
            {
                Location = new Point(16, 320),
                Size = new Size(this.ClientSize.Width - 32, 176),
                BackColor = colCard,
                BorderColor = colCardBorder,
                CornerRadius = 16
            };

            lblNoiseTitle = new Label
            {
                Text = Localization.NoiseControlHeader,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = colTextSecondary,
                Location = new Point(18, 14),
                AutoSize = true
            };
            pnlNoiseCard.Controls.Add(lblNoiseTitle);

            // Três botões circulares na ordem oficial da Xiaomi: Transparência, Cancelamento, Desativado
            btnTrans = new CircularNoiseButton(Localization.TransparencyTitle, Localization.TransparencySubtitle, NoiseMode.Transparency);
            btnTrans.Click += (s, e) => SelectNoiseMode(NoiseMode.Transparency);

            btnAnc = new CircularNoiseButton(Localization.NoiseCancellationTitle, Localization.NoiseCancellationSubtitle, NoiseMode.NoiseCancellation);
            btnAnc.Click += (s, e) => SelectNoiseMode(NoiseMode.NoiseCancellation);

            btnOff = new CircularNoiseButton(Localization.OffTitle, Localization.OffSubtitle, NoiseMode.Off);
            btnOff.Click += (s, e) => SelectNoiseMode(NoiseMode.Off);

            pnlNoiseCard.Controls.Add(btnTrans);
            pnlNoiseCard.Controls.Add(btnAnc);
            pnlNoiseCard.Controls.Add(btnOff);
            pnlNoiseCard.Resize += (s, e) => LayoutNoiseButtons();
            LayoutNoiseButtons();

            this.Controls.Add(pnlNoiseCard);

            // 4. Cartão Inferior com Informações e Ação de Minimizar
            // Top = 496 + 14 = 510, Altura = Math.Max(150, ClientSize.Height - 510 - 14)
            int initialFooterH = Math.Max(150, this.ClientSize.Height - 510 - 14);
            pnlFooter = new RoundedPanel
            {
                Location = new Point(16, 510),
                Size = new Size(this.ClientSize.Width - 32, initialFooterH),
                BackColor = colCard,
                BorderColor = colCardBorder,
                CornerRadius = 16
            };

            btnMinimizeToTray = new Button
            {
                Text = Localization.BtnMinimize,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = colTextPrimary,
                BackColor = Color.FromArgb(36, 37, 44),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnMinimizeToTray.FlatAppearance.BorderSize = 0;
            btnMinimizeToTray.FlatAppearance.MouseOverBackColor = Color.FromArgb(48, 50, 60);
            btnMinimizeToTray.Click += (s, e) => this.Hide();

            lblDeviceInfo = new Label
            {
                Text = Localization.TechInfo,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(125, 125, 132),
                AutoSize = false,
                AutoEllipsis = true
            };

            lblHint = new Label
            {
                Text = Localization.HintHoverTray,
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Color.FromArgb(145, 145, 150),
                AutoSize = false,
                AutoEllipsis = true
            };

            pnlFooter.Controls.Add(btnMinimizeToTray);
            pnlFooter.Controls.Add(lblDeviceInfo);
            pnlFooter.Controls.Add(lblHint);
            pnlFooter.Resize += (s, e) => LayoutFooter();
            LayoutFooter();

            this.Controls.Add(pnlFooter);

            LayoutHeaderButtons();
            UpdateFormRegion();
        }

        private void LayoutHeaderButtons()
        {
            if (pnlHeader == null) return;
            int w = pnlHeader.ClientSize.Width;

            // Margem superior e lateral direita limpas e consistentes
            int right = w - 14;
            int btnY = 13;
            int btnH = 26;

            int xClose = right - 32;
            int xMin = xClose - 4 - 32;
            int xReset = xMin - 4 - 26;
            int xLang = xReset - 6 - 36;

            // 1. Fechar (✕): 32x26px
            if (btnClose != null)
            {
                btnClose.Size = new Size(32, btnH);
                btnClose.Location = new Point(xClose, btnY);
            }

            // 2. Minimizar (─): 32x26px
            if (btnMin != null)
            {
                btnMin.Size = new Size(32, btnH);
                btnMin.Location = new Point(xMin, btnY);
            }

            // 3. Repor Tamanho (↺): 26x26px
            if (btnResetSize != null)
            {
                btnResetSize.Size = new Size(26, btnH);
                btnResetSize.Location = new Point(xReset, btnY);
            }

            // 4. Alternar Idioma (PT / EN): 36x26px
            if (btnLanguage != null)
            {
                btnLanguage.Size = new Size(36, btnH);
                btnLanguage.Location = new Point(xLang, btnY);
            }

            // Título totalmente adaptativo e com altura suficiente (36px) para nunca cortar o texto na horizontal
            // Espaçamento generoso (gap de 18px) antes do botão de idioma para nunca ficar colado
            if (lblTitle != null)
            {
                lblTitle.Location = new Point(18, 11);
                int maxTitleW = Math.Max(50, xLang - 18 - 18);
                lblTitle.Size = new Size(maxTitleW, 36);
            }

            if (lblStatusPill != null)
            {
                lblStatusPill.Location = new Point(20, 50);
            }
        }

        private void LayoutNoiseButtons()
        {
            if (pnlNoiseCard == null || btnTrans == null || btnAnc == null || btnOff == null) return;
            int cardW = pnlNoiseCard.ClientSize.Width;
            int margin = 12;
            int availableW = cardW - (margin * 2);
            int btnW = availableW / 3;
            int btnH = 126;
            int y = 38;

            btnTrans.Location = new Point(margin, y);
            btnTrans.Size = new Size(btnW, btnH);

            btnAnc.Location = new Point(margin + btnW, y);
            btnAnc.Size = new Size(btnW, btnH);

            btnOff.Location = new Point(margin + btnW * 2, y);
            btnOff.Size = new Size(btnW, btnH);
        }

        private void LayoutFooter()
        {
            if (pnlFooter == null) return;
            int cardW = pnlFooter.ClientSize.Width;

            btnMinimizeToTray.Location = new Point(20, 16);
            btnMinimizeToTray.Size = new Size(cardW - 40, 42);

            lblDeviceInfo.Location = new Point(20, 68);
            lblDeviceInfo.Size = new Size(cardW - 40, 20);

            lblHint.Location = new Point(20, 92);
            lblHint.Size = new Size(cardW - 40, Math.Max(40, pnlFooter.ClientSize.Height - 96));
        }

        public void UpdateLocalizedStrings()
        {
            this.Text = "Mi Earbuddy - " + GetCurrentDeviceName();
            if (lblTitle != null)
            {
                lblTitle.Text = GetCurrentDeviceName();
            }

            if (btnLanguage != null)
            {
                btnLanguage.Text = Localization.IsPortuguese ? "PT" : "EN";
            }

            if (lblNoiseTitle != null)
            {
                lblNoiseTitle.Text = Localization.NoiseControlHeader;
            }

            if (btnTrans != null)
            {
                btnTrans.SetText(Localization.TransparencyTitle, Localization.TransparencySubtitle);
            }

            if (btnAnc != null)
            {
                btnAnc.SetText(Localization.NoiseCancellationTitle, Localization.NoiseCancellationSubtitle);
            }

            if (btnOff != null)
            {
                btnOff.SetText(Localization.OffTitle, Localization.OffSubtitle);
            }

            if (btnMinimizeToTray != null)
            {
                btnMinimizeToTray.Text = Localization.BtnMinimize;
            }

            if (lblDeviceInfo != null)
            {
                lblDeviceInfo.Text = Localization.TechInfo;
            }

            if (lblHint != null)
            {
                lblHint.Text = Localization.HintHoverTray;
            }

            if (toolTip != null)
            {
                toolTip.SetToolTip(btnClose, Localization.IsPortuguese ? "Fechar (Ocultar na bandeja)" : "Close (Hide to tray)");
                toolTip.SetToolTip(btnMin, Localization.IsPortuguese ? "Minimizar" : "Minimize");
                toolTip.SetToolTip(btnResetSize, Localization.BtnResetSizeTooltip);
                toolTip.SetToolTip(btnLanguage, Localization.IsPortuguese ? "Mudar idioma para Inglês (EN)" : "Switch language to Portuguese (PT)");
            }

            UpdateStatusLabel();
            LayoutHeaderButtons();
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(UpdateLocalizedStrings)); } catch { }
                return;
            }
            UpdateLocalizedStrings();
        }

        private int lastRegionWidth = 0;
        private int lastRegionHeight = 0;

        private void UpdateFormRegion()
        {
            try
            {
                if (this.WindowState == FormWindowState.Minimized) return;
                if (this.Width <= 16 || this.Height <= 16) return;
                if (this.Width == lastRegionWidth && this.Height == lastRegionHeight) return;

                lastRegionWidth = this.Width;
                lastRegionHeight = this.Height;

                Region oldRegion = this.Region;
                using (GraphicsPath path = GraphicsExtensions.RoundedRect(new Rectangle(0, 0, this.Width, this.Height), 16))
                {
                    this.Region = new Region(path);
                }
                if (oldRegion != null)
                {
                    try { oldRegion.Dispose(); } catch { }
                }
            }
            catch { }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UpdateFormRegion();
            LayoutHeaderButtons();
            if (client != null && client.Battery != null && client.Battery.HasData)
            {
                heroView.UpdateBattery(client.Battery);
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (this.Visible && client != null && client.Battery != null && client.Battery.HasData)
            {
                heroView.UpdateBattery(client.Battery);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            UpdateFormRegion();
            LayoutHeaderButtons();

            int cardWidth = Math.Max(200, this.ClientSize.Width - 32);

            if (heroView != null)
            {
                heroView.Location = new Point(16, 90);
                heroView.Size = new Size(cardWidth, 216);
            }

            if (pnlNoiseCard != null)
            {
                pnlNoiseCard.Location = new Point(16, 320);
                pnlNoiseCard.Size = new Size(cardWidth, 176);
                LayoutNoiseButtons();
            }

            if (pnlFooter != null)
            {
                pnlFooter.Location = new Point(16, 510);
                int footerH = Math.Max(150, this.ClientSize.Height - 510 - 14);
                pnlFooter.Size = new Size(cardWidth, footerH);
                LayoutFooter();
            }

            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(Color.FromArgb(45, 47, 56), 1f))
            using (var path = GraphicsExtensions.RoundedRect(new Rectangle(0, 0, this.Width - 1, this.Height - 1), 16))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }

        private void Header_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void ApplyDarkTheme()
        {
            try
            {
                int preference = 1; // DWMWA_USE_IMMERSIVE_DARK_MODE
                DwmSetWindowAttribute(this.Handle, 20, ref preference, sizeof(int));
            }
            catch { }

            ApplyWindowCorners();
        }

        private void ApplyWindowCorners()
        {
            try
            {
                // 33 = DWMWA_WINDOW_CORNER_PREFERENCE
                // 2 = DWMWCP_ROUND (Round the corners appropriately)
                int roundPref = 2;
                DwmSetWindowAttribute(this.Handle, 33, ref roundPref, sizeof(int));
            }
            catch { }
        }

        private void SelectNoiseMode(NoiseMode mode)
        {
            if (client != null && client.CurrentMode == mode) return;
            UpdateNoiseButtonsVisual(mode);
            if (client != null) client.SetNoiseMode(mode);
        }

        private void UpdateNoiseButtonsVisual(NoiseMode mode)
        {
            btnTrans.IsActive = (mode == NoiseMode.Transparency);
            btnAnc.IsActive = (mode == NoiseMode.NoiseCancellation);
            btnOff.IsActive = (mode == NoiseMode.Off);
        }

        private void OnConnected(string name)
        {
            if (!IsHandleCreated || IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnConnected(name))); } catch { }
                return;
            }

            lblTitle.Text = GetCurrentDeviceName();
            this.Text = "Mi Earbuddy - " + GetCurrentDeviceName();
            UpdateStatusLabel();

            heroView.SetConnected(true);
            if (client != null && client.Battery != null && client.Battery.HasData)
            {
                heroView.UpdateBattery(client.Battery);
            }
            UpdateNoiseButtonsVisual(client.CurrentMode);
        }

        private void OnDisconnected()
        {
            if (!IsHandleCreated || IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(OnDisconnected)); } catch { }
                return;
            }

            UpdateStatusLabel();
            heroView.SetConnected(false);
        }

        private string lastStatusRaw = "";

        private void OnStatusChanged(string status)
        {
            if (!IsHandleCreated || IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnStatusChanged(status))); } catch { }
                return;
            }

            lastStatusRaw = status;
            UpdateStatusLabel();
        }

        private void UpdateStatusLabel()
        {
            if (lblStatusPill == null) return;

            if (client != null && client.IsConnected)
            {
                lblStatusPill.Text = Localization.StatusConnected;
                lblStatusPill.ForeColor = colGreen;
            }
            else
            {
                string text;
                if (string.IsNullOrEmpty(lastStatusRaw))
                {
                    text = Localization.StatusConnecting;
                }
                else if (lastStatusRaw.IndexOf("procurar", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         lastStatusRaw.IndexOf("searching", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    text = Localization.StatusSearching;
                }
                else if (lastStatusRaw.IndexOf("aguardando", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         lastStatusRaw.IndexOf("waiting", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    text = Localization.StatusWaitingConnection;
                }
                else if (lastStatusRaw.IndexOf("reconectar", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         lastStatusRaw.IndexOf("reconnecting", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    text = Localization.StatusReconnecting;
                }
                else if (lastStatusRaw.IndexOf("desconectado", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         lastStatusRaw.IndexOf("disconnected", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    text = Localization.StatusDisconnected;
                }
                else
                {
                    text = lastStatusRaw.StartsWith("● ") ? lastStatusRaw : ("● " + lastStatusRaw);
                }

                lblStatusPill.Text = text;
                lblStatusPill.ForeColor = (text == Localization.StatusDisconnected) ? Color.FromArgb(255, 69, 58) : colAccent;
            }

            LayoutHeaderButtons();
        }

        private void OnBatteryUpdated(BatteryState state)
        {
            if (!IsHandleCreated || IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnBatteryUpdated(state))); } catch { }
                return;
            }

            if (state != null && state.HasData)
            {
                heroView.UpdateBattery(state);
            }
            else
            {
                heroView.ResetBattery();
            }
        }

        private void OnNoiseModeChanged(NoiseMode mode)
        {
            if (!IsHandleCreated || IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnNoiseModeChanged(mode))); } catch { }
                return;
            }

            UpdateNoiseButtonsVisual(mode);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                // Minimizar para a bandeja em vez de fechar
                e.Cancel = true;
                this.Hide();
            }
            else
            {
                base.OnFormClosing(e);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_GETMINMAXINFO)
            {
                base.WndProc(ref m);
                try
                {
                    MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(m.LParam, typeof(MINMAXINFO));
                    mmi.ptMinTrackSize.x = 440;
                    mmi.ptMinTrackSize.y = 674;
                    Marshal.StructureToPtr(mmi, m.LParam, true);
                }
                catch { }
                return;
            }

            const int WM_NCHITTEST = 0x84;
            const int HTLEFT = 10;
            const int HTRIGHT = 11;
            const int HTTOP = 12;
            const int HTTOPLEFT = 13;
            const int HTTOPRIGHT = 14;
            const int HTBOTTOM = 15;
            const int HTBOTTOMLEFT = 16;
            const int HTBOTTOMRIGHT = 17;

            // Permite redimensionar a janela arrastando os rebordos ou cantos
            if (m.Msg == WM_NCHITTEST)
            {
                base.WndProc(ref m);
                Point pt = this.PointToClient(new Point(m.LParam.ToInt32()));

                // Os botões da barra de título no canto superior direito devem receber os cliques normalmente
                if (pt.X >= ClientSize.Width - 180 && pt.Y <= 44)
                {
                    return;
                }

                int border = 8;
                if (pt.X <= border && pt.Y <= border) { m.Result = (IntPtr)HTTOPLEFT; return; }
                if (pt.X >= ClientSize.Width - border && pt.Y <= border) { m.Result = (IntPtr)HTTOPRIGHT; return; }
                if (pt.X <= border && pt.Y >= ClientSize.Height - border) { m.Result = (IntPtr)HTBOTTOMLEFT; return; }
                if (pt.X >= ClientSize.Width - border && pt.Y >= ClientSize.Height - border) { m.Result = (IntPtr)HTBOTTOMRIGHT; return; }
                if (pt.X <= border) { m.Result = (IntPtr)HTLEFT; return; }
                if (pt.X >= ClientSize.Width - border) { m.Result = (IntPtr)HTRIGHT; return; }
                if (pt.Y <= border) { m.Result = (IntPtr)HTTOP; return; }
                if (pt.Y >= ClientSize.Height - border) { m.Result = (IntPtr)HTBOTTOM; return; }
                return;
            }

            if (Program.WM_ACTIVATE_APP_MSG != 0 && (uint)m.Msg == Program.WM_ACTIVATE_APP_MSG)
            {
                AppLog.Log("WM_ACTIVATE_APP_MSG received! Restoring window to foreground.");
                this.Show();
                if (this.WindowState == FormWindowState.Minimized)
                {
                    this.WindowState = FormWindowState.Normal;
                }
                this.TopMost = true;
                this.BringToFront();
                this.Activate();
                this.TopMost = false;
                return;
            }

            base.WndProc(ref m);
        }
    }

    /// <summary>
    /// Botão circular elegante de controlo de ruído baseado diretamente no design da aplicação Xiaomi Earbuds.
    /// </summary>
    public class CircularNoiseButton : Control
    {
        private string title;
        private string subtitle;
        private readonly NoiseMode mode;
        private bool isActive = false;
        private bool isHovered = false;

        public bool IsActive
        {
            get { return isActive; }
            set { isActive = value; Invalidate(); }
        }

        public CircularNoiseButton(string title, string subtitle, NoiseMode mode)
        {
            this.title = title;
            this.subtitle = subtitle;
            this.mode = mode;

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.ResizeRedraw, true);
            this.BackColor = Color.FromArgb(24, 25, 30);
            this.Cursor = Cursors.Hand;
        }

        public void SetText(string newTitle, string newSubtitle)
        {
            this.title = newTitle;
            this.subtitle = newSubtitle;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            isHovered = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color parentBg = Parent != null ? Parent.BackColor : this.BackColor;
            using (var bgBrush = new SolidBrush(parentBg))
            {
                g.FillRectangle(bgBrush, ClientRectangle);
            }

            int cx = Width / 2;
            int circleD = 48;
            int circleY = 6;
            Rectangle circleRect = new Rectangle(cx - circleD / 2, circleY, circleD, circleD);

            // Cores do botão circular (Azul vibrante)
            Color colAccent = Color.FromArgb(28, 140, 255);       // Modern Vibrant Blue
            Color colCircleBg = isActive ? Color.FromArgb(16, 36, 62) :
                                isHovered ? Color.FromArgb(36, 37, 44) : Color.FromArgb(26, 27, 32);

            Color colCircleBorder = isActive ? colAccent :
                                    isHovered ? Color.FromArgb(60, 62, 72) : Color.FromArgb(42, 44, 52);

            using (var brush = new SolidBrush(colCircleBg))
            using (var pen = new Pen(colCircleBorder, isActive ? 2.2f : 1.2f))
            {
                g.FillEllipse(brush, circleRect);
                g.DrawEllipse(pen, circleRect);
            }

            // Desenhar Ícone Vectorial Específico do Modo
            Color iconColor = isActive ? Color.White : (isHovered ? Color.FromArgb(220, 220, 225) : Color.FromArgb(140, 140, 148));
            DrawModeIcon(g, cx, circleY + circleD / 2, iconColor);

            // Texto: Título
            Color titleColor = isActive ? Color.White : (isHovered ? Color.FromArgb(240, 240, 245) : Color.FromArgb(160, 160, 168));
            using (var fontTitle = new Font("Segoe UI", 8.2f, isActive ? FontStyle.Bold : FontStyle.Regular))
            {
                Rectangle rTitle = new Rectangle(1, circleY + circleD + 7, Width - 2, 20);
                TextRenderer.DrawText(g, title, fontTitle, rTitle, titleColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
            }

            // Texto: Subtítulo
            using (var fontSub = new Font("Segoe UI", 7.2f, FontStyle.Regular))
            {
                Rectangle rSub = new Rectangle(1, circleY + circleD + 26, Width - 2, 18);
                TextRenderer.DrawText(g, subtitle, fontSub, rSub, Color.FromArgb(120, 120, 128),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
            }
        }

        private void DrawModeIcon(Graphics g, int cx, int cy, Color col)
        {
            using (var pen = new Pen(col, 1.8f))
            using (var brush = new SolidBrush(col))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                // Silhueta central de auscultador
                g.FillEllipse(brush, cx - 4, cy - 6, 8, 8);
                g.FillRoundedRectangle(brush, cx, cy - 2, 3, 8, 1);

                if (mode == NoiseMode.Transparency)
                {
                    // Ondas de som abertas expandindo para fora (Transparência)
                    pen.DashStyle = DashStyle.Dash;
                    g.DrawArc(pen, cx - 14, cy - 12, 28, 24, 135, 90);
                    g.DrawArc(pen, cx - 14, cy - 12, 28, 24, -45, 90);
                }
                else if (mode == NoiseMode.NoiseCancellation)
                {
                    // Arcos de isolamento / bloqueio acústico protetor
                    pen.DashStyle = DashStyle.Solid;
                    g.DrawArc(pen, cx - 15, cy - 13, 30, 26, 120, 120);
                    g.DrawArc(pen, cx - 15, cy - 13, 30, 26, -60, 120);
                }
                else
                {
                    // Modo Desativado: Círculo fino neutro
                    using (var thinPen = new Pen(col, 1.3f))
                    {
                        g.DrawEllipse(thinPen, cx - 12, cy - 12, 24, 24);
                        g.DrawLine(thinPen, cx - 8, cy - 8, cx + 8, cy + 8);
                    }
                }
            }
        }
    }

    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.ResizeRedraw, true);
        }
    }

    public class RoundedPanel : Panel
    {
        public Color BorderColor { get; set; }
        public int CornerRadius { get; set; }

        public RoundedPanel()
        {
            BorderColor = Color.Transparent;
            CornerRadius = 12;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.SupportsTransparentBackColor |
                          ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color parentBg = Parent != null ? Parent.BackColor : Color.FromArgb(14, 15, 18);
            using (var clearBrush = new SolidBrush(parentBg))
            {
                g.FillRectangle(clearBrush, ClientRectangle);
            }

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var brush = new SolidBrush(BackColor))
            using (var pen = new Pen(BorderColor, 1f))
            {
                g.FillRoundedRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height, CornerRadius);
                if (BorderColor != Color.Transparent)
                {
                    g.DrawRoundedRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height, CornerRadius);
                }
            }
        }
    }

    /// <summary>
    /// Botão de controlo moderno para barra de cabeçalho com cantos arredondados e suporte anti-aliased.
    /// 100% compatível com C# 5 e .NET Framework 4.0+.
    /// </summary>
    public class ModernCaptionButton : Button
    {
        public int CornerRadius { get; set; }
        public Color HoverBackColor { get; set; }
        public Color PressedBackColor { get; set; }
        public Color NormalBackColor { get; set; }
        public Color BorderColor { get; set; }
        public Color NormalForeColor { get; set; }
        public Color HoverForeColor { get; set; }

        private bool isHovered = false;
        private bool isPressed = false;

        public ModernCaptionButton()
        {
            CornerRadius = 6;
            NormalBackColor = Color.Transparent;
            HoverBackColor = Color.FromArgb(45, 48, 58);
            PressedBackColor = Color.FromArgb(36, 38, 48);
            BorderColor = Color.Transparent;
            NormalForeColor = Color.FromArgb(192, 192, 200);
            HoverForeColor = Color.White;
            Cursor = Cursors.Default;

            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.SupportsTransparentBackColor |
                          ControlStyles.ResizeRedraw, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            isHovered = false;
            isPressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            if (mevent.Button == MouseButtons.Left)
            {
                isPressed = true;
                Invalidate();
            }
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            isPressed = false;
            Invalidate();
            base.OnMouseUp(mevent);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 1. SEMPRE limpar o fundo com a cor do painel pai para evitar qualquer artefacto/fantasma de transparência
            Color parentBg = Parent != null ? Parent.BackColor : Color.FromArgb(14, 15, 18);
            using (var clearBrush = new SolidBrush(parentBg))
            {
                g.FillRectangle(clearBrush, ClientRectangle);
            }

            // 2. Desenhar o fundo arredondado se não for transparente
            Color bg = isPressed ? PressedBackColor : (isHovered ? HoverBackColor : NormalBackColor);
            Color fg = isHovered ? HoverForeColor : NormalForeColor;

            if (bg != Color.Transparent)
            {
                Rectangle bgRect = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var brush = new SolidBrush(bg))
                {
                    GraphicsExtensions.FillRoundedRectangle(g, brush, bgRect.X, bgRect.Y, bgRect.Width, bgRect.Height, CornerRadius);
                }
            }

            if (BorderColor != Color.Transparent)
            {
                Rectangle bRect = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var pen = new Pen(BorderColor, 1f))
                {
                    GraphicsExtensions.DrawRoundedRectangle(g, pen, bRect.X, bRect.Y, bRect.Width, bRect.Height, CornerRadius);
                }
            }

            TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoClipping;
            TextRenderer.DrawText(g, this.Text, this.Font, ClientRectangle, fg, flags);
        }
    }
}
