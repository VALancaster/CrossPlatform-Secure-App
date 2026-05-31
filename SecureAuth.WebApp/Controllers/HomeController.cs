using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SecureAuth.WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace SecureAuth.WebApp.Controllers
{

    [Route("[controller]")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("")]
        [Route("Index")]
        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        [HttpGet("Privacy")]
        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost("UploadEcg")]
        public async Task<IActionResult> UploadEcg(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ViewBag.Error = "File is empty or not selected.";
                return View("Index");
            }

            _logger.LogInformation($"Received  file '{file.FileName}' from web client. Forwarding to API Gateway.");

            try
            {
                // HTTP-клиент дл€ отправки запроса к API-сервису
                using var client = new HttpClient();

                // получение JWT-токена из Cookie
                var token = Request.Cookies["JwtToken"];
                if (string.IsNullOrEmpty(token))
                {
                    ViewBag.Error = "You are not authorized. Please log in.";
                    return View("Index");
                }

                // прикрепление токена в заголовок запроса
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // упаковка файла в формат multipart/form-data
                using var content = new MultipartFormDataContent();
                using var fileStream = file.OpenReadStream();
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

                content.Add(fileContent, "file", file.FileName);

                // отправка запроса по внутренней сети Docker на шлюз 'api'
                var response = await client.PostAsync("http://api:8080/api/ecg/analyze", content);

                if (response.IsSuccessStatusCode)
                {
                    // получение JSON-ответа от шлюза
                    var jsonResponse = await response.Content.ReadAsStringAsync();

                    try
                    {
                        // парсинг строки в JSON
                        using var jsonDoc = JsonDocument.Parse(jsonResponse);
                        // включение отступов и переносов строк
                        var options = new JsonSerializerOptions { WriteIndented = true };
                        // сериализаци€ обратно в строку
                        ViewBag.Result = JsonSerializer.Serialize(jsonDoc, options);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to format JSON: {ex.Message}");
                        ViewBag.Result = jsonResponse;
                    }
                }
                else
                {
                    ViewBag.Error = $"Analysis error (Code: {response.StatusCode}). The token may be invalid.";
                }
            }
            catch(Exception ex)
            {
                _logger.LogError($"Failed to forward ECG file: {ex.Message}");
                ViewBag.Error = $"Internal server error while sending the file: {ex.Message}";
            }

            return View("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}
