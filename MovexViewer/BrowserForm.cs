using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MovexViewer
{
    public class BrowserForm : Form
    {
        private WebBrowser webBrowser;

        public BrowserForm(string url)
        {
            this.Text = "MOVEX Viewer";
            this.WindowState = FormWindowState.Maximized;

            webBrowser = new WebBrowser
            {
                Dock = DockStyle.Fill,
                ScriptErrorsSuppressed = true
            };

            this.Controls.Add(webBrowser);
            webBrowser.Navigate(url);
        }
    }
}