using System;
using System.Globalization;
using Microsoft.Win32;
using MiBudsApp.Bluetooth;

namespace MiBudsApp.Common
{
    /// <summary>
    /// Idiomas suportados pela aplicação.
    /// Supported application languages.
    /// </summary>
    public enum Language
    {
        PT = 0,
        EN = 1
    }

    /// <summary>
    /// Sistema centralizado e modular de localização e definições para Xiaomi EarBuddy.
    /// Suporta troca de idioma em tempo de execução, persistência no Registro do Windows,
    /// e notificação de eventos para atualização instantânea da interface.
    /// 100% compatível com C# 5 e .NET Framework 4.0+.
    /// </summary>
    public static class Localization
    {
        private const string REG_SUBKEY = @"Software\MiEarbuddy";
        private const string REG_VAL_LANGUAGE = "Language";

        private static Language _currentLanguage;
        private static bool _initialized = false;
        private static readonly object _syncLock = new object();

        /// <summary>
        /// Evento disparado quando o idioma é alterado.
        /// Fired whenever the active language is changed.
        /// </summary>
        public static event EventHandler LanguageChanged;

        static Localization()
        {
            EnsureInitialized();
        }

        private static void EnsureInitialized()
        {
            if (!_initialized)
            {
                lock (_syncLock)
                {
                    if (!_initialized)
                    {
                        _currentLanguage = LoadLanguagePreference();
                        _initialized = true;
                    }
                }
            }
        }

        /// <summary>
        /// Obtém ou define o idioma atual.
        /// Ao definir, guarda automaticamente no Registro do Windows e notifica a UI via LanguageChanged.
        /// </summary>
        public static Language CurrentLanguage
        {
            get
            {
                EnsureInitialized();
                return _currentLanguage;
            }
            set
            {
                EnsureInitialized();
                if (_currentLanguage != value)
                {
                    lock (_syncLock)
                    {
                        _currentLanguage = value;
                        SaveLanguagePreference(value);
                    }
                    RaiseLanguageChanged();
                }
            }
        }

        /// <summary>
        /// Indica se o idioma ativo é o Português.
        /// </summary>
        public static bool IsPortuguese
        {
            get { return CurrentLanguage == Language.PT; }
        }

        /// <summary>
        /// Indicates whether the active language is English.
        /// </summary>
        public static bool IsEnglish
        {
            get { return CurrentLanguage == Language.EN; }
        }

        /// <summary>
        /// Alterna entre Português e Inglês, gravando no Registro e notificando a interface.
        /// Toggles between PT and EN, persists to registry, and notifies subscribers.
        /// </summary>
        public static void ToggleLanguage()
        {
            CurrentLanguage = (CurrentLanguage == Language.PT) ? Language.EN : Language.PT;
        }

        /// <summary>
        /// Define explicitamente o idioma e guarda a preferência no Registro.
        /// </summary>
        public static void SetLanguage(Language language)
        {
            CurrentLanguage = language;
        }

        private static void RaiseLanguageChanged()
        {
            EventHandler handler = LanguageChanged;
            if (handler != null)
            {
                try
                {
                    handler(null, EventArgs.Empty);
                }
                catch { }
            }
        }

        #region Persistência no Registro & Deteção do Sistema

