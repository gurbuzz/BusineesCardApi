using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessCardAPI.Services
{
    public class OllamaClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<OllamaClient> _logger;
        private const string OllamaUrl = "http://localhost:11434/api/generate";

        public OllamaClient(HttpClient httpClient, IConfiguration config, ILogger<OllamaClient> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
        }

        public async Task<string> SendRequestAsync(string prompt)
        {
            var requestBody = new
            {
                model = "llama3.2:1b",
                prompt = prompt,
                stream = false,
                max_tokens = 200
            };

            // Gerekliyse API_KEY kullanılıyor
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config["API_KEY"]);

            _logger.LogInformation("Ollama'ya istek gönderiliyor. URL: {Url}", OllamaUrl);
            _logger.LogInformation("Request Body: {RequestBody}", JsonSerializer.Serialize(requestBody));

            var response = await _httpClient.PostAsJsonAsync(OllamaUrl, requestBody);
            _logger.LogInformation("Yanıt Kodu: {StatusCode}", response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Yanıt İçeriği: {ResponseContent}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Ollama API Hatası: {StatusCode} - {ResponseContent}", response.StatusCode, responseContent);
                throw new HttpRequestException($"Ollama API Hatası: {response.StatusCode}");
            }

            using var rootJson = JsonDocument.Parse(responseContent);
            var rootElement = rootJson.RootElement;

            string? extractedText = null;
            if (rootElement.TryGetProperty("response", out var respProp))
            {
                extractedText = respProp.GetString();
            }

            return extractedText ?? string.Empty;
        }

        public async Task<string> SendRawRequestAsync(string prompt)
        {
            // İhtiyaç duyulursa ham metni almak için de aynı metodu kullanabiliriz.
            return await SendRequestAsync(prompt);
        }
    }
}
