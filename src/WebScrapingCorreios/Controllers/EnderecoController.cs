using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebScrapingCorreios.Models;

namespace WebScrapingCorreios.Controllers
{
    [Route("api/v1[controller]")]
    [ApiController]
    public class EnderecoController : BaseController
    {
        public EnderecoController(IOptions<AppSettings> settings)
            :base(settings) {}

        /// <summary>
        /// Retorna todas as informações
        /// </summary>
        /// <param name="endereco">Nome da Rua/Avenida sem o número</param>
        /// <returns></returns>
        [HttpGet("{endereco}")]
        public async Task<IActionResult> GetByEndereco(string endereco)
        {
            try
            {
                var lstEnderecos = new List<ResponseEndereco>();
                bool hasNextPage = false;
                var hsRespEndereco = new HashSet<ResponseEndereco>();
                var dict = new Dictionary<string, string>
                {
                    {"relaxation",endereco},
                    {"tipoCEP","ALL" },
                    {"semelhante","N" }
                };

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, UrlCorreio)
                {
                    Content = new FormUrlEncodedContent(dict)
                };

                var httpResponse = await _client.SendAsync(httpRequest).Result.Content.ReadAsStringAsync();
                var html = httpResponse;

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);
                var link = htmlDoc.DocumentNode.SelectNodes("//div//a");

                if (link == null)
                {
                    return StatusCode(204, new { Descricao = "Não Encontrado" });
                }

                for (int i = 0; i < link.Count; i++)
                {
                    if (link[i].InnerHtml.Contains("[ Próxima ]"))
                    {
                        hasNextPage = true;
                    }
                }

                var name = htmlDoc.DocumentNode.SelectNodes("//td");
                var divCtrlContent = htmlDoc.DocumentNode.SelectNodes("//div[@class='ctrlcontent']");
                var numPages = divCtrlContent[0].OuterHtml.Substring(1068,13);

                if (name == null)
                {
                    return StatusCode(204, new { Descricao = "Não Encontrado" });
                }

                for (int i = 0; i < name.Count; i++)
                {
                    if (i % 4 == 0)
                    {
                        var reponseEndereco = new ResponseEndereco
                        {
                            Bairro = name[i + 1].InnerText.Replace("&nbsp;", string.Empty),
                            Cep = name[i + 3].InnerText.Replace("&nbsp;", string.Empty),
                            Cidade = name[i + 2].InnerText.Replace("&nbsp;", string.Empty),
                            Rua = name[i].InnerText.Replace("&nbsp;", string.Empty),
                        };

                        hsRespEndereco.Add(reponseEndereco);
                    }
                }

                if (hasNextPage)
                {
                    AcessaProximasPaginas(hsRespEndereco, endereco, numPages);
                }

                return Ok(hsRespEndereco);

            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        private void AcessaProximasPaginas(HashSet<ResponseEndereco> hsRespEnd, string endereco,
                                            string numPages, int pageIni = 51, int pageFim = 100)
        {
            bool hasNextPage = false;
            var dict = new Dictionary<string, string>
            {
                {"relaxation", endereco},
                {"tipoCEP", "ALL"},
                {"semelhante", "N"},
                {"qtdrow", "50"},
                {"pagIni", pageIni.ToString()},
                {"pagFim", pageFim.ToString()}
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, UrlCorreio)
            {
                Content = new FormUrlEncodedContent(dict)
            };

            var httpResponse = _client.SendAsync(httpRequest).Result.Content.ReadAsStringAsync();

            var html = httpResponse.Result;

            var docHtml = new HtmlDocument();
            docHtml.LoadHtml(html);

            var name = docHtml.DocumentNode.SelectNodes("//td");

            if (!hasNextPage)
            {
                var link = docHtml.DocumentNode.SelectNodes("//div//a");

                for (int i = 0; i < link.Count; i++)
                {
                    if (link[i].InnerHtml.Contains("[ Próxima ]"))
                    {
                        hasNextPage = true;
                        pageIni += 25;
                        pageFim += 25;
                    }
                }
            }

            for (int i = 0; i < name.Count; i++)
            {
                if (i % 4 == 0)
                {
                    var reponseEndereco = new ResponseEndereco
                    {
                        Bairro = name[i + 1].InnerText.Replace("&nbsp;", string.Empty),
                        Cep = name[i + 3].InnerText.Replace("&nbsp;", string.Empty),
                        Cidade = name[i + 2].InnerText.Replace("&nbsp;", string.Empty),
                        Rua = name[i].InnerText.Replace("&nbsp;", string.Empty),
                    };

                    hsRespEnd.Add(reponseEndereco);
                }
            }

            if (hasNextPage)
            {
                AcessaProximasPaginas(hsRespEnd, endereco, numPages, pageIni, pageFim);
            }


        }
    }
}