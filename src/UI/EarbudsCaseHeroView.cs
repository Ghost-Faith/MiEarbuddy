using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using MiBudsApp.Bluetooth;
using MiBudsApp.Common;

namespace MiBudsApp.UI
{
    /// <summary>
    /// Visualizador central realista do estojo e auscultadores com imagem de alta fidelidade
    /// e indicadores de bateria tipo cápsula com código de cores e modo de carga.
    /// 100% compatível com C# 5 e Windows .NET Framework 4.0+.
    /// </summary>
    public class EarbudsCaseHeroView : Panel
    {
        public Color BorderColor { get; set; }
        public int CornerRadius { get; set; }

        private int leftBatt = -1;
        private bool leftChg = false;
        private int rightBatt = -1;
        private bool rightChg = false;
        private int caseBatt = -1;
        private bool caseChg = false;
        private bool isConnected = false;

        private Image caseImage = null;
        private bool imageLoadAttempted = false;
        private readonly object imageLock = new object();

        // Cores de bateria oficiais Xiaomi HyperOS / iOS Modern
        private static readonly Color ColBatteryGreen = Color.FromArgb(52, 199, 89);
        private static readonly Color ColBatteryOrange = Color.FromArgb(255, 149, 0);
        private static readonly Color ColBatteryRed = Color.FromArgb(255, 69, 58);
        private static readonly Color ColBatteryMuted = Color.FromArgb(120, 120, 128);

        // Cores dos cartões de bateria
        private static readonly Color ColBadgeBg = Color.FromArgb(30, 31, 38);
        private static readonly Color ColBadgeBorder = Color.FromArgb(44, 46, 56);
        private static readonly Color ColCapsuleBorder = Color.FromArgb(90, 93, 105);

        public EarbudsCaseHeroView()
        {
            CornerRadius = 16;
            BorderColor = Color.FromArgb(38, 40, 48);
            BackColor = Color.FromArgb(24, 25, 30);

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            try
            {
                Localization.LanguageChanged += OnLanguageChanged;
            }
            catch { }
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            if (IsHandleCreated && !IsDisposed)
            {
                if (InvokeRequired)
                {
                    try { BeginInvoke(new Action(Invalidate)); } catch { }
                }
                else
                {
                    Invalidate();
                }
            }
        }

        public void SetConnected(bool connected)
        {
            this.isConnected = connected;
            if (!connected)
            {
                ResetBattery();
            }
            else
            {
                Invalidate();
            }
        }

        public void ResetBattery()
        {
            this.leftBatt = -1;
            this.leftChg = false;
            this.rightBatt = -1;
            this.rightChg = false;
            this.caseBatt = -1;
            this.caseChg = false;
            this.isConnected = false;
            Invalidate();
        }

        public void UpdateBattery(BatteryState state)
        {
            if (state == null) return;
            if (!state.HasData)
            {
                ResetBattery();
                return;
            }
            this.leftBatt = state.Left;
            this.leftChg = state.LeftCharging;
            this.rightBatt = state.Right;
            this.rightChg = state.RightCharging;
            this.caseBatt = state.Case;
            this.caseChg = state.CaseCharging;
            this.isConnected = true;
            Invalidate();
        }

        public void UpdateBattery(int left, bool leftChg, int right, bool rightChg, int caseBatt, bool caseChg)
        {
            this.leftBatt = left;
            this.leftChg = leftChg;
            this.rightBatt = right;
            this.rightChg = rightChg;
            this.caseBatt = caseBatt;
            this.caseChg = caseChg;
            this.isConnected = true;
            Invalidate();
        }

