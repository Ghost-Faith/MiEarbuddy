/**
 * MiEarbuddy — Internationalization (i18n) Module
 * Authentic bilingual experience: English (EN) and Portuguese (PT)
 * Mirrors the native Windows desktop application Localization.cs
 */

const translations = {
  en: {
    // Banner
    bannerText: "MiEarbuddy 1.0 is now available for Windows 11 & 10. Pure C# • 0.0% CPU.",
    bannerLink: "Download v1.0.0 (.exe) >",

    // Navigation
    navOverview: "Overview",
    navShowcase: "Showcase",
    navNoiseControl: "Noise Control",
    navBattery: "Battery",
    navTray: "System Tray",
    navPerformance: "Performance",
    navHowItWorks: "How It Works",
    navCompatibility: "Compatibility",
    navFaq: "FAQ",
    navDownload: "Download",

    // Hero Section
    heroBadge: "Windows Desktop Companion • Pure Native C#",
    heroTitlePart1: "Your earbuds.",
    heroTitlePart2: "Your control.",
    heroSubtitle: "A lightweight Windows companion that brings intelligent control, real-time battery monitoring and seamless earbud management to your desktop.",
    heroBtnDownload: "Download for Windows",
    heroBtnGithub: "View on GitHub",
    heroRevealHint: "✦ Click or hover to reveal full title",
    heroSpecWin: "Windows 11 & 10",
    heroSpecNative: "Pure C# Native",
    heroSpecCpu: "0.0% CPU Idle",
    heroSpecRam: "~25 MB RAM",

    // Showcase
    showcaseHeading: "Engineered for Windows.<br><span style=\"color: var(--text-silver);\">Designed with HyperOS Precision.</span>",
    showcaseSubtitle: "MiEarbuddy bridges the gap between Xiaomi's refined mobile design and native Windows desktop architecture.",
    showcaseWinTitle: "Mi Earbuddy - Redmi Buds 5 Pro",
    showcaseStatus: "Connected",
    showcaseHotspot1Title: "Responsive Adaptive Header",
    showcaseHotspot1Desc: "Fluid title bar controls, instant language toggle (EN/PT), and clean custom caption buttons (↺ Reset, ─ Minimize, ✕ Hide to Tray).",
    showcaseHotspot2Title: "GDI+ Real-Time Battery Capsules",
    showcaseHotspot2Desc: "Triple-cell FastPair monitoring rendered natively with dynamic color transitions and charging lightning indicators.",
    showcaseHotspot3Title: "Direct Noise Control Architecture",
    showcaseHotspot3Desc: "One-click switching between Active Noise Cancellation, Transparency Mode, and Off Mode via RFCOMM sockets.",
    showcaseHotspot4Title: "Smart System Tray Minimization",
    showcaseHotspot4Desc: "Minimizes instantly next to your Windows clock. Zero CPU overhead during background standby.",

    // Active Noise Cancellation
    ancHeading: "Up to 52 dB of silence.<br><span style=\"color: var(--apple-blue);\">At your fingertips.</span>",
    ancSubtitle: "Command earbud DSP hardware directly over Winsock RFCOMM sockets with zero audio stutter.",
    ancMode1Title: "Active Noise Cancellation",
    ancMode2Title: "Transparency Mode",
    ancMode3Title: "Off Mode",
    ancCaptionLead1: "Active Noise Cancellation:",
    ancCaptionBody1: "Anti-phase acoustic frequencies actively canceling external background hum (-52 dB).",
    ancCaptionLead2: "Transparency Mode:",
    ancCaptionBody2: "Environmental frequencies passing through wideband acoustic microphones with natural clarity.",
    ancCaptionLead3: "Off Mode:",
    ancCaptionBody3: "DSP filtering bypassed. Standard passive acoustic isolation engaged.",
    ancVisualizerStatus1: "Anti-phase acoustic frequencies actively canceling external background hum (-52 dB).",
    ancVisualizerStatus2: "Environmental frequencies passing through wideband acoustic microphones with natural clarity.",
    ancVisualizerStatus3: "DSP filtering bypassed. Standard passive acoustic isolation engaged.",
    ancMode1Desc: "Deep active noise suppression. Eliminates low-frequency engine, fan, and commute hum for pure acoustic focus.",
    ancMode2Desc: "Natural ambient awareness. Blends surrounding voices and environment seamlessly without removing your earbuds.",
    ancMode3Desc: "Standard passive acoustic seal. Conserves earbud battery while maintaining natural physical isolation.",
    ancAntiBounceTitle: "Smart Anti-Bounce Filter:",
    ancAntiBounceDesc: "Prevents audio stutter and suppresses transient dual-earbud sync echoes, ensuring mode transitions are instantaneous with a single clean earbud chime.",

    // Battery Section
    battHeading: "Every bud. The case.<br><span style=\"color: var(--status-green);\">Always in sight.</span>",
    battSubtitle: "Never get surprised by an empty earbud. Live battery telemetry decoded instantly in real time.",
    battLeft: "Left Earbud",
    battCase: "Charging Case",
    battRight: "Right Earbud",
    battCharging: "Charging",
    battReady: "In Use",
    battFooterNote: "Decodes Google FastPair telemetry beacons in real time. Dynamic color thresholds warn you when an earbud reaches 20%, and lightning indicators verify charging status instantly.",

    // System Tray
    trayHeading: "Always ready.<br><span style=\"color: var(--text-silver);\">Never in your way.</span>",
    traySubtitle: "MiEarbuddy resides quietly in your Windows Notification Area, giving you instant access without cluttering your taskbar.",
    trayPreviewCaption: "Dedicated Windows System Tray Context Menu",
    trayBlock1Title: "Real-Time Hover Tooltip",
    trayBlock1Desc: "Hovering over the tray icon next to your Windows clock instantly reveals current earbud model, individual battery percentages, and active noise mode.",
    trayBlock2Title: "One-Click Noise Mode Switching",
    trayBlock2Desc: "Toggle Noise Cancellation, Transparency Mode, or Off directly from the notification area without opening the main window.",
    trayBlock3Title: "Start with Windows",
    trayBlock3Desc: "Integrated with Windows Registry (Run key) and -tray launch argument for completely silent, unobtrusive background startup.",
    trayTaskbarSim: "Windows 11 Notification Area",

    // Performance Section
    perfHeading: "0.0% CPU.<br>~25 MB RAM.<br><span style=\"color: var(--apple-blue);\">Zero Electron.</span>",
    perfSubtitle: "Built with pure C# and Windows GDI+. No web runtimes, no Node.js, no WebView2, and no Python.",
    perfStat1Label: "RAM Footprint",
    perfStat1Desc: "95% less memory than modern Electron utilities.",
    perfStat2Label: "CPU at Idle",
    perfStat2Desc: "Zero background consumption while in system tray.",
    perfStat4Label: "Single Executable",
    perfStat4Desc: "Ultra-compact standalone portable binary.",
    perfStat5Label: "Instant Startup",
    perfStat5Desc: "Instantaneous execution with no framework lag.",
    arch1Title: "Winsock RFCOMM (ws2_32.dll)",
    arch1Desc: "Direct low-level communication with Windows Bluetooth sockets, eliminating high-latency driver wrappers.",
    arch2Title: "E21 Cryptographic Handshake",
    arch2Desc: "Reverse-engineered mutual challenge authentication protocol ensuring secure pairing with Xiaomi hardware.",
    arch3Title: "FastPair Telemetry Decoder",
    arch3Desc: "Extracts individual Left, Right, and Case battery percentages from Google FastPair beacons in real time.",
    arch4Title: "Single-Instance Mutex Guard",
    arch4Desc: "Prevents duplicate background processes, automatically bringing the active instance to the foreground.",

    // How It Works
    howTitlePart1: "Simple Setup.",
    howTitlePart2: "Seamless Operation.",
    howSubtitle: "Four simple steps to bring your Xiaomi and Redmi earbuds to your Windows desktop.",
    step1Title: "Download MiEarbuddy",
    step1Desc: "Download the standalone single executable (~680 KB). No installer, wizard, or admin permissions required.",
    step2Title: "Connect via Windows",
    step2Desc: "Pair your Xiaomi or Redmi earbuds to your PC using the standard Windows Bluetooth settings menu.",
    step3Title: "Launch & Authenticate",
    step3Desc: "Run MiEarbuddy. It automatically discovers paired earbuds and performs the secure E21 cryptographic handshake.",
    step4Title: "Enjoy Full Control",
    step4Desc: "Monitor triple-cell battery levels and toggle Active Noise Cancellation directly from your desktop or system tray.",

    // Compatibility
    compatTitle: "Compatibility & Requirements",
    compatSubtitle: "Engineered specifically for Xiaomi and Redmi audio hardware on modern Windows operating systems.",
    compatBudsTitle: "Verified Hardware: Redmi Buds 5 Pro",
    compatBudsDesc: "Specifically developed and verified on Redmi Buds 5 Pro. Compatible with Xiaomi and Redmi earbuds supporting Xiaomi MMA and FastPair RFCOMM protocol.",
    compatCheck1: "Active Noise Cancellation (ANC), Transparency, and Off modes",
    compatCheck2: "Individual Left Bud, Right Bud, and Charging Case battery monitoring",
    compatCheck3: "Smart Anti-Bounce filter for smooth acoustic transitions",
    compatCheck4: "Start with Windows background auto-startup",
    compatSysReqTitle: "System Requirements",
    compatReqOsTitle: "Operating System:",
    compatReqOsDesc: "Windows 11 (all editions) or Windows 10 (version 1809+).",
    compatReqNetTitle: ".NET Framework:",
    compatReqNetDesc: ".NET Framework 4.0 or newer (built into Windows by default).",
    compatReqBtTitle: "Bluetooth Hardware:",
    compatReqBtDesc: "Bluetooth 4.0+ adapter supporting RFCOMM serial connection.",
    compatReqPermTitle: "User Permissions:",
    compatReqPermDesc: "Standard user account (no Administrator elevation required).",

    // Download Section
    dlTitlePart1: "Take control of your",
    dlTitlePart2: "listening experience.",
    dlSubtitle: "Get MiEarbuddy and bring your earbuds closer to your Windows desktop.",
    dlBtnExe: "Download MiEarbuddy.exe",
    dlBtnRepo: "GitHub Repository",
    dlMetaVersion: "Version 1.0.0 • Standalone Executable (~680 KB)",
    dlMetaLicense: "Open Source • MIT License",

    // FAQ Section
    faqTitle: "Frequently Asked Questions",
    faqSubtitle: "Everything you need to know about MiEarbuddy, its architecture, and compatibility.",
    faq1Q: "What is MiEarbuddy?",
    faq1A: "MiEarbuddy is a native, ultra-lightweight Windows desktop companion designed specifically for Xiaomi and Redmi earbuds. It provides real-time triple-cell battery monitoring (Left Bud, Case, Right Bud), seamless Active Noise Cancellation (ANC) control, and full Windows System Tray integration.",
    faq2Q: "Which earbuds are officially supported?",
    faq2A: "The application is officially verified and hardware-tested on the Redmi Buds 5 Pro. It also communicates using standard Xiaomi MMA (Mobile Media Application) RFCOMM protocols and Google FastPair telemetry, meaning other Xiaomi and Redmi earbuds using this protocol family can connect.",
    faq3Q: "Is MiEarbuddy free and open source?",
    faq3A: "Yes. MiEarbuddy is 100% free and open-source software released under the permissive MIT License. You are free to inspect the source code, modify it, and compile it yourself.",
    faq4Q: "Does it work on Windows 11 and Windows 10?",
    faq4A: "Yes, MiEarbuddy fully supports both Windows 11 and Windows 10 (version 1809 and newer). On Windows 11, it automatically enables native Desktop Window Manager rounded corners (DWMWA_WINDOW_CORNER_PREFERENCE) and adaptive dark aesthetics.",
    faq5Q: "Do I need to install Python, Node.js, or Electron?",
    faq5A: "No. MiEarbuddy is a pure native C# WinForms application compiled with built-in Windows APIs (GDI+ and Winsock). It has zero external dependencies and runs directly using the .NET Framework 4.0+ already included with Windows.",
    faq6Q: "Can I monitor each earbud and the case battery independently?",
    faq6A: "Yes. MiEarbuddy decodes Google FastPair telemetry beacons in real time, displaying separate battery percentages and charging lightning indicators for the Left Earbud, Charging Case, and Right Earbud with color-coded safety levels.",
    faq7Q: "How does the Active Noise Cancellation control work?",
    faq7A: "When you click Noise Cancellation, Transparency Mode, or Off, MiEarbuddy sends direct authenticated commands over Winsock RFCOMM sockets to the earbud's onboard DSP. A smart anti-bounce filter prevents dual-earbud sync echoes and audio stutter.",
    faq8Q: "Can I minimize the application to the Windows System Tray?",
    faq8A: "Yes. Clicking the close button or pressing ESC minimizes MiEarbuddy directly to your Windows Notification Area. Hovering over the icon displays a live summary tooltip, and right-clicking opens a dark context menu with instant mode controls.",
    faq9Q: "How do I make MiEarbuddy start automatically with Windows?",
    faq9A: "Right-click the MiEarbuddy icon in your System Tray and click 'Start with Windows'. The app writes an entry to your personal Windows User Registry (Software\\Microsoft\\Windows\\CurrentVersion\\Run) with the -tray flag, ensuring silent background startup.",
    faq10Q: "How do I build the application from source code?",
    faq10A: "The repository includes a fast native build script (build.bat). Simply run build.bat in command prompt. It uses the Microsoft .NET Framework C# compiler (csc.exe) pre-installed on your system, compiling the entire project in less than 1 second without needing Visual Studio.",

    // Footer
    footerDesc: "A native, ultra-lightweight Windows desktop companion for Xiaomi and Redmi earbuds.",
    footerNavTitle: "Navigation",
    footerProjectTitle: "Project",
    footerTechTitle: "Architecture",
    footerCopyright: "© 2026 MiEarbuddy. Open source under the MIT License.",
    footerCreatedBy: "Created by",
    footerLegal: "Xiaomi, Redmi, and HyperOS are registered trademarks of Xiaomi Inc. MiEarbuddy is an independent, community-developed open-source software project and is not affiliated with or endorsed by Xiaomi Inc."
  },

  pt: {
    // Banner
    bannerText: "O MiEarbuddy 1.0 já está disponível para o Windows 11 e 10. C# puro • 0.0% CPU.",
    bannerLink: "Descarregar v1.0.0 (.exe) >",

    // Navigation
    navOverview: "Visão Geral",
    navShowcase: "Interface",
    navNoiseControl: "Controlo de Ruído",
    navBattery: "Bateria",
    navTray: "Área de Notificação",
    navPerformance: "Desempenho",
    navHowItWorks: "Como Funciona",
    navCompatibility: "Compatibilidade",
    navFaq: "Perguntas Frequentes",
    navDownload: "Descarregar",

    // Hero Section
    heroBadge: "Acompanhante Windows • C# Nativo Puro",
    heroTitlePart1: "Os teus auscultadores.",
    heroTitlePart2: "O teu controlo.",
    heroSubtitle: "Um companheiro leve para Windows que traz controlo inteligente, monitorização de bateria em tempo real e gestão perfeita dos teus auscultadores para o teu ambiente de trabalho.",
    heroBtnDownload: "Descarregar para Windows",
    heroBtnGithub: "Ver no GitHub",
    heroRevealHint: "✦ Clica ou passa o rato para revelar o nome completo",
    heroSpecWin: "Windows 11 e 10",
    heroSpecNative: "C# Nativo Puro",
    heroSpecCpu: "0.0% CPU em Repouso",
    heroSpecRam: "~25 MB de RAM",

    // Showcase
    showcaseHeading: "Criado para Windows.<br><span style=\"color: var(--text-silver);\">Desenvolvido com a Precisão do HyperOS.</span>",
    showcaseSubtitle: "O MiEarbuddy une o design refinado do tema escuro da Xiaomi à arquitetura nativa do ambiente de trabalho Windows.",
    showcaseWinTitle: "Mi Earbuddy - Redmi Buds 5 Pro",
    showcaseStatus: "Conectado",
    showcaseHotspot1Title: "Cabeçalho Adaptativo Fluido",
    showcaseHotspot1Desc: "Controlos de barra de título fluidos, alternância instantânea de idioma (PT/EN) e botões de controlo personalizados (↺ Repor, ─ Minimizar, ✕ Ocultar no Tabuleiro).",
    showcaseHotspot2Title: "Cápsulas de Bateria GDI+ em Tempo Real",
    showcaseHotspot2Desc: "Monitorização de três células FastPair renderizada nativamente com transições dinâmicas de cor e ícones de relâmpago de carga.",
    showcaseHotspot3Title: "Arquitetura Direta de Controlo de Ruído",
    showcaseHotspot3Desc: "Troca com um clique entre Cancelamento de Ruído, Transparência e Desativado através de sockets RFCOMM.",
    showcaseHotspot4Title: "Minimização Inteligente para o Tabuleiro",
    showcaseHotspot4Desc: "Minimiza instantaneamente junto ao relógio do Windows. Zero consumo de CPU em repouso de fundo.",

    // Active Noise Cancellation
    ancHeading: "Até 52 dB de silêncio.<br><span style=\"color: var(--apple-blue);\">Ao teu alcance.</span>",
    ancSubtitle: "Comanda o DSP dos auscultadores diretamente por sockets Winsock RFCOMM sem falhas ou gaguejos de áudio.",
    ancMode1Title: "Cancelamento de Ruído",
    ancMode2Title: "Modo Transparência",
    ancMode3Title: "Modo Desativado",
    ancCaptionLead1: "Cancelamento de Ruído:",
    ancCaptionBody1: "Frequências acústicas em oposição de fase a neutralizar ruídos externos contínuos (-52 dB).",
    ancCaptionLead2: "Modo Transparência:",
    ancCaptionBody2: "Frequências ambientais transmitidas através de microfones de banda larga com clareza natural.",
    ancCaptionLead3: "Modo Desativado:",
    ancCaptionBody3: "Filtros DSP desligados. Isolamento físico acústico passivo em funcionamento.",
    ancVisualizerStatus1: "Frequências acústicas em oposição de fase a neutralizar ruídos externos contínuos (-52 dB).",
    ancVisualizerStatus2: "Frequências ambientais transmitidas através de microfones de banda larga com clareza natural.",
    ancVisualizerStatus3: "Filtros DSP desligados. Isolamento físico acústico passivo em funcionamento.",
    ancMode1Desc: "Supressão ativa profunda de ruído. Elimina zumbidos de ventoinhas, motores e trânsito para concentração total.",
    ancMode2Desc: "Consciência ambiental natural. Deixa passar vozes e sons circundantes com clareza sem retirar os auscultadores.",
    ancMode3Desc: "Isolamento acústico passivo. Poupa a bateria dos auscultadores mantendo a barreira física natural.",
    ancAntiBounceTitle: "Filtro Inteligente Anti-Ressalto:",
    ancAntiBounceDesc: "Previne gaguejos de áudio e suprime ecos transitórios de sincronização entre os auscultadores, garantindo transições instantâneas com um único sinal sonoro limpo.",

    // Battery Section
    battHeading: "Cada auscultador. O estojo.<br><span style=\"color: var(--status-green);\">Sempre à vista.</span>",
    battSubtitle: "Nunca sejas apanhado com um auscultador descarregado. Telemetria em tempo real descodificada instantaneamente.",
    battLeft: "Auscultador Esquerdo",
    battCase: "Estojo de Carga",
    battRight: "Auscultador Direito",
    battCharging: "A carregar",
    battReady: "Em uso",
    battFooterNote: "Descodifica beacons de telemetria do Google FastPair em tempo real. Limiares de cor dinâmicos alertam quando a bateria atinge 20%, e ícones de relâmpago confirmam o carregamento de imediato.",

    // System Tray
    trayHeading: "Sempre pronto.<br><span style=\"color: var(--text-silver);\">Nunca no teu caminho.</span>",
    traySubtitle: "O MiEarbuddy vive discretamente na Área de Notificação do Windows, dando acesso imediato sem ocupar a barra de tarefas.",
    trayPreviewCaption: "Menu de Contexto Dedicado para a Barra de Tarefas",
    trayBlock1Title: "Tooltip com Informação ao Passar o Rato",
    trayBlock1Desc: "Passar o rato sobre o ícone junto ao relógio do Windows revela instantaneamente o modelo dos auscultadores, percentagens de bateria e modo de ruído ativo.",
    trayBlock2Title: "Alternância de Modos com Um Clique",
    trayBlock2Desc: "Alterna entre Cancelamento de Ruído, Modo Transparência ou Desativado diretamente na barra de tarefas sem abrir a janela principal.",
    trayBlock3Title: "Iniciar com o Windows",
    trayBlock3Desc: "Integrado no Registo do Windows (chave Run) com argumento -tray para arranque silencioso em segundo plano.",
    trayTaskbarSim: "Área de Notificação do Windows 11",

    // Performance Section
    perfHeading: "0.0% CPU.<br>~25 MB RAM.<br><span style=\"color: var(--apple-blue);\">Zero Electron.</span>",
    perfSubtitle: "Construído em C# puro e Windows GDI+. Sem runtimes web, sem Node.js, sem WebView2 e sem Python.",
    perfStat1Label: "Consumo de RAM",
    perfStat1Desc: "95% menos memória do que utilitários em Electron.",
    perfStat2Label: "CPU em Repouso",
    perfStat2Desc: "Zero consumo em segundo plano na barra de tarefas.",
    perfStat4Label: "Executável Único",
    perfStat4Desc: "Binário portátil ultracompacto.",
    perfStat5Label: "Arranque Instantâneo",
    perfStat5Desc: "Execução instantânea sem atrasos de frameworks pesadas.",
    arch1Title: "Winsock RFCOMM (ws2_32.dll)",
    arch1Desc: "Comunicação de baixo nível direta com sockets Bluetooth do Windows, eliminando camadas lentas de drivers.",
    arch2Title: "Desafio Criptográfico E21",
    arch2Desc: "Protocolo de autenticação mútua revertido garantindo emparelhamento seguro com o hardware Xiaomi.",
    arch3Title: "Descodificador FastPair",
    arch3Desc: "Extrai percentagens de bateria de Esquerda, Direita e Estojo de pacotes Google FastPair em tempo real.",
    arch4Title: "Proteção Mutex de Instância Única",
    arch4Desc: "Impede processos duplicados em segundo plano, trazendo automaticamente a janela ativa para primeiro plano.",

    // How It Works
    howTitlePart1: "Configuração Simples.",
    howTitlePart2: "Operação Perfeita.",
    howSubtitle: "Quatro passos simples para trazer os teus auscultadores Xiaomi e Redmi para o Windows.",
    step1Title: "Descarregar MiEarbuddy",
    step1Desc: "Descarrega o executável único (~680 KB). Não requer instalador, assistente ou permissões de administrador.",
    step2Title: "Ligar através do Windows",
    step2Desc: "Emparelha os teus auscultadores Xiaomi ou Redmi através do menu normal de Bluetooth do Windows.",
    step3Title: "Abrir e Autenticar",
    step3Desc: "Inicia o MiEarbuddy. Ele deteta automaticamente os auscultadores e executa o handshake criptográfico E21.",
    step4Title: "Desfrutar de Controlo Total",
    step4Desc: "Monitoriza baterias de três células e alterna modos de ruído diretamente do ambiente de trabalho ou tabuleiro.",

    // Compatibility
    compatTitle: "Compatibilidade e Requisitos",
    compatSubtitle: "Desenvolvido especificamente para hardware de áudio Xiaomi e Redmi em sistemas Windows modernos.",
    compatBudsTitle: "Hardware Verificado: Redmi Buds 5 Pro",
    compatBudsDesc: "Testado e certificado especificamente nos Redmi Buds 5 Pro. Compatível com modelos Xiaomi e Redmi que usem o protocolo Xiaomi MMA e FastPair RFCOMM.",
    compatCheck1: "Modos de Cancelamento de Ruído (ANC), Transparência e Desativado",
    compatCheck2: "Monitorização independente de Auscultador Esquerdo, Direito e Estojo",
    compatCheck3: "Filtro Anti-Ressalto inteligente para transições acústicas suaves",
    compatCheck4: "Início automático em segundo plano com o Windows",
    compatSysReqTitle: "Requisitos de Sistema",
    compatReqOsTitle: "Sistema Operativo:",
    compatReqOsDesc: "Windows 11 (todas as edições) ou Windows 10 (versão 1809+).",
    compatReqNetTitle: ".NET Framework:",
    compatReqNetDesc: ".NET Framework 4.0 ou mais recente (já incluído no Windows).",
    compatReqBtTitle: "Adaptador Bluetooth:",
    compatReqBtDesc: "Bluetooth 4.0+ com suporte a conexões de porta série RFCOMM.",
    compatReqPermTitle: "Permissões de Utilizador:",
    compatReqPermDesc: "Conta de utilizador padrão (não necessita de permissões de Administrador).",

    // Download Section
    dlTitlePart1: "Assume o controlo da tua",
    dlTitlePart2: "experiência de audição.",
    dlSubtitle: "Obtém o MiEarbuddy e aproxima os teus auscultadores do teu ambiente de trabalho Windows.",
    dlBtnExe: "Descarregar MiEarbuddy.exe",
    dlBtnRepo: "Repositório no GitHub",
    dlMetaVersion: "Versão 1.0.0 • Executável Portátil (~680 KB)",
    dlMetaLicense: "Código Aberto • Licença MIT",

    // FAQ Section
    faqTitle: "Perguntas Frequentes",
    faqSubtitle: "Tudo o que precisas de saber sobre o MiEarbuddy, a sua arquitetura e compatibilidade.",
    faq1Q: "O que é o MiEarbuddy?",
    faq1A: "O MiEarbuddy é uma aplicação nativa ultraleve para Windows desenhada especificamente para auscultadores Xiaomi e Redmi. Fornece monitorização de bateria em tempo real para três células (Auscultador Esq., Estojo, Auscultador Dir.), controlo de Cancelamento de Ruído (ANC) e integração completa na barra de tarefas do Windows.",
    faq2Q: "Quais são os auscultadores oficialmente suportados?",
    faq2A: "A aplicação foi oficialmente testada e verificada em hardware nos Redmi Buds 5 Pro. Comunica através do protocolo oficial Xiaomi MMA (Mobile Media Application) RFCOMM e telemetria Google FastPair, sendo compatível com outros auscultadores Xiaomi e Redmi que utilizem esta mesma pilha de protocolos.",
    faq3Q: "O MiEarbuddy é gratuito e de código aberto?",
    faq3A: "Sim. O MiEarbuddy é 100% gratuito e de código aberto, distribuído sob a permissiva Licença MIT. Podes inspecionar o código-fonte, modificá-lo e compilá-lo por ti próprio.",
    faq4Q: "Funciona no Windows 11 e no Windows 10?",
    faq4A: "Sim, suporta totalmente tanto o Windows 11 como o Windows 10 (versão 1809 ou mais recente). No Windows 11, ativa cantos arredondados nativos do Desktop Window Manager (DWMWA_WINDOW_CORNER_PREFERENCE) e tema escuro harmonioso.",
    faq5Q: "Preciso de instalar Python, Node.js ou Electron?",
    faq5A: "Não. O MiEarbuddy é uma aplicação C# WinForms nativa pura, compilada com APIs diretas do Windows (GDI+ e Winsock). Tem zero dependências externas e corre diretamente com o .NET Framework 4.0+ pré-instalado no Windows.",
    faq6Q: "Posso monitorizar cada auscultador e o estojo separadamente?",
    faq6A: "Sim. O MiEarbuddy descodifica os beacons de telemetria do Google FastPair em tempo real, mostrando percentagens de bateria independentes e ícones de relâmpago de carga para o Auscultador Esquerdo, Estojo e Auscultador Direito, com código de cores dinâmico.",
    faq7Q: "Como funciona o controlo do Cancelamento de Ruído?",
    faq7A: "Ao clicares em Cancelamento de Ruído, Transparência ou Desativado, o MiEarbuddy envia comandos autenticados por sockets RFCOMM diretamente para o processador de som (DSP) dos auscultadores. Um filtro anti-ressalto impede gaguejos de áudio e ecos de sincronização.",
    faq8Q: "Posso minimizar a aplicação para a barra de tarefas?",
    faq8A: "Sim. Clicar em Fechar ou pressionar ESC minimiza o MiEarbuddy diretamente para a Área de Notificação do Windows. Passar o rato sobre o ícone mostra o tooltip com informação em tempo real, e o botão direito abre um menu escuro personalizado.",
    faq9Q: "Como faço para o MiEarbuddy iniciar com o Windows?",
    faq9A: "Clica com o botão direito no ícone da barra de tarefas e ativa a opção 'Iniciar com o Windows'. A aplicação grava um registo no Windows (Software\\Microsoft\\Windows\\CurrentVersion\\Run) com a flag -tray, iniciando silenciosamente em segundo plano.",
    faq10Q: "Como compilo a aplicação a partir do código-fonte?",
    faq10A: "O repositório inclui um script de compilação rápida (build.bat). Basta executá-lo na linha de comandos. Ele utiliza o compilador oficial da Microsoft (csc.exe) já existente no teu computador e compila todo o projeto em menos de 1 segundo, sem necessidade do Visual Studio.",

    // Footer
    footerDesc: "Um companheiro nativo e ultraleve para Windows para auscultadores Xiaomi e Redmi.",
    footerNavTitle: "Navegação",
    footerProjectTitle: "Projeto",
    footerTechTitle: "Arquitetura",
    footerCopyright: "© 2026 MiEarbuddy. Código aberto sob a Licença MIT.",
    footerCreatedBy: "Desenvolvido por",
    footerLegal: "Xiaomi, Redmi e HyperOS são marcas registadas da Xiaomi Inc. O MiEarbuddy é um projeto independente de código aberto desenvolvido pela comunidade e não é afiliado nem patrocinado pela Xiaomi Inc."
  }
};

