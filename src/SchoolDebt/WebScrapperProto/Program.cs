using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace WebScrapperProto
{
    class Program
    {
        private static Uri root = new Uri("https://istu.ru/");
        private static string login = "******";
        private static string password = "******";
        static async Task Main(string[] args)
        {
            var cookieContainer = new CookieContainer();
            using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer }) 
            using(var client = new HttpClient(handler))
            {
                string token = string.Empty;
                
                using(var request = new HttpRequestMessage(HttpMethod.Get, new Uri(root, "login")))
                {
                    var response = await client.SendAsync(request);

                    var htmlString = await response.Content.ReadAsStringAsync();
                    var html = new HtmlDocument();
                    html.LoadHtml(htmlString);
                    var document = html.DocumentNode;

                    var tokenNode = document.QuerySelectorAll("#block-student > input[name=_token]").FirstOrDefault();
                    token = tokenNode.Attributes.FirstOrDefault(a => a.Name == "value").Value;
                }

                var loginUri = new Uri(root, $"login?_token={token}&email={login}&password={password}");
                using (var request = new HttpRequestMessage(HttpMethod.Post, loginUri))
                {
                   await client.SendAsync(request);
                }

                using(var request = new HttpRequestMessage(HttpMethod.Get, new Uri("https://istu.ru/cabinet/zachet")))
                {
                    var response = await client.SendAsync(request);

                    var html = new HtmlDocument();
                    html.LoadHtml(await response.Content.ReadAsStringAsync());

                    var document = html.DocumentNode;
                    var tableNodes = document.SelectNodes("//*[@id=\"app\"]/section[2]/div/div[2]/div/form/table");

                    var results = tableNodes.Select(node => new
                    {
                        Caption = node.Elements().FirstOrDefault(e => e.Name.Equals("caption", StringComparison.OrdinalIgnoreCase)).InnerText,
                        Rows = node.Elements().FirstOrDefault(e => e.Name.Equals("tbody", StringComparison.OrdinalIgnoreCase))
                                              .Elements().Where(e => e.Name.Equals("tr", StringComparison.OrdinalIgnoreCase))
                                              .Select(e => new 
                                              {
                                                  DisciplinePair = GeActionData(e.Elements().ElementAt(0)),
                                                  Type = e.Elements().ElementAt(1).InnerText,
                                                  Result = e.Elements().ElementAt(2).InnerText,
                                                  TeacherPair = GeActionData(e.Elements().ElementAt(3))
                                              })
                    });

                    foreach(var r in results)
                    {
                        Console.WriteLine($"{r.Caption}, {r.Rows.Count()}");

                        foreach(var row in r.Rows)
                        {
                            Console.WriteLine($"{row.DisciplinePair.Item2} - {row.Result} - {row.TeacherPair.Item2}");
                        }
                    }
                }
            }
        }

        private static (string, string)  GeActionData(HtmlNode node)
        {
            var aElement = node.Element("a");


            if (aElement != null)
            {
                var href = aElement?.GetAttributes("href")?.FirstOrDefault()?.Value;
                var text = aElement.InnerText.Trim();

                return (href, text);
            }

            return (null, null);
        }
    }
}
