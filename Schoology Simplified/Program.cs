﻿using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Schoology_Simplified
{

    class Schoology {

        public static ChromeOptions options = new ChromeOptions();

        public static IEnumerable<System.Net.Cookie> responseCookies;
        public static IWebDriver chrome;

        public static string path = System.IO.Path.GetTempPath() + "schoology-simplified/";

        private static readonly CookieContainer cookies = new CookieContainer();

        private static Dictionary<string, Dictionary<string, string>> schedule;
        public static Dictionary<string, string> course_to_id;

        public static ChromeDriverService driverService;

        public Schoology()
        {

            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }

            if (!File.Exists(path + "chromedriver.exe"))
            {
                byte[] bytes = Properties.Resources.chromedriver;
                File.WriteAllBytes(path + "chromedriver.exe", bytes);
            }

            ChromeOptions options_two = new ChromeOptions();
            options_two.AddArgument("start-maximized");
            //options_two.AddArgument("headless");

            driverService = ChromeDriverService.CreateDefaultService(path);
            driverService.HideCommandPromptWindow = true;

            chrome = new ChromeDriver(path, options_two);

        }

        public static async Task<HtmlAgilityPack.HtmlDocument> LogIn(string username, string password, string grade, ProgressBar pb=null)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                CookieContainer = cookies
            };

            HttpClient client = new HttpClient(handler);

            if (pb != null)
            {
                pb.Invoke((MethodInvoker)delegate {
                    pb.Value = 10;
                });
            }

            string responseString = await client.GetStringAsync("https://inpsa.schoology.com/login?&school=2382278049");

            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();

            document.LoadHtml(responseString);

            if (pb != null)
            {
                pb.Invoke((MethodInvoker)delegate
                {
                    pb.Value = 20;
                });
            }

            string form_id = document.DocumentNode.Descendants("input").Where(node => node.Attributes["name"].Value == "form_build_id").FirstOrDefault().Attributes["value"].Value;

            var values = new Dictionary<string, string>
            {
                { "mail", username },
                { "pass", password },
                { "school_nid", "2382278049" },
                { "form_id", "s_user_login_form" },
                { "form_build_id", form_id },
            };


            var content = new FormUrlEncodedContent(values);

            if (pb != null)
            {
                pb.Invoke((MethodInvoker)delegate
                {
                    pb.Value = 25;
                });
            }

            var response = await client.PostAsync("https://inpsa.schoology.com/login?&school=2382278049", content);

            var responseStringPOST = await response.Content.ReadAsStringAsync();

            document.LoadHtml(responseStringPOST);

            if (document.GetElementbyId("login-container") != null)
            {
                return null;
            }

            if (pb != null)
            {
                pb.Invoke((MethodInvoker)delegate
                {
                    pb.Value = 40;
                });
            }

            responseCookies = cookies.GetCookies(new Uri("https://inpsa.schoology.com")).Cast<System.Net.Cookie>();

            chrome.Url = "https://inpsa.schoology.com/login?&school=2382278049";

            if (pb != null)
            {
                pb.Invoke((MethodInvoker)delegate
                {
                    pb.Value = 60;
                });
            }

            foreach (System.Net.Cookie cookie in responseCookies)
            {

                chrome.Manage().Cookies.AddCookie(new OpenQA.Selenium.Cookie(cookie.Name, cookie.Value));

            }

            chrome.Url = "https://inpsa.schoology.com/login?&school=2382278049";

            if (pb != null)
            {
                pb.Invoke((MethodInvoker)delegate
                {
                    pb.Value = 80;
                });
            }

            chrome.FindElement(By.CssSelector("button._1SIMq._2kpZl._3OAXJ._13cCs._3_bfp._2M5aC._24avl._3v0y7._2s0LQ._3ghFm._3LeCL._31GLY._9GDcm._1D8fw.util-height-six-3PHnk.Z_KgC.fjQuT.uQOmx")).Click();

            chrome.FindElement(By.CssSelector("a._2JX1Q._3VHSs._1k0yk._3_bfp._1tpub.dVlNp._3v0y7._3eD4l._3ghFm._3LeCL._3lLLU._2gJbx.util-text-decoration-none-1n0lI")).Click();

            document.LoadHtml(chrome.PageSource);

            client.Dispose();

            if (pb != null)
            {
                pb.Invoke((MethodInvoker)delegate
                {
                    pb.Value = 95;
                });
            }

            File.WriteAllText(path + "information.json", "{\"username\": \"" + username + "\", \"password\": \"" + password + "\",\"grade\":\"" + grade + "\"}");

            await SetScheduleAsync(grade);

            return document;
        }

        public static string NextPeriodIn()
        {
            DateTime now = DateTime.Now;

            DateTime finalTime = new DateTime(now.Year, now.Month, now.Day, 13, 10, 0);

            int daystoadd = 0;

            string day;

            if (now > finalTime)
            {
                daystoadd++;
                day = DateTime.Now.AddDays(1).ToString("dddd");
            }
            else
            {
                day = DateTime.Now.ToString("dddd");
            }

            switch (day)
            {
                case "Saturday":
                    daystoadd++;
                    day = "Sunday";
                    break;
                case "Friday":
                    daystoadd += 2;
                    day = "Sunday";
                    break;
            }

            Dictionary<string, string> todaySchedule = schedule[day];

            DateTime nextPeriod = now;

            foreach (KeyValuePair<string, string> entry in todaySchedule)
            {
                string time = entry.Key;

                string hours = time.Substring(0, time.IndexOf(":"));

                string minutes = time.Substring(time.IndexOf(":") + 1);

                DateTime fakeTime = new DateTime(now.Year, now.Month, now.Day + daystoadd, int.Parse(hours), int.Parse(minutes), 0);

                if (fakeTime >= now)
                {
                    nextPeriod = fakeTime;
                    break;
                }

            }

            TimeSpan margin = TimeSpan.FromSeconds((nextPeriod - now).TotalSeconds);
            string answer = string.Format("{0:D1}:{1:D2}:{2:D2}:{3:D2}",
                                            margin.Days,
                                            margin.Hours,
                                            margin.Minutes,
                                            margin.Seconds);

            return answer;

        }

        public static string NextPeriod()
        {
            DateTime now = DateTime.Now;

            DateTime finalTime = new DateTime(now.Year, now.Month, now.Day, 14, 0, 0);

            int daystoadd = 0;

            string day;

            if (now > finalTime)
            {
                day = DateTime.Now.AddDays(1).ToString("dddd");
                daystoadd++;
            }
            else
            {
                day = DateTime.Now.ToString("dddd");
            }

            switch (day)
            {
                case "Saturday":
                    daystoadd++;
                    day = "Sunday";
                    break;
                case "Friday":
                    daystoadd += 2;
                    day = "Sunday";
                    break;
            }

            Dictionary<string, string> todaySchedule = schedule[day];

            string nextPeriod = null;

            foreach (KeyValuePair<string, string> entry in todaySchedule)
            {
                string time = entry.Key;

                string hours = time.Substring(0, time.IndexOf(":"));

                string minutes = time.Substring(time.IndexOf(":") + 1);

                DateTime fakeTime = new DateTime(now.Year, now.Month, now.Day + daystoadd, int.Parse(hours), int.Parse(minutes), 0);

                if (fakeTime >= now)
                {
                    nextPeriod = entry.Value;
                    break;
                }

            }

            return nextPeriod;
        }

        public static string CurrentPeriod()
        {
            DateTime now = DateTime.Now;

            DateTime finalTime = new DateTime(now.Year, now.Month, now.Day, 14, 0, 0);

            string day = DateTime.Now.ToString("dddd");

            switch (day)
            {
                case "Saturday":
                    return null;
                case "Friday":
                    return null;
            }

            if (now >= finalTime)
            {
                return null;
            }

            Dictionary<string, string> todaySchedule = schedule[day];

            string currentPeriod = null;

            foreach (KeyValuePair<string, string> entry in todaySchedule)
            {
                string time = entry.Key;

                string hours = time.Substring(0, time.IndexOf(":"));

                string minutes = time.Substring(time.IndexOf(":") + 1);

                DateTime fakeTime = new DateTime(now.Year, now.Month, now.Day, int.Parse(hours), int.Parse(minutes), 0);

                if (now >= fakeTime)
                {
                    currentPeriod = entry.Value;
                }
                else
                {
                    break;
                }

            }

            return currentPeriod;
        }

        public static string GetConferenceLink(string course)
        {
            string courseID = course_to_id[course];

            chrome.Url = "https://inpsa.schoology.com/apps/login/saml/initial?realm=course&realm_id=" + courseID + "&spentityid=9295f7b9ba9a31af8c09d5442f697eb005452c17d&RelayState=https%3A%2F%2Fbigbluebutton.app.schoology.com%2Fhome%3Frealm%3Dsection%26realm_id%3D" + courseID + "%26app_id%3D191034318%26is_ssl%3D1";

            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();

            document.LoadHtml(chrome.PageSource);

            HtmlAgilityPack.HtmlNode conference = document.DocumentNode.Descendants("tr").Where(node => node.HasClass("conference-row")).FirstOrDefault();

            if (conference == null)
            {
                return null;
            }

            string is_running = conference.Descendants("span").Where(node => node.HasClass("conference-status")).FirstOrDefault().InnerText;

            if (is_running == "Not started")
            {
                return null;
            }

            var element = conference.Descendants("a").Where(node => node.HasClass("ng-binding")).FirstOrDefault();

            while (element == null)
            {
                element = conference.Descendants("a").Where(node => node.HasClass("ng-binding")).FirstOrDefault();
            }

            string code = element.Attributes["href"].Value;

            string url = "https://bigbluebutton.app.schoology.com/" + code;

            return url;
        }

        public static bool HasConferenceStarted(string course)
        {
            if (course == "Break" || course == "Break 2")
            {
                return false;
            }

            string courseID = course_to_id[course];

            chrome.Url = "https://inpsa.schoology.com/apps/login/saml/initial?realm=course&realm_id=" + courseID + "&spentityid=9295f7b9ba9a31af8c09d5442f697eb005452c17d&RelayState=https%3A%2F%2Fbigbluebutton.app.schoology.com%2Fhome%3Frealm%3Dsection%26realm_id%3D" + courseID + "%26app_id%3D191034318%26is_ssl%3D1";

            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();

            document.LoadHtml(chrome.PageSource);

            HtmlAgilityPack.HtmlNode conference = document.DocumentNode.Descendants("tr").Where(node => node.HasClass("conference-row")).FirstOrDefault();

            if (conference == null)
            {
                return false;
            }

            string is_running = conference.Descendants("span").Where(node => node.HasClass("conference-status")).FirstOrDefault().InnerText;

            return is_running != "Not started";
        }

        private static async Task SetScheduleAsync(string grade)
        {

            var path = System.IO.Path.GetTempPath();

            if (File.Exists(path + "schoology/schedule.json"))
            {
                return;
            }

            HttpClient client = new HttpClient();

            string response = await client.GetStringAsync("https://raw.githubusercontent.com/Am4nso/schoology-simplified-database/main/schedule.json");

            Dictionary<string, Dictionary<string, Dictionary<string, string>>> product = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(response);

            schedule = product[grade];

            string secondResponse = await client.GetStringAsync("https://raw.githubusercontent.com/Am4nso/schoology-simplified-database/main/course_to_id.json");

            Dictionary<string, Dictionary<string, string>> newProduct = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(secondResponse);

            course_to_id = newProduct[grade];
        }
    }


    static class Program
    {

        [STAThread]
        static void Main()
        {
            // Initializes Schoology
            Schoology schoology = new Schoology();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (File.Exists(Schoology.path + "information.json"))
            {
                Application.Run(new LoadingPage());
                return;
            }

            Application.Run(new LoginPage());
        }

    }
}
