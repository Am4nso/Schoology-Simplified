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
using System.Timers;
using System.Windows.Forms;

namespace Schoology_Simplified
{
    public partial class HomePage : Form
    {

        private string currentPeriod;
        private readonly System.Timers.Timer myTimer = new System.Timers.Timer(500);

        private bool in_conference = false;

        private IWebDriver current_driver;

        public HomePage(HtmlAgilityPack.HtmlDocument document, string grade)
        {

            string student_name = document.DocumentNode.Descendants("h2").Where(node => node.GetClasses().Contains("page-title")).FirstOrDefault().InnerText;

            string first_name = student_name.Split(' ')[0];

            InitializeComponent();

            label3.Text = grade;

            this.Text = "Schoology Simplified - " + first_name;

            label2.Text = "Welcome back, " + first_name + ".";

            pictureBox3.Load("https://github.com/Am4nso/schoology-simplified-database/blob/main/schedules/" + grade + ".PNG?raw=true");

        }


        private void HomePage_Load(object sender, EventArgs e)
        {
            myTimer.Elapsed += InformationUpdater;
            myTimer.Elapsed += ConferenceCheck;
            myTimer.Start();
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

                    foreach (System.Net.Cookie cookie in Schoology.responseCookies)
                    {

                        current_driver.Manage().Cookies.AddCookie(new Cookie(cookie.Name, cookie.Value));

                    }

                    current_driver.Url = "https://inpsa.schoology.com/apps/login/saml/initial?realm=course&realm_id=" + courseID + "&spentityid=9295f7b9ba9a31af8c09d5442f697eb005452c17d&RelayState=https%3A%2F%2Fbigbluebutton.app.schoology.com%2Fhome%3Frealm%3Dsection%26realm_id%3D" + courseID + "%26app_id%3D191034318%26is_ssl%3D1";

                    current_driver.Url = url;
                }
                catch (Exception)
                {
                    if (current_driver != null)
                    {
                        current_driver.Quit();
                    }

                    Invoke(new Action(() =>
                    {
                        in_conference = false;
                        button1.Enabled = true;
                    }));
                }

            }).Start();
        }

        public static bool IsBrowserClosed(IWebDriver driver)
        {
            bool isClosed = false;
            try
            {
                var title = driver.Title;
            }
            catch (Exception)
            {
                isClosed = true;
            }

            return isClosed;
        }

        private void ConferenceCheck(object sender, ElapsedEventArgs e) {
            if (currentPeriod == null)
            {
                return;
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

            if (current_driver != null && in_conference)
            {
                if (IsBrowserClosed(current_driver))
                {
                    current_driver = null;

                    Invoke(new Action(() =>
                    {
                        current_driver.Quit();
                        in_conference = false;

                        button1.Enabled = true;
                    }));
                }
            }

        }

        private void InformationUpdater(object sender, ElapsedEventArgs e)
        {
            string upcoming = Schoology.NextPeriod();

            string starting_in = Schoology.NextPeriodIn();

            string current_period = Schoology.CurrentPeriod();

            Invoke(new Action(() =>
            {
                label6.Text = "Upcoming: " + upcoming;

                label7.Text = "Next period: " + starting_in;

                if (string.IsNullOrEmpty(current_period))
                {

                    label5.Text = "Current: None";
                    currentPeriod = null;
                }
                else
                {
                    label5.Text = "Current: " + current_period;
                    currentPeriod = current_period;
                }
            }));
        }

        private void HomePage_FormClosing(object sender, FormClosingEventArgs e)
        {

            if (Schoology.chrome != null)
            {
                Schoology.chrome.Quit();
            }

            if (current_driver != null)
            {
                current_driver.Quit();
            }

            Environment.Exit(Environment.ExitCode);
        }
    }
}
