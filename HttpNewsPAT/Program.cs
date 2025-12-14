using HtmlAgilityPack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HttpNewsPAT
{
    internal class Program
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        static async Task Main(string[] args)
        {
            Console.WriteLine("1.Авторизация и получение новостей");
            Console.WriteLine("2.Добавить новую новость вручную");
            Console.Write("Выберите действие: ");

            string choice = Console.ReadLine();

            if (choice == "1")
            {
                Cookie token = await SingInAsync("user", "user");
                string content = await GetContentAsync(token);
                var newsList = ParsingHtml(content);

                foreach (var news in newsList)
                {
                    Console.WriteLine($"Найдено: {news.Name}");
                }

                Console.Write("\nХотите добавить новость? (y/n): ");
                if (Console.ReadLine().ToLower() == "y")
                {
                    await AddNewsManuallyAsync(token);
                }
            }
            else if (choice == "2")
            {
                Cookie token = await SingInAsync("user", "user");
                await AddNewsManuallyAsync(token);
            }

            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        public class NewsItem
        {
            public string Src { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
        }

        public static async Task<Cookie> SingInAsync(string login, string password)
        {
            string url = "http://localhost:81/news/ajax/login.php";
            Debug.WriteLine($"Выполняем запрос: {url}");

            try
            {
                var cookieContainer = new CookieContainer();
                var handler = new HttpClientHandler
                {
                    CookieContainer = cookieContainer,
                    UseCookies = true
                };

                using (var client = new HttpClient(handler))
                {
                    var postData = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("login", login),
                        new KeyValuePair<string, string>("password", password)
                    });
                    var response = await client.PostAsync(url, postData);
                    Debug.WriteLine($"Статус выполнения: {response.StatusCode}");
                    var responseFromServer = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Авторизация успешна!");
                    var cookies = cookieContainer.GetCookies(new Uri(url));
                    var token = cookies["token"];
                    if (token != null)
                    {
                        return new Cookie(token.Name, token.Value, token.Path, token.Domain);
                    }

                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка авторизации: {ex.Message}");
                return null;
            }
        }

        public static async Task<string> GetContentAsync(Cookie token)
        {
            string url = "http://localhost:81/news/main.php";
            Debug.WriteLine($"Выполняем запрос: {url}");

            try
            {
                var cookieContainer = new CookieContainer();
                var handler = new HttpClientHandler
                {
                    CookieContainer = cookieContainer,
                    UseCookies = true
                };
                cookieContainer.Add(new Uri(url), new System.Net.Cookie(token.Name, token.Value, token.Path, token.Domain));

                using (var client = new HttpClient(handler))
                {
                    var response = await client.GetAsync(url);
                    Debug.WriteLine($"Статус выполнения: {response.StatusCode}");
                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStringAsync();
                    return content;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения контента: {ex.Message}");
                return null;
            }
        }

        public static List<NewsItem> ParsingHtml(string htmlCode)
        {
            var newsList = new List<NewsItem>();

            if (string.IsNullOrEmpty(htmlCode))
            {
                Console.WriteLine("HTML код пуст или не получен");
                return newsList;
            }

            try
            {
                var html = new HtmlDocument();
                html.LoadHtml(htmlCode);

                var Document = html.DocumentNode;
                IEnumerable<HtmlNode> DivsNews = Document.Descendants(0).Where(n => n.HasClass("news"));

                foreach (HtmlNode DivNews in DivsNews)
                {
                    var src = DivNews.ChildNodes[1].GetAttributeValue("src", "none");
                    var name = DivNews.ChildNodes[3].InnerText;
                    var description = DivNews.ChildNodes[5].InnerText;

                    Console.WriteLine($"\nНазвание: {name}");
                    Console.WriteLine($"Изображение: {src}");
                    Console.WriteLine($"Описание: {description}");

                    newsList.Add(new NewsItem
                    {
                        Src = src,
                        Name = name,
                        Description = description
                    });
                }

                Console.WriteLine($"\nВсего найдено новостей: {newsList.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка парсинга HTML: {ex.Message}");
            }

            return newsList;
        }

        public static async Task AddNewsManuallyAsync(Cookie token)
        {
            if (token == null)
            {
                Console.WriteLine("Ошибка: Токен не получен. Авторизуйтесь снова.");
                return;
            }

            Console.WriteLine("\n--- Добавление новой новости ---");
            Console.Write("Введите URL изображения: ");
            string src = Console.ReadLine();

            Console.Write("Введите название новости: ");
            string name = Console.ReadLine();

            Console.Write("Введите описание новости: ");
            string description = Console.ReadLine();

            Console.WriteLine("\n--- Проверьте введенные данные ---");
            Console.WriteLine($"Изображение: {src}");
            Console.WriteLine($"Название: {name}");
            Console.WriteLine($"Описание: {description}");
            Console.Write("\nДобавить эту новость? (y/n): ");

            if (Console.ReadLine().ToLower() != "y")
            {
                Console.WriteLine("Добавление отменено.");
                return;
            }

            bool result = await AddNewsToDatabaseAsync(new NewsItem
            {
                Src = src,
                Name = name,
                Description = description
            }, token);

            if (result)
            {
                Console.WriteLine($"\nНовость '{name}' успешно добавлена!");
            }
            else
            {
                Console.WriteLine($"\nОшибка при добавлении новости '{name}'");
            }
        }

        public static async Task<bool> AddNewsToDatabaseAsync(NewsItem news, Cookie token)
        {
            try
            {
                string url = "http://localhost:81/news/ajax/add.php";
                Debug.WriteLine($"Добавляем новость: {news.Name}");
                var cookieContainer = new CookieContainer();
                var handler = new HttpClientHandler
                {
                    CookieContainer = cookieContainer,
                    UseCookies = true
                };
                cookieContainer.Add(new Uri(url), new System.Net.Cookie(token.Name, token.Value, token.Path, token.Domain));

                using (var client = new HttpClient(handler))
                {
                    var postData = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("src", news.Src),
                        new KeyValuePair<string, string>("name", news.Name),
                        new KeyValuePair<string, string>("description", news.Description)
                    });
                    var response = await client.PostAsync(url, postData);
                    Debug.WriteLine($"Статус добавления: {response.StatusCode}");
                    string responseFromServer = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Ответ сервера: {responseFromServer}");

                    Console.WriteLine($"Сервер ответил: {responseFromServer}");

                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Ошибка HTTP запроса: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Внутренняя ошибка: {ex.InnerException.Message}");
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при добавлении новости: {ex.Message}");
                Console.WriteLine($"Ошибка: {ex.Message}");
                return false;
            }
        }
    }
}
