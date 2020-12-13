using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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


        private static readonly CookieContainer cookies = new CookieContainer();

        private static Dictionary<string, Dictionary<string, string>> schedule;
        public static Dictionary<string, string> course_to_id;

        public static ChromeDriverService driverService;

        public Schoology()
        {

            var path = System.IO.Path.GetTempPath();

            if (!Directory.Exists(path + "schoology/")) {
                Directory.CreateDirectory(path + "schoology/");
            }


            if (File.Exists(path + "schoology/schedule.json"))
            {
                string scheduleText = File.ReadAllText(path + "schoology/schedule.json");

                schedule = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(scheduleText);

                string courseText = File.ReadAllText(path + "schoology/course_to_id.json");

                course_to_id = JsonConvert.DeserializeObject<Dictionary<string, string>>(courseText);
            }

            if (!File.Exists(path + "schoology/chromedriver.exe"))
            {
                byte[] bytes = Properties.Resources.chromedriver;
                File.WriteAllBytes(path + "schoology/chromedriver.exe", bytes);
            }

            ChromeOptions options_two = new ChromeOptions();
            options_two.AddArgument("start-maximized");
            options_two.AddArgument("headless");

            driverService = ChromeDriverService.CreateDefaultService(path + "schoology/");
            driverService.HideCommandPromptWindow = true;

            chrome = new ChromeDriver(driverService, options_two);

        }

        public static async Task<HtmlAgilityPack.HtmlDocument> LogIn(string username, string password, string grade)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                CookieContainer = cookies
            };

            HttpClient client = new HttpClient(handler);

            string responseString = await client.GetStringAsync("https://inpsa.schoology.com/login?&school=2382278049");

            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();

            document.LoadHtml(responseString);

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

            var response = await client.PostAsync("https://inpsa.schoology.com/login?&school=2382278049", content);

            var responseStringPOST = await response.Content.ReadAsStringAsync();

            HtmlAgilityPack.HtmlDocument documentPOST = new HtmlAgilityPack.HtmlDocument();

            document.LoadHtml(responseStringPOST);

            if (document.GetElementbyId("login-container") != null)
            {
                return null;
            }

            responseCookies = cookies.GetCookies(new Uri("https://inpsa.schoology.com")).Cast<System.Net.Cookie>();

            HtmlAgilityPack.HtmlDocument finalDocument;

            chrome.Url = "https://inpsa.schoology.com/login?&school=2382278049";

            foreach (System.Net.Cookie cookie in responseCookies)
            {

                chrome.Manage().Cookies.AddCookie(new OpenQA.Selenium.Cookie(cookie.Name, cookie.Value));

            }

            chrome.Url = "https://inpsa.schoology.com/login?&school=2382278049";

            chrome.FindElement(By.CssSelector("button._1SIMq._2kpZl._3OAXJ._13cCs._3_bfp._2M5aC._24avl._3v0y7._2s0LQ._3ghFm._3LeCL._31GLY._9GDcm._1D8fw.util-height-six-3PHnk.Z_KgC.fjQuT.uQOmx")).Click();

            chrome.FindElement(By.CssSelector("a._2JX1Q._3VHSs._1k0yk._3_bfp._1tpub.dVlNp._3v0y7._3eD4l._3ghFm._3LeCL._3lLLU._2gJbx.util-text-decoration-none-1n0lI")).Click();

            finalDocument = new HtmlAgilityPack.HtmlDocument();

            finalDocument.LoadHtml(chrome.PageSource);

            client.Dispose();

            var path = System.IO.Path.GetTempPath();
            File.WriteAllText(path + "schoology/information.json", "{\"username\": \"" + username + "\", \"password\": \"" + password + "\",\"grade\":\"" + grade + "\"}");

            await SetScheduleAsync(grade);

            return finalDocument;
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

            File.WriteAllText(path + "schoology/schedule.json", JsonConvert.SerializeObject(product[grade]));

            schedule = product[grade];

            string secondResponse = await client.GetStringAsync("https://raw.githubusercontent.com/Am4nso/schoology-simplified-database/main/course_to_id.json");

            Dictionary<string, Dictionary<string, string>> newProduct = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(secondResponse);

            File.WriteAllText(path + "schoology/course_to_id.json", JsonConvert.SerializeObject(newProduct[grade]));
            course_to_id = newProduct[grade];
        }
    }


    static class Program
    {

        [STAThread]
        static async Task Main()
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnApplicationExit);

            // Initializes Schoology
            Schoology schoology = new Schoology();

            var path = System.IO.Path.GetTempPath();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (File.Exists(path + "schoology/information.json"))
            {
                string information = File.ReadAllText(path + "schoology/information.json");

                Dictionary<string, string> product = JsonConvert.DeserializeObject<Dictionary<string, string>>(information);

                var document = await Schoology.LogIn(product["username"], product["password"], product["grade"]);

                Application.Run(new HomePage(document, product["grade"]));

                return;

            }

            Application.Run(new LoginPage());
        }

        private static void OnApplicationExit(object sender, EventArgs e)
        {
            try
            {
                Schoology.chrome.Quit();
            }
            catch (Exception) { return; }
        }
    }
}
