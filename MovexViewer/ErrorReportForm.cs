using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MovexViewer
{
    public partial class ErrorReportForm : Form
    {
        public string ReportText => textBoxReport.Text.Trim();
        public string UserEmail => textBoxEmail.Text.Trim();

        public ErrorReportForm(string email)
        {
            InitializeComponent();
            textBoxEmail.Text = email ?? "";
        }

        private void InitializeComponent()
        {
            this.textBoxEmail = new TextBox();
            this.textBoxReport = new TextBox();
            this.buttonSend = new Button();
            var labelEmail = new Label();
            var labelReport = new Label();

            // Label Email
            labelEmail.Text = "Váš e-mail:";
            labelEmail.Top = 10;
            labelEmail.Left = 10;
            labelEmail.Width = 100;

            // Email TextBox
            this.textBoxEmail.Top = 30;
            this.textBoxEmail.Left = 10;
            this.textBoxEmail.Width = 400;

            // Label Report
            labelReport.Text = "Popis chyby:";
            labelReport.Top = 60;
            labelReport.Left = 10;
            labelReport.Width = 100;

            // Report TextBox
            this.textBoxReport.Multiline = true;
            this.textBoxReport.Top = 80;
            this.textBoxReport.Left = 10;
            this.textBoxReport.Width = 400;
            this.textBoxReport.Height = 200;
            this.textBoxReport.ScrollBars = ScrollBars.Vertical;

            // Button
            this.buttonSend.Text = "Odeslat";
            this.buttonSend.Top = 290;
            this.buttonSend.Left = 10;
            this.buttonSend.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(UserEmail) || string.IsNullOrWhiteSpace(ReportText))
                {
                    MessageBox.Show("Vyplňte e-mail a text chyby.", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            // Form
            this.Text = "Nahlášení chyby";
            this.ClientSize = new System.Drawing.Size(430, 340);
            this.Controls.Add(labelEmail);
            this.Controls.Add(textBoxEmail);
            this.Controls.Add(labelReport);
            this.Controls.Add(textBoxReport);
            this.Controls.Add(buttonSend);
        }


        private TextBox textBoxEmail;
        private TextBox textBoxReport;
        private Button buttonSend;
    }
}
