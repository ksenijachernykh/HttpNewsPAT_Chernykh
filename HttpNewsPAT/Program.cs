using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace HttpNewsPAT
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Cookie token = SingIn("user", "user");
            string Content = GetContent(token);
            ParsingHtml(Content);
            /*WebRequest request = WebRequest.Create("http://10.111.20.114/main.php");
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Console.WriteLine(response.StatusDescription);
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();
            Console.WriteLine(responseFromServer);
            reader.Close();
            dataStream.Close();
            response.Close();*/
            Console.Read();
        }

        public static Cookie SingIn(string Login, string Password)
        {
            Cookie token = null;
            string Url = "http://10.111.20.114/ajax/login.php";
            Debug.WriteLine($"Выполняем запрос: {Url}");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.CookieContainer = new CookieContainer();
            string postData = $"login={Login}&password={Password}";
            byte[] Data = Encoding.ASCII.GetBytes(postData);
            request.ContentLength = Data.Length;
            using (var stream = request.GetRequestStream())
            {
                stream.Write(Data, 0, Data.Length);
            }
            HttpWebResponse Response = (HttpWebResponse)request.GetResponse();
            Debug.WriteLine($"Статус выполнения: {Response.StatusCode}");
            string responseFromServer = new StreamReader(Response.GetResponseStream()).ReadToEnd();
            token= Response.Cookies["token"];
            return token;
        }

        public static string GetContent(Cookie Token)
        {
            string Content = null;
            string uri = "http://10.111.20.114/main.php";
            Debug.WriteLine($"Выполняем запрос: {uri}");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(Token);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Debug.WriteLine($"Статус выполения: {response.StatusCode}");
            Content = new StreamReader(response.GetResponseStream()).ReadToEnd();
            return Content;
        }
        public static void ParsingHtml(string htmlCode)
        {
            var html = new HtmlDocument();
            html.LoadHtml(htmlCode);
            var Document = html.DocumentNode;
            IEnumerable DivsNews = Document.Descendants(0).Where(n => n.HasClass("news"));
            foreach (HtmlNode DivNews in DivsNews){
                var src = DivNews.ChildNodes[1].GetAttributeValue("src", "none");
                var name = DivNews.ChildNodes[3].InnerText;
                var description = DivNews.ChildNodes[5].InnerText;
                Console.WriteLine(name + "\n" + "Изображение: " + src + "\n" + "Описание: " + description + "\n");
            }
        }
    }
}