        private Image LoadCaseImage()
        {
            if (caseImage != null) return caseImage;
            if (imageLoadAttempted) return null;

            lock (imageLock)
            {
                if (caseImage != null) return caseImage;
                if (imageLoadAttempted) return null;
                imageLoadAttempted = true;

                // 1. Tentar carregar a partir dos recursos embutidos (assembly manifest)
                try
                {
                    Assembly asm = Assembly.GetExecutingAssembly();
                    string[] resNames = asm.GetManifestResourceNames();
                    string matchedRes = null;
                    if (resNames != null)
                    {
                        for (int i = 0; i < resNames.Length; i++)
                        {
                            if (resNames[i].EndsWith("case.png", StringComparison.OrdinalIgnoreCase))
                            {
                                matchedRes = resNames[i];
                                break;
                            }
                        }
                    }

                    if (matchedRes != null)
                    {
                        using (Stream s = asm.GetManifestResourceStream(matchedRes))
                        {
                            if (s != null)
                            {
                                using (Image temp = Image.FromStream(s))
                                {
                                    caseImage = new Bitmap(temp);
                                    return caseImage;
                                }
                            }
                        }
                    }
                }
                catch { }

                // 2. Fallback: procurar ficheiro case.png no diretório da app
                try
                {
                    string[] searchPaths = new string[]
                    {
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "case.png"),
                        Path.Combine(Application.StartupPath, "case.png"),
                        "case.png"
                    };

                    for (int i = 0; i < searchPaths.Length; i++)
                    {
                        if (File.Exists(searchPaths[i]))
                        {
                            byte[] bytes = File.ReadAllBytes(searchPaths[i]);
                            using (MemoryStream ms = new MemoryStream(bytes))
                            using (Image temp = Image.FromStream(ms))
                            {
                                caseImage = new Bitmap(temp);
                                return caseImage;
                            }
                        }
                    }
                }
                catch { }

                return null;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            int w = Width;
            int h = Height;
            if (w <= 0 || h <= 0) return;

            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 0. Desenhar o cartão de fundo com cantos arredondados
            Color parentBg = Parent != null ? Parent.BackColor : Color.FromArgb(14, 15, 18);
            using (var clearBrush = new SolidBrush(parentBg))
            {
                g.FillRectangle(clearBrush, ClientRectangle);
            }

            Rectangle cardRect = new Rectangle(0, 0, w - 1, h - 1);
            using (var cardPath = CreateRoundedRectPath(cardRect, CornerRadius))
            using (var cardBrush = new SolidBrush(BackColor))
            using (var cardPen = new Pen(BorderColor != Color.Transparent ? BorderColor : Color.FromArgb(38, 40, 48), 1f))
            {
                g.FillPath(cardBrush, cardPath);
                g.DrawPath(cardPen, cardPath);
            }

            // 1. Layout dos Badges de Bateria (Inferior)
            int badgeH = 52;
            int badgeY = Math.Max(12, h - badgeH - 12);
            int margin = 12;
            int gap = 8;
            int availableW = w - (margin * 2) - (gap * 2);
            int badgeW = Math.Max(20, availableW / 3);

            // 2. Desenhar a Imagem do Estojo (Superior / Centralizado)
            int maxImgH = Math.Max(60, badgeY - 14);
            int targetImgSize = Math.Min(155, Math.Min(w - 40, maxImgH));
            if (targetImgSize < 60) targetImgSize = 60;

            Image img = LoadCaseImage();
            int imgX = (w - targetImgSize) / 2;
            int imgY = Math.Max(6, (badgeY - targetImgSize) / 2);

            // Sombra suave sob o estojo
            using (var shadowBrush = new SolidBrush(Color.FromArgb(28, 0, 0, 0)))
            {
                int shadowW = (int)(targetImgSize * 0.88);
                int shadowH = Math.Max(12, (int)(targetImgSize * 0.16));
                int shadowX = (w - shadowW) / 2;
                int shadowY = imgY + targetImgSize - shadowH / 2;
                g.FillEllipse(shadowBrush, shadowX, shadowY, shadowW, shadowH);
            }

            if (img != null)
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;

                g.DrawImage(img, new Rectangle(imgX, imgY, targetImgSize, targetImgSize));
            }
            else
            {
                // Fallback elegante caso a imagem não possa ser carregada
                DrawProceduralCaseFallback(g, w / 2, imgY + targetImgSize / 2, targetImgSize);
            }