        /// <summary>
        /// Carrega a preferência do utilizador a partir de HKCU\Software\MiEarbuddy\Language.
        /// Se não existir, utiliza o idioma do sistema (PT se português, EN se inglês, por defeito PT).
        /// </summary>
        private static Language LoadLanguagePreference()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REG_SUBKEY, false))
                {
                    if (key != null)
                    {
                        object val = key.GetValue(REG_VAL_LANGUAGE);
                        if (val != null)
                        {
                            string strVal = val.ToString().Trim();
                            if (strVal.Equals("EN", StringComparison.OrdinalIgnoreCase) || strVal == "1")
                            {
                                return Language.EN;
                            }
                            if (strVal.Equals("PT", StringComparison.OrdinalIgnoreCase) || strVal == "0")
                            {
                                return Language.PT;
                            }
                        }
                    }
                }
            }
            catch { }

            return DetectSystemDefaultLanguage();
        }

        /// <summary>
        /// Deteta o idioma padrão com base na cultura de interface do Windows.
        /// </summary>
        private static Language DetectSystemDefaultLanguage()
        {
            try
            {
                string cultureName = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                if (!string.IsNullOrEmpty(cultureName))
                {
                    if (cultureName.Equals("pt", StringComparison.OrdinalIgnoreCase))
                    {
                        return Language.PT;
                    }
                    if (cultureName.Equals("en", StringComparison.OrdinalIgnoreCase))
                    {
                        return Language.EN;
                    }
                }
            }
            catch { }

            return Language.PT;
        }

        /// <summary>
        /// Grava a preferência em HKCU\Software\MiEarbuddy\Language ("PT" ou "EN").
        /// </summary>
        private static void SaveLanguagePreference(Language language)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(REG_SUBKEY))
                {
                    if (key != null)
                    {
                        key.SetValue(REG_VAL_LANGUAGE, language.ToString());
                    }
                }
            }
            catch { }
        }

        #endregion

        #region Textos Gerais da Aplicação

        public static string AppName
        {
            get { return "Mi Earbuddy"; }
        }

        public static string DeviceTitlePlaceholder
        {
            get { return "Redmi Buds 5 Pro"; }
        }

        public static string DeviceTitleFallback
        {
            get { return "Mi Earbuds"; }
        }

        public static string FormTitle
        {
            get { return "Mi Earbuddy - Redmi Buds 5 Pro"; }
        }

        public static string CurrentLanguageCode
        {
            get { return IsPortuguese ? "PT" : "EN"; }
        }

        public static string CurrentLanguageDisplayName
        {
            get { return IsPortuguese ? "Português" : "English"; }
        }

        public static string ToggleLanguageMenuText
        {
            get { return IsPortuguese ? "Mudar para Inglês (EN)" : "Switch to Portuguese (PT)"; }
        }

        #endregion

        #region Estado da Ligação

        public static string StatusConnected
        {
            get { return IsPortuguese ? "● Conectado" : "● Connected"; }
        }

        public static string StatusConnecting
        {
            get { return IsPortuguese ? "● A conectar..." : "● Connecting..."; }
        }

        public static string StatusSearching
        {
            get { return IsPortuguese ? "● A procurar auscultadores..." : "● Searching for earphones..."; }
        }

        public static string StatusWaitingConnection
        {
            get { return IsPortuguese ? "● A aguardar ligação dos auscultadores..." : "● Waiting for earphones connection..."; }
        }

        public static string StatusReconnecting
        {
            get { return IsPortuguese ? "● Ligação perdida. A reconectar..." : "● Connection lost. Reconnecting..."; }
        }

        public static string StatusDisconnected
        {
            get { return IsPortuguese ? "● Desconectado" : "● Disconnected"; }
        }

        public static string StatusDisconnectedSuffix
        {
            get { return IsPortuguese ? "(Desligado)" : "(Disconnected)"; }
        }

        public static string StatusPrefix
        {
            get { return "● "; }
        }

        #endregion

        #region Modos de Ruído & Cabeçalho

        public static string NoiseControlHeader
        {
            get { return IsPortuguese ? "CONTROLO DE RUÍDO" : "NOISE CONTROL"; }
        }

        // Transparência / Transparency
        public static string TransparencyTitle
        {
            get { return IsPortuguese ? "Transparência" : "Transparency"; }
        }

        public static string TransparencySubtitle
        {
            get { return IsPortuguese ? "Ouvir ambiente" : "Hear surroundings"; }
        }

        public static string TransparencyShort
        {
            get { return "Transp."; }
        }

        public static string TransparencyMenuText
        {
            get { return IsPortuguese ? "🔊 Transparência" : "🔊 Transparency"; }
        }

        // Cancelamento de Ruído / Noise Cancellation
        public static string NoiseCancellationTitle
        {
            get { return IsPortuguese ? "Cancelamento" : "Cancellation"; }
        }

        public static string NoiseCancellationSubtitle
        {
            get { return IsPortuguese ? "Isolar ruídos" : "Block noise"; }
        }

        public static string NoiseCancellationShort
        {
            get { return "ANC"; }
        }

        public static string NoiseCancellationMenuText
        {
            get { return IsPortuguese ? "🔇 Cancelamento de Ruído" : "🔇 Noise Cancellation"; }
        }

        // Desativado / Off
        public static string OffTitle
        {
            get { return IsPortuguese ? "Desativado" : "Off"; }
        }

        public static string OffSubtitle
        {
            get { return IsPortuguese ? "Desligado" : "Disabled"; }
        }

        public static string OffShort
        {
            get { return IsPortuguese ? "Desat." : "Off"; }
        }

        public static string OffMenuText
        {
            get { return IsPortuguese ? "⭕ Desativado" : "⭕ Off"; }
        }

        #endregion

        #region Bateria

        public static string BatteryLeft
        {
            get { return IsPortuguese ? "Esq" : "Left"; }
        }

        public static string BatteryCase
        {
            get { return IsPortuguese ? "Caixa" : "Case"; }
        }

        public static string BatteryRight
        {
            get { return IsPortuguese ? "Dir" : "Right"; }
        }

        public static string BatteryCharging
        {
            get { return IsPortuguese ? "⚡ A carregar" : "⚡ Charging"; }
        }

        public static string BatteryChargingSymbol
        {
            get { return "⚡"; }
        }

        public static string BatteryUnknown
        {
            get { return "--"; }
        }

        public static string BatteryReading
        {
            get { return IsPortuguese ? "A ler..." : "Reading..."; }
        }

        public static string TrayBatteryReadingText
        {
            get { return IsPortuguese ? "🔋 Bateria: A ler..." : "🔋 Battery: Reading..."; }
        }

        #endregion

        #region Rodapé & Botões de Ação

        public static string BtnMinimize
        {
            get { return IsPortuguese ? "📥 Minimizar" : "📥 Minimize"; }
        }

        public static string BtnRefresh
        {
            get { return IsPortuguese ? "🔄 Atualizar" : "🔄 Refresh"; }
        }

        public static string BtnResetSizeTooltip
        {
            get { return IsPortuguese ? "Repor tamanho padrão" : "Reset default size"; }
        }

        public static string HintHoverTray
        {
            get
            {
                return IsPortuguese
                    ? "Dica: Passa o rato no ícone junto ao relógio para ver a bateria rápida."
                    : "Tip: Hover over the tray icon next to the clock for quick battery info.";
            }
        }

        public static string TechInfo
        {
            get
            {
                return IsPortuguese
                    ? "0.0% CPU | ~25 MB RAM | Bluetooth Nativo"
                    : "0.0% CPU | ~25 MB RAM | Native Bluetooth";
            }
        }

        #endregion

        #region Menu de Contexto do Tray & Tooltip

        public static string TrayNoiseHeader
        {
            get { return IsPortuguese ? "MODO DE RUÍDO" : "NOISE MODE"; }
        }

        public static string TrayStartWithWindows
        {
            get { return IsPortuguese ? "Iniciar no arranque" : "Start with Windows"; }
        }

        public static string TrayExit
        {
            get { return IsPortuguese ? "Sair" : "Exit"; }
        }

        public static string TrayOpenPanel
        {
            get { return IsPortuguese ? "Abrir Painel" : "Open Dashboard"; }
        }

        public static string TrayModeLabel
        {
            get { return IsPortuguese ? "Modo" : "Mode"; }
        }

        #endregion

        #region Métodos Auxiliares de Formatação

        /// <summary>
        /// Obtém o título do modo de ruído (para os botões do formulário).
        /// </summary>
        public static string GetNoiseModeTitle(NoiseMode mode)
        {
            switch (mode)
            {
                case NoiseMode.Transparency:
                    return TransparencyTitle;
                case NoiseMode.NoiseCancellation:
                    return NoiseCancellationTitle;
                case NoiseMode.Off:
                default:
                    return OffTitle;
            }
        }

        /// <summary>
        /// Obtém o subtítulo do modo de ruído (para os botões do formulário).
        /// </summary>
        public static string GetNoiseModeSubtitle(NoiseMode mode)
        {
            switch (mode)
            {
                case NoiseMode.Transparency:
                    return TransparencySubtitle;
                case NoiseMode.NoiseCancellation:
                    return NoiseCancellationSubtitle;
                case NoiseMode.Off:
                default:
                    return OffSubtitle;
            }
        }

        /// <summary>
        /// Obtém a abreviação do modo de ruído (ex.: "Transp.", "ANC", "Desat." / "Off").
        /// </summary>
        public static string GetNoiseModeShort(NoiseMode mode)
        {
            switch (mode)
            {
                case NoiseMode.Transparency:
                    return TransparencyShort;
                case NoiseMode.NoiseCancellation:
                    return NoiseCancellationShort;
                case NoiseMode.Off:
                default:
                    return OffShort;
            }
        }

        /// <summary>
        /// Obtém o texto do item de menu de contexto da bandeja.
        /// </summary>
        public static string GetNoiseModeMenuText(NoiseMode mode)
        {
            switch (mode)
            {
                case NoiseMode.Transparency:
                    return TransparencyMenuText;
                case NoiseMode.NoiseCancellation:
                    return NoiseCancellationMenuText;
                case NoiseMode.Off:
                default:
                    return OffMenuText;
            }
        }

        /// <summary>
        /// Formata o texto de percentagem dos badges de bateria (ex: "85%", "85% ⚡", "⚡ A carregar", "--").
        /// </summary>
        public static string FormatBatteryBadgeText(int percent, bool charging)
        {
            if (percent >= 0 && percent <= 100)
            {
                string text = percent.ToString() + "%";
                if (charging)
                {
                    text += " ⚡";
                }
                return text;
            }
            if (charging)
            {
                return BatteryCharging;
            }
            return BatteryUnknown;
        }

        /// <summary>
        /// Formata a linha de resumo da bateria no menu de contexto da bandeja.
        /// Exemplo: "🔋 Esq: 80% | Caixa: 100% | Dir: 80%" ou "🔋 Left: 80% | Case: 100% | Right: 80%"
        /// </summary>
        public static string FormatTrayBatteryLine(string left, string caseBat, string right)
        {
            return string.Format(
                IsPortuguese ? "🔋 Esq: {0} | Caixa: {1} | Dir: {2}" : "🔋 Left: {0} | Case: {1} | Right: {2}",
                left, caseBat, right);
        }

        /// <summary>
        /// Formata o título do dispositivo no menu de contexto da bandeja.
        /// Exemplo: "Redmi Buds 5 Pro" ou "Redmi Buds 5 Pro (Desligado)"
        /// </summary>
        public static string FormatTrayDeviceTitle(string deviceName, bool isConnected)
        {
            if (string.IsNullOrEmpty(deviceName))
            {
                deviceName = DeviceTitlePlaceholder;
            }

            if (isConnected)
            {
                return deviceName;
            }
            else
            {
                return deviceName + " " + StatusDisconnectedSuffix;
            }
        }

        /// <summary>
        /// Formata o texto do tooltip do NotifyIcon na bandeja, respeitando rigorosamente o limite de 63 caracteres do Windows.
        /// Formato: "{NomeDispositivo}\n{ResumoBateria}\n{Modo/Mode}: {ModoRuido}"
        /// </summary>
        public static string FormatTrayTooltip(string deviceName, string batterySummary, string modeSummary)
        {
            if (string.IsNullOrEmpty(deviceName))
            {
                deviceName = DeviceTitlePlaceholder;
            }

            string label = TrayModeLabel;
            string tip = string.Format("{0}\n{1}\n{2}: {3}", deviceName, batterySummary, label, modeSummary);
            if (tip.Length > 63)
            {
                tip = tip.Substring(0, 63);
            }
            return tip;
        }

        /// <summary>
        /// Formata o texto do tooltip diretamente a partir do estado de bateria e modo de ruído.
        /// Respeita rigorosamente o limite de 63 caracteres do NotifyIcon.
        /// </summary>
        public static string FormatTrayTooltip(string deviceName, BatteryState battery, NoiseMode mode)
        {
            if (string.IsNullOrEmpty(deviceName))
            {
                deviceName = DeviceTitlePlaceholder;
            }

            string modeStr = GetNoiseModeShort(mode);

            string lStr = (battery != null && battery.Left >= 0 && battery.Left <= 100)
                ? battery.Left.ToString() + "%" + (battery.LeftCharging ? "⚡" : "")
                : "--";
            string rStr = (battery != null && battery.Right >= 0 && battery.Right <= 100)
                ? battery.Right.ToString() + "%" + (battery.RightCharging ? "⚡" : "")
                : "--";
            string cStr = (battery != null && battery.Case >= 0 && battery.Case <= 100)
                ? battery.Case.ToString() + "%" + (battery.CaseCharging ? "⚡" : "")
                : "--";

            string batStr = string.Format("L:{0} C:{1} R:{2}", lStr, cStr, rStr);
            return FormatTrayTooltip(deviceName, batStr, modeStr);
        }

        #endregion
    }
}
