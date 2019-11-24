using System;
using System.Net.Http;
using WebScrapingCorreios.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace WebScrapingCorreios.Controllers
{
    public class BaseController : ControllerBase
    {

        private AppSettings AppSettings { get; set; }
        protected HttpClient _client;

        public BaseController(IOptions<AppSettings> settings)
        {
            AppSettings = settings.Value;
            _client = new HttpClient() { Timeout = TimeSpan.FromMinutes(30) };
        }
        
        protected string UrlCorreio { get => AppSettings.UrlCorreio; }
        protected string UrlRastreio { get => AppSettings.UrlRastreio; }

    }
}