            // 3. Ordem obrigatória dos badges: Esq (Left) | Caixa (Case) | Dir (Right)
            string lblLeft = Localization.BatteryLeft;
            string lblCase = Localization.BatteryCase;
            string lblRight = Localization.BatteryRight;

            int xLeft = margin;
            int xCase = margin + badgeW + gap;
            int xRight = margin + (badgeW + gap) * 2;

            // Alargar o último badge para compensar arredondamento de divisão inteira
            int lastBadgeW = w - margin - xRight;
            if (lastBadgeW < badgeW) lastBadgeW = badgeW;

            DrawBatteryBadge(g, xLeft, badgeY, badgeW, badgeH, lblLeft, leftBatt, leftChg);
            DrawBatteryBadge(g, xCase, badgeY, badgeW, badgeH, lblCase, caseBatt, caseChg);
            DrawBatteryBadge(g, xRight, badgeY, lastBadgeW, badgeH, lblRight, rightBatt, rightChg);
        }

        private void DrawProceduralCaseFallback(Graphics g, int cx, int cy, int size)
        {
            int caseW = (int)(size * 0.95);
            int caseH = (int)(size * 0.65);
            Rectangle rect = new Rectangle(cx - caseW / 2, cy - caseH / 2, caseW, caseH);

            using (var path = CreateRoundedRectPath(rect, 24))
            using (var brush = new LinearGradientBrush(rect, Color.FromArgb(34, 35, 40), Color.FromArgb(18, 19, 22), 90f))
            using (var pen = new Pen(Color.FromArgb(50, 52, 60), 1.5f))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }

