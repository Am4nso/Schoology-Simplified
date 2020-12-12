using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Schoology_Simplified
{
    public partial class HomePage : Form
    {

        private string currentPeriod;

        public static Thread thread_one;
        public static Thread thread_two;
        public static Thread thread_three;

        private bool in_conference = false;

        private IWebDriver current_driver;

        public HomePage(HtmlAgilityPack.HtmlDocument document, string grade)
        {

            string student_name = document.DocumentNode.Descendants("h2").Where(node => node.GetClasses().Contains("page-title")).FirstOrDefault().InnerText;

            string first_name = student_name.Split(' ')[0];

            string upcoming = Schoology.NextPeriod();

            string starting_in = Schoology.NextPeriodIn();

            string current_period = Schoology.CurrentPeriod();

            InitializeComponent();

            label3.Text = grade;

            this.Text = "Schoology Simplified - " + first_name;

            label2.Text = "Welcome back, " + first_name + ".";

            label6.Text = "Upcoming: " + upcoming;

            label7.Text = "Next period: " + starting_in;

            label5.Text = "Current: " + current_period;

            pictureBox3.Load("https://github.com/Am4nso/schoology-simplified-database/blob/main/schedules/" + grade + ".png?raw=true");

            thread_one = new Thread(new ThreadStart(InformationUpdater))
            {
                IsBackground = true
            };
            thread_one.Start();

            thread_two = new Thread(new ThreadStart(Conference_Checker))
            {
                IsBackground = true
            };
            thread_two.Start();
        }

        private void JoinConferenceClick(object sender, EventArgs e)
        {
            in_conference = true;

            button1.Enabled = false;

            new Thread(() =>
            {
                try
                {
                    string url = Schoology.GetConferenceLink(currentPeriod);

                    string courseID = Schoology.course_to_id[currentPeriod];

                    ChromeDriver temp_driver = new ChromeDriver(Schoology.driverService, Schoology.options)
                    {
                        Url = "https://inpsa.schoology.com/login?&school=2382278049"
                    };

                    current_driver = temp_driver;

                    foreach (System.Net.Cookie cookie in Schoology.responseCookies)
                    {

                        temp_driver.Manage().Cookies.AddCookie(new Cookie(cookie.Name, cookie.Value));

                    }

                    temp_driver.Url = "https://inpsa.schoology.com/apps/login/saml/initial?realm=course&realm_id=" + courseID + "&spentityid=9295f7b9ba9a31af8c09d5442f697eb005452c17d&RelayState=https%3A%2F%2Fbigbluebutton.app.schoology.com%2Fhome%3Frealm%3Dsection%26realm_id%3D" + courseID + "%26app_id%3D191034318%26is_ssl%3D1";

                    temp_driver.Url = url;
                }
                catch (Exception)
                {
                    Invoke(new Action(() =>
                    {
                        in_conference = false;
                        button1.Enabled = true;
                    }));
                    return;
                }

                thread_three = new Thread(new ThreadStart(Check_Browser))
                {
                    IsBackground = true
                };
                thread_three.Start();

            }).Start();
        }

        public static bool IsBrowserClosed(IWebDriver driver)
        {
            bool isClosed = false;
            try
            {
                if (driver == null)
                {
                    isClosed = true;
                }
                else
                {
                    var title = driver.Title;
                }
            }
            catch (Exception)
            {
                isClosed = true;
            }

            return isClosed;
        }

        private void Check_Browser()
        {
            while (current_driver == null)
            {
                continue;
            }
            while (true)
            {

                if (IsBrowserClosed(current_driver))
                {
                    Invoke(new Action(() =>
                    {
                        in_conference = false;

                        button1.Enabled = true;
                        current_driver.Quit();
                    }));

                    current_driver = null;

                    break;
                }
            }
        }

        private void Conference_Checker()
        {

            while (true)
            {
                Thread.Sleep(2);

                if (currentPeriod == null)
                {
                    continue;
                }

                if (Schoology.HasConferenceStarted(currentPeriod))
                {
                    Invoke(new Action(() =>
                    {
                        if (!in_conference)
                        {
                            button1.Enabled = true;
                        }
                        label4.Text = "Conference has started!";
                        label4.Location = new System.Drawing.Point(590, 64);
                    }));
                }
                else
                {
                    Invoke(new Action(() =>
                    {
                        button1.Enabled = false;
                        label4.Text = "Conference has not started!";
                        label4.Location = new System.Drawing.Point(575, 64);
                    }));
                }
            }
        }

        private void InformationUpdater()
        {
            Thread.Sleep(2);
            while (true)
            {
                string upcoming = Schoology.NextPeriod();

                string starting_in = Schoology.NextPeriodIn();

                string current_period = Schoology.CurrentPeriod();

                currentPeriod = current_period;

                Invoke(new Action(() =>
                {
                    label6.Text = "Upcoming: " + upcoming;

                    label7.Text = "Next period: " + starting_in;

                    label5.Text = "Current: " + current_period;
                }));
            }

        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (thread_one.IsAlive)
            {
                thread_one.Abort();
            }
            if (thread_two.IsAlive)
            {
                thread_two.Abort();
            }
            if (thread_three != null && thread_three.IsAlive)
            {
                thread_three.Abort();
            }

            if (current_driver != null)
            {
                current_driver.Quit();
            }

            Application.Exit();

            base.OnClosing(e);
        }
    }
}