let currentLanguage = localStorage.getItem('miearbuddy_lang') || 'en';

function getTranslation(key) {
  if (translations[currentLanguage] && translations[currentLanguage][key]) {
    return translations[currentLanguage][key];
  }
  return translations.en[key] || key;
}

function setLanguage(lang) {
  if (lang !== 'en' && lang !== 'pt') lang = 'en';
  currentLanguage = lang;
  localStorage.setItem('miearbuddy_lang', lang);
  document.documentElement.lang = lang;

  // Update text elements with data-i18n attribute
  document.querySelectorAll('[data-i18n]').forEach(el => {
    const key = el.getAttribute('data-i18n');
    const translated = getTranslation(key);
    if (translated) {
      if (el.tagName === 'INPUT' && el.type === 'button') {
        el.value = translated;
      } else {
        el.textContent = translated;
      }
    }
  });

  // Update rich HTML elements with data-i18n-html attribute
  document.querySelectorAll('[data-i18n-html]').forEach(el => {
    const key = el.getAttribute('data-i18n-html');
    const translated = getTranslation(key);
    if (translated) {
      el.innerHTML = translated;
    }
  });

  // Update title / tooltip attributes with data-i18n-title attribute
  document.querySelectorAll('[data-i18n-title]').forEach(el => {
    const key = el.getAttribute('data-i18n-title');
    const translated = getTranslation(key);
    if (translated) {
      el.setAttribute('title', translated);
    }
  });

  // Update language toggle button visual state across all buttons
  document.querySelectorAll('.lang-indicator-en').forEach(el => {
    el.classList.toggle('active-lang', currentLanguage === 'en');
  });
  document.querySelectorAll('.lang-indicator-pt').forEach(el => {
    el.classList.toggle('active-lang', currentLanguage === 'pt');
  });

  // Notify other components
  window.dispatchEvent(new CustomEvent('languageChanged', { detail: { lang: currentLanguage } }));
}

function toggleLanguage() {
  const nextLang = currentLanguage === 'en' ? 'pt' : 'en';
  setLanguage(nextLang);
}

// Initialize on DOM ready
document.addEventListener('DOMContentLoaded', () => {
  setLanguage(currentLanguage);

  document.querySelectorAll('.lang-toggle-btn').forEach(btn => {
    btn.addEventListener('click', (e) => {
      e.preventDefault();
      toggleLanguage();
    });
  });
});

window.i18n = {
  getTranslation,
  setLanguage,
  toggleLanguage,
  getCurrentLanguage: () => currentLanguage
};
