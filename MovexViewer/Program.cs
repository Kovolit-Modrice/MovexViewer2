using System;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MovexViewer
{
    internal static class Program
    {
        /// <summary>
        /// Přepínač pro zobrazení diagnostické hlášky po startu.
        /// Až bude vše odladěno, můžeš nastavit na false.
        /// </summary>
        private const bool DIAGNOSTIKA_AKTIVNI = true;

        /// <summary>
        /// Hlavní vstupní bod aplikace.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Nastavení prostředí pro správný běh IE / WebBrowseru
            IeEnvironmentConfigurator.Configure();

            if (DIAGNOSTIKA_AKTIVNI)
            {
                string report = IeEnvironmentConfigurator.VytvorDiagnostiku();
                MessageBox.Show(report, "Diagnostika MOVEX Viewer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }

    /// <summary>
    /// Pomocná třída, která připraví prostředí pro MOVEX:
    /// - nastaví IE11 emulaci pro aktuální EXE
    /// - přidá MOVEX servery do zóny "Místní intranet"
    /// - přidá MOVEX servery také do EscDomains (IE Enhanced Security)
    /// - povolí ActiveX a skriptování v intranet zóně
    /// </summary>
    internal static class IeEnvironmentConfigurator
    {
        // Hodnota 11001 = IE11 ve standardním režimu
        private const int IE11_EMULATION = 11001;

        public static void Configure()
        {
            try { NastavitIeEmulaci(); } catch { }
            try { PridatMovexServeryDoZoneMap(); } catch { }
            try { PridatMovexServeryDoEscDomains(); } catch { }
            try { PovolitActiveXProIntranet(); } catch { }
        }

        /// <summary>
        /// Vytvoří text s diagnostikou – stav emulace IE, zón a ActiveX.
        /// </summary>
        public static string VytvorDiagnostiku()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Diagnostika prostředí pro MOVEX Viewer");
            sb.AppendLine("=======================================");
            sb.AppendLine();

            // 1) IE emulace
            try
            {
                string exeName = Process.GetCurrentProcess().ProcessName + ".exe";
                int? emu = null;

                using (var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"))
                {
                    if (key != null)
                    {
                        object val = key.GetValue(exeName);
                        if (val is int)
                            emu = (int)val;
                    }
                }

                if (emu.HasValue)
                {
                    sb.AppendLine($"IE emulace pro {exeName}: NASTAVENO ({emu.Value})"
                                  + (emu.Value == IE11_EMULATION ? " = IE11 OK" : " = jiná hodnota!"));
                }
                else
                {
                    sb.AppendLine($"IE emulace pro {exeName}: NENÍ NASTAVENA");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("Chyba při čtení IE emulace: " + ex.Message);
            }

            sb.AppendLine();

            // 2) ZoneMap (běžné zóny)
            try
            {
                sb.AppendLine("Zóny pro servery (ZoneMap):");

                int? zoneWp = PrectiZoneMap("movex-wp");
                int? zoneApl = PrectiZoneMap("movex-apl");

                sb.AppendLine(" - movex-wp : " + (zoneWp.HasValue ? zoneWp.Value.ToString() : "není v ZoneMap")
                              + (zoneWp == 1 ? " (Intranet OK)" : zoneWp == 2 ? " (Důvěryhodné weby)" : ""));
                sb.AppendLine(" - movex-apl: " + (zoneApl.HasValue ? zoneApl.Value.ToString() : "není v ZoneMap")
                              + (zoneApl == 1 ? " (Intranet OK)" : zoneApl == 2 ? " (Důvěryhodné weby)" : ""));
            }
            catch (Exception ex)
            {
                sb.AppendLine("Chyba při čtení ZoneMap: " + ex.Message);
            }

            sb.AppendLine();

            // 3) EscDomains (IE ESC)
            try
            {
                sb.AppendLine("EscDomains (IE Enhanced Security):");

                int? escWp = PrectiEscDomains("movex-wp");
                int? escApl = PrectiEscDomains("movex-apl");

                sb.AppendLine(" - movex-wp : " + (escWp.HasValue ? escWp.Value.ToString() : "není v EscDomains")
                              + (escWp == 1 ? " (Intranet)" : escWp == 2 ? " (Důvěryhodné weby)" : ""));
                sb.AppendLine(" - movex-apl: " + (escApl.HasValue ? escApl.Value.ToString() : "není v EscDomains")
                              + (escApl == 1 ? " (Intranet)" : escApl == 2 ? " (Důvěryhodné weby)" : ""));
            }
            catch (Exception ex)
            {
                sb.AppendLine("Chyba při čtení EscDomains: " + ex.Message);
            }

            sb.AppendLine();

            // 4) ActiveX / scripting
            try
            {
                sb.AppendLine("Nastavení ActiveX pro zónu 1 (Místní intranet):");

                using (var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\1"))
                {
                    if (key == null)
                    {
                        sb.AppendLine(" Klíč zóny 1 nenalezen.");
                    }
                    else
                    {
                        int? v1001 = key.GetValue("1001") as int?;
                        int? v1004 = key.GetValue("1004") as int?;
                        int? v1200 = key.GetValue("1200") as int?;
                        int? v1400 = key.GetValue("1400") as int?;

                        sb.AppendLine($" 1001 (Download unsigned ActiveX): {FormatPolicyValue(v1001)}");
                        sb.AppendLine($" 1004 (Run ActiveX controls):       {FormatPolicyValue(v1004)}");
                        sb.AppendLine($" 1200 (Init/script ActiveX unsafe): {FormatPolicyValue(v1200)}");
                        sb.AppendLine($" 1400 (Active scripting):           {FormatPolicyValue(v1400)}");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("Chyba při čtení nastavení zóny 1: " + ex.Message);
            }

            sb.AppendLine();
            sb.AppendLine("Poznámka:");
            sb.AppendLine(" 0 = Povolit, 1 = Dotázat se, 3 = Zakázat.");
            sb.AppendLine(" Hodnoty se zapisují do HKCU, takže jsou platné jen pro aktuálního uživatele.");
            sb.AppendLine();
            sb.AppendLine("Až bude vše v pořádku, lze diagnostiku vypnout změnou DIAGNOSTIKA_AKTIVNI = false v Program.cs.");

            return sb.ToString();
        }

        private static string FormatPolicyValue(int? val)
        {
            if (!val.HasValue)
                return "není nastaveno";

            switch (val.Value)
            {
                case 0: return "0 (Povolit)";
                case 1: return "1 (Dotázat se)";
                case 3: return "3 (Zakázat)";
                default: return val.Value.ToString() + " (jiná hodnota)";
            }
        }

        private static int? PrectiZoneMap(string domain)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Internet Settings\ZoneMap\Domains\" + domain))
            {
                if (key == null)
                    return null;

                object val = key.GetValue("*");
                if (val is int)
                    return (int)val;

                return null;
            }
        }

        private static int? PrectiEscDomains(string domain)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Internet Settings\ZoneMap\EscDomains\" + domain))
            {
                if (key == null)
                    return null;

                object val = key.GetValue("*");
                if (val is int)
                    return (int)val;

                return null;
            }
        }

        /// <summary>
        /// Nastaví pro aktuální EXE emulaci IE11 (FEATURE_BROWSER_EMULATION).
        /// </summary>
        private static void NastavitIeEmulaci()
        {
            string exeName = Process.GetCurrentProcess().ProcessName + ".exe";

            using (var key = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION",
                true))
            {
                if (key == null)
                    return;

                object current = key.GetValue(exeName);
                if (current == null || (int)current != IE11_EMULATION)
                {
                    key.SetValue(exeName, IE11_EMULATION, RegistryValueKind.DWord);
                }
            }
        }

        /// <summary>
        /// Přidá MOVEX servery do běžné ZoneMap (bez ESC).
        /// </summary>
        private static void PridatMovexServeryDoZoneMap()
        {
            // 1 = Local Intranet
            const int INTRANET_ZONE = 1;

            using (var keyWp = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Internet Settings\ZoneMap\Domains\movex-wp", true))
            {
                keyWp?.SetValue("*", INTRANET_ZONE, RegistryValueKind.DWord);
            }

            using (var keyApl = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Internet Settings\ZoneMap\Domains\movex-apl", true))
            {
                keyApl?.SetValue("*", INTRANET_ZONE, RegistryValueKind.DWord);
            }
        }

        /// <summary>
        /// Přidá MOVEX servery do EscDomains (IE Enhanced Security).
        /// Dáme je do zóny Důvěryhodné servery (2).
        /// </summary>
        private static void PridatMovexServeryDoEscDomains()
        {
            // 2 = Trusted Sites
            const int TRUSTED_ZONE = 2;

            using (var keyWp = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Internet Settings\ZoneMap\EscDomains\movex-wp", true))
            {
                keyWp?.SetValue("*", TRUSTED_ZONE, RegistryValueKind.DWord);
            }

            using (var keyApl = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Internet Settings\ZoneMap\EscDomains\movex-apl", true))
            {
                keyApl?.SetValue("*", TRUSTED_ZONE, RegistryValueKind.DWord);
            }
        }

        /// <summary>
        /// Povolí ActiveX a skriptování v zóně "Místní intranet" (Zones\1).
        /// </summary>
        private static void PovolitActiveXProIntranet()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\1", writable: true))
            {
                if (key == null)
                    return;

                key.SetValue("1001", 0, RegistryValueKind.DWord);
                key.SetValue("1004", 0, RegistryValueKind.DWord);
                key.SetValue("1200", 0, RegistryValueKind.DWord);
                key.SetValue("1400", 0, RegistryValueKind.DWord);
            }
        }
    }
}