            // LED de estado
            Color ledColor = isConnected ? Color.FromArgb(240, 240, 255) : Color.FromArgb(28, 140, 255);
            using (var ledPen = new Pen(ledColor, 2f))
            {
                g.DrawLine(ledPen, cx - 12, cy, cx + 12, cy);
            }
        }

        /// <summary>
        /// Desenha um cartão de bateria (badge) com etiqueta, cápsula gráfica realista com terminal e percentagem nítida.
        /// </summary>
        private void DrawBatteryBadge(Graphics g, int x, int y, int width, int height, string label, int percent, bool charging)
        {
            Rectangle rect = new Rectangle(x, y, width, height);

            // Fundo pill do badge
            using (var path = CreateRoundedRectPath(rect, 10))
            using (var brush = new SolidBrush(ColBadgeBg))
            using (var pen = new Pen(charging ? Color.FromArgb(40, ColBatteryGreen) : ColBadgeBorder, 1f))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }

            // Determinar cor do estado da bateria
            Color statusColor;
            if (charging)
            {
                statusColor = ColBatteryGreen;
            }
            else if (percent > 50)
            {
                statusColor = ColBatteryGreen;
            }
            else if (percent >= 21)
            {
                statusColor = ColBatteryOrange;
            }
            else if (percent >= 0)
            {
                statusColor = ColBatteryRed;
            }
            else
            {
                statusColor = ColBatteryMuted;
            }

            // Construir texto da percentagem
            string textPercent;
            if (percent >= 0 && percent <= 100)
            {
                textPercent = percent.ToString() + "%" + (charging ? " ⚡" : "");
            }
            else if (charging)
            {
                textPercent = "⚡";
            }
            else
            {
                textPercent = Localization.BatteryUnknown;
            }

            // 1. Linha superior: Etiqueta (Esq / Caixa / Dir ou Left / Case / Right)
            using (var fontLbl = new Font("Segoe UI", 8f, FontStyle.Regular))
            {
                Rectangle rLbl = new Rectangle(x + 4, y + 5, width - 8, 16);
                TextRenderer.DrawText(g, label, fontLbl, rLbl, Color.FromArgb(150, 153, 165),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            }

            // 2. Linha inferior: Cápsula gráfica + Percentagem em 10pt Negrito
            using (var fontNum = new Font("Segoe UI", 10f, FontStyle.Bold))
            {
                Size textSize = TextRenderer.MeasureText(textPercent, fontNum);

                // Dimensões da cápsula gráfica
                int capsuleBodyW = 22;
                int capsuleBodyH = 12;
                int nippleW = 2;
                int nippleH = 6;
                int totalCapsuleW = capsuleBodyW + nippleW + 1;
                int gapCapsuleText = 6;

                int rowContentW = totalCapsuleW + gapCapsuleText + textSize.Width;
                int startX = x + Math.Max(4, (width - rowContentW) / 2);

                int capsuleY = y + 25 + (20 - capsuleBodyH) / 2;

                // Desenhar a cápsula de bateria
                DrawMiniBatteryCapsule(g, startX, capsuleY, capsuleBodyW, capsuleBodyH, nippleW, nippleH, percent, charging, statusColor);

                // Desenhar o texto da percentagem com contraste elevado
                Rectangle rNum = new Rectangle(startX + totalCapsuleW + gapCapsuleText, y + 23, width - (startX + totalCapsuleW + gapCapsuleText - x) - 2, 24);
                Color textColor = (percent >= 0 || charging) ? Color.FromArgb(248, 248, 252) : ColBatteryMuted;
                if (charging) textColor = ColBatteryGreen;

                TextRenderer.DrawText(g, textPercent, fontNum, rNum, textColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            }
        }

        /// <summary>
        /// Desenha uma cápsula de bateria com corpo arredondado, terminal (nipple) e nível interno de preenchimento.
        /// </summary>
        private void DrawMiniBatteryCapsule(Graphics g, int x, int y, int bodyW, int bodyH, int nippleW, int nippleH, int percent, bool charging, Color statusColor)
        {
            Rectangle bodyRect = new Rectangle(x, y, bodyW, bodyH);

            // Contorno da cápsula
            Color penColor = (percent >= 0 || charging) ? ColCapsuleBorder : Color.FromArgb(65, 68, 78);
            using (var pathBody = CreateRoundedRectPath(bodyRect, 2))
            using (var penBody = new Pen(penColor, 1.2f))
            {
                g.DrawPath(penBody, pathBody);
            }

            // Terminal (nipple) no lado direito
            int nippleX = x + bodyW;
            int nippleY = y + (bodyH - nippleH) / 2;
            Rectangle nippleRect = new Rectangle(nippleX, nippleY, nippleW, nippleH);
            using (var pathNipple = CreateRoundedRectPath(nippleRect, 1))
            using (var brushNipple = new SolidBrush(penColor))
            {
                g.FillPath(brushNipple, pathNipple);
            }

            // Preenchimento interno conforme a percentagem
            if (percent > 0)
            {
                int maxInnerW = bodyW - 4;
                int innerH = bodyH - 4;
                int fillW = (int)Math.Round((double)maxInnerW * Math.Min(percent, 100) / 100.0);
                if (fillW < 2) fillW = 2;

                Rectangle fillRect = new Rectangle(x + 2, y + 2, fillW, innerH);
                using (var pathFill = CreateRoundedRectPath(fillRect, 1))
                using (var brushFill = new SolidBrush(statusColor))
                {
                    g.FillPath(brushFill, pathFill);
                }
            }
        }

        private static GraphicsPath CreateRoundedRectPath(Rectangle bounds, int radius)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                GraphicsPath p = new GraphicsPath();
                p.AddRectangle(bounds);
                return p;
            }

            int diameter = radius * 2;
            if (diameter > bounds.Width) diameter = bounds.Width;
            if (diameter > bounds.Height) diameter = bounds.Height;

            if (diameter <= 0 || radius <= 0)
            {
                GraphicsPath path = new GraphicsPath();
                path.AddRectangle(bounds);
                return path;
            }

            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath gPath = new GraphicsPath();

            gPath.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            gPath.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            gPath.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            gPath.AddArc(arc, 90, 90);
            gPath.CloseFigure();
            return gPath;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    Localization.LanguageChanged -= OnLanguageChanged;
                }
                catch { }

                lock (imageLock)
                {
                    if (caseImage != null)
                    {
                        try { caseImage.Dispose(); } catch { }
                        caseImage = null;
                    }
                }
            }
            base.Dispose(disposing);
        }
    }
}
