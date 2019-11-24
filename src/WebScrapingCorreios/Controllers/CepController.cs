using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebScrapingCorreios.Models;

namespace WebScrapingCorreios.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class CepController : BaseController
    {
        private readonly Regex rx = new Regex(@"[0-9]{5}[\d]{3}");

        public CepController(IOptions<AppSettings> settings)
            :base(settings) { }

        /// <summary>
        /// Retorna o endereço completo
        /// </summary>
        /// <param name="cep">Número do CEP sem '-' (traço)</param>
        /// <returns>JSON contendo todas as informações</returns>
        [HttpGet("{cep}")]
        public async Task<IActionResult> GetByCep(string cep)
        {
            if (!rx.IsMatch(cep))
            {
                return StatusCode(204);
            }

            try
            {
                var dict = new Dictionary<string, string>
                {
                    {"relaxation",cep},
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
                var name = htmlDoc.DocumentNode.SelectNodes("//td");
                if (name == null)
                {
                    return StatusCode(204, new { Descricao = "Não Encontrado"});
                }

                string bairro = name[1].InnerText.Replace("&nbsp;", string.Empty);
                string numeroCep = name[3].InnerText.Replace("&nbsp;", string.Empty);
                string cidade = name[2].InnerText.Replace("&nbsp;", string.Empty);
                string rua = name[0].InnerText.Replace("&nbsp;", string.Empty);

                var responseCep = new ResponseCep
                {
                    Bairro = bairro,
                    Cep = numeroCep,
                    Cidade = cidade,
                    Rua = rua
                };

                return Ok(responseCep);
            }
            catch (Exception ex)
            {

                return StatusCode(500, ex.Message);
            }
        }
    }
}