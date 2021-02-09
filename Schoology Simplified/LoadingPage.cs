using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Schoology_Simplified
{
    public partial class LoadingPage : Form
    {

        public LoadingPage()
        {
            InitializeComponent();
        }

        private void LoadingPage_LoadAsync(object sender, EventArgs e)
        {

            new Thread(async () =>
            {

                string information = File.ReadAllText(Schoology.path + "information.json");

                Dictionary<string, string> product = JsonConvert.DeserializeObject<Dictionary<string, string>>(information);

                var document = await Schoology.LogIn(product["username"], product["password"], product["grade"], progressBar1);
                
                Invoke(new Action(() =>
                {

                    if (document == null)
                    {
                        LoginPage page = new LoginPage();
                        page.Show();
                        this.Hide();
                    }else
                    {
                        HomePage page = new HomePage(document, product["grade"]);
                        page.Show();
                        this.Hide();
                    }
                    

                }));

            }).Start();
        }

        private void LoadingPage_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Schoology.chrome != null)
            {
                Schoology.chrome.Quit();
            }

            Environment.Exit(Environment.ExitCode);
        }
    }
}
