using System;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Net.Mail;
using System.Security.Principal;
using System.Windows.Forms;

namespace MovexViewer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.Load += new EventHandler(Form1_Load);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                // Povolení admin tlačítka jen pro skupinu G_Admins
                button2.Enabled = IsUserInGroup("G_Admins");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba pri overení skupiny:\n" + ex.Message,
                                "Chyba prístupu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                button2.Enabled = false;
            }

            // Po štarte automaticky otvorí MOVEX Workplace v IE
            OtevritMovexWorkplace();
        }

        private bool IsUserInGroup(string groupName)
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);

                var allGroups = identity.Groups.Select(g => g.Translate(typeof(NTAccount)).ToString());
                return allGroups.Any(g => g.EndsWith("\\" + groupName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Výnimka pri kontrole skupín: " + ex.Message);
                return false;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ReportError();
        }

        private void ReportError()
        {
            string userEmail = "";

            try
            {
                string username = WindowsIdentity.GetCurrent().Name.Split('\\')[1];
                userEmail = GetEmailFromAD(username) ?? "";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nepodarilo sa získať e-mail používateľa: " + ex.Message,
                                "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var dialog = new ErrorReportForm(userEmail);
            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(userEmail);    // Odosielateľ = prihlásený používateľ
                mail.To.Add("helpdesk@kovolit.cz");        // Helpdesk
                mail.To.Add(userEmail);                    // Kopia používateľovi

                mail.Subject = "MOVEX chyba od uživatele";
                mail.Body = $"Nahlásená chyba od: {dialog.UserEmail}\n\nText chyby:\n{dialog.ReportText}";

                SmtpClient smtp = new SmtpClient("10.0.2.29");
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

                smtp.Send(mail);

                MessageBox.Show(
                    $"Chyba bola odoslaná.\n\nOd: {dialog.UserEmail}\nNa: helpdesk@kovolit.cz, {userEmail}\n\nText:\n{dialog.ReportText}",
                    "Odoslané", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nepodarilo sa odoslať chybu: " + ex.Message,
                                "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetEmailFromAD(string username)
        {
            using (PrincipalContext ctx = new PrincipalContext(ContextType.Domain))
            {
                UserPrincipal user = UserPrincipal.FindByIdentity(ctx, username);
                return user?.EmailAddress ?? throw new Exception("Nepodařilo se získat e-mail uživatele.");
            }
        }

        // ==========================================
        //  Spúšťanie MOVEX cez klasický Internet Explorer
        // ==========================================

        private void OtevritMovexWorkplace()
        {
            SpustitInternetExplorer("http://movex-wp/mwp/");
        }

        private void OtevritMovexAdmin()
        {
            SpustitInternetExplorer("http://movex-apl:6666/");
        }

        private void SpustitInternetExplorer(string url)
        {
            try
            {
                Process.Start("iexplore.exe", url);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nepodarilo sa spustiť Internet Explorer:\n" + ex.Message,
                                "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OtevritMovexWorkplace();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OtevritMovexAdmin();
        }
    }
}
