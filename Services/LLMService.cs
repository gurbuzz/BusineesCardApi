using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using BusinessCardAPI.DTOs;
using BusinessCardAPI.Models;

namespace BusinessCardAPI.Services
{
    public partial class LLMService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<LLMService> _logger;

        public LLMService(HttpClient httpClient, IConfiguration config, ILogger<LLMService> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
        }

        public async Task<CardResponseDto> SendToLLM(CardRequestDto requestDto, string workspaceSlug)
        {
            _logger.LogInformation("=== [LLMService.SendToLLM] Başladı ===");

            string? apiKey = _config["API_KEY"];
            string? baseUrl = _config["BaseUrl"];
            string requestUrl = $"{baseUrl}/{workspaceSlug}/chat";

            if (string.IsNullOrWhiteSpace(requestDto.Mode))
            {
                requestDto.Mode = "chat";
            }

            // AI modelden JSON formatında cevap almak için mesajın başına ek bilgi ekleniyor.
            requestDto.Message = "find to “full name”, “titles”,“organization” , “phone”, “email”,“Adress”, “webAddress”\n" + requestDto.Message;

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            _logger.LogInformation("API isteği gönderiliyor. URL: {Url}", requestUrl);
            _logger.LogInformation("İstek Body (RequestDto): {RequestBody}", JsonSerializer.Serialize(requestDto));

            var response = await _httpClient.PostAsJsonAsync(requestUrl, requestDto);
            _logger.LogInformation("Yanıt Kodu: {StatusCode}", response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Ham Yanıt (JSON): {ResponseJson}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("API Hatası: {StatusCode} - {ResponseJson}", response.StatusCode, responseContent);
                throw new HttpRequestException($"API Hatası: {response.StatusCode}");
            }

            using var rootJson = JsonDocument.Parse(responseContent);
            var rootElement = rootJson.RootElement;

            string? textResponse = null;
            if (rootElement.TryGetProperty("textResponse", out var textRespProp))
            {
                textResponse = textRespProp.GetString();
            }

            if (string.IsNullOrWhiteSpace(textResponse))
            {
                _logger.LogWarning("textResponse alanı boş döndü, boş CardData dönüyor.");
                _logger.LogInformation("=== [LLMService.SendToLLM] Bitti ===");
                return new CardResponseDto { CardData = new BusinessCard() };
            }

            _logger.LogInformation("textResponse Ham İçerik: {Text}", textResponse);

            // textResponse metnini BusinessCard nesnesine dönüştürüyoruz.
            var card = ParseTextResponseToBusinessCard(textResponse);

            _logger.LogInformation("Çıkan Son CardData: {Result}", JsonSerializer.Serialize(card));
            _logger.LogInformation("=== [LLMService.SendToLLM] Bitti ===");

            return new CardResponseDto { CardData = card };
        }

        /// <summary>
        /// LLM'den gelen ham metni satır satır işleyip BusinessCard nesnesine dönüştürür.
        /// Beklenen format örneği:
        /// 
        /// Here are the extracted information:
        /// 
        /// * Full Name: Fahri ÖZSUNGUR
        /// * Titles: Doçenti (Professor), Doç. Dr.
        /// * Organization: Sosyal Hizmet (Social Service)
        /// * Phone: #90 324 361 00 O1 - 15317, *90 324 36108 88 0532 258 19
        /// * Email: fahriozsungurE@gmail.com
        /// * Address: Çiftlikköy Kampüsü, Mersin Üniversitesi, Mersin, Türkiye (33343)
        /// * WebAddress: Not available
        /// </summary>
        private BusinessCard ParseTextResponseToBusinessCard(string text)
        {
            var card = new BusinessCard();

            // Metni satırlara bölüyoruz.
            var lines = text.Split('\n');

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Sadece "*" ile başlayan satırları işliyoruz.
                if (!trimmedLine.StartsWith("*"))
                    continue;

                // "*" karakterini kaldırıp kalan kısmı temizliyoruz.
                var content = trimmedLine.Substring(1).Trim();

                // İlk ":" karakteri anahtar-değer ayrımı için.
                int colonIndex = content.IndexOf(':');
                if (colonIndex <= 0)
                    continue;

                var key = content.Substring(0, colonIndex).Trim().ToLower();
                var value = content.Substring(colonIndex + 1).Trim();

                _logger.LogInformation("Ayrıştırılan Alan -> Key: {Key}, Value: {Value}", key, value);

                if (key.Contains("full name"))
                    card.fullname = value;
                else if (key.Contains("titles"))
                    card.Titles = value;
                else if (key.Contains("organization"))
                    card.Organization = value;
                else if (key.Contains("phone"))
                    card.Phone = value;
                else if (key.Contains("email"))
                    card.Email = new List<string> { value };
                else if (key.Contains("address") && !key.Contains("web"))
                    card.Address = value;
                else if (key.Contains("webaddress") || key.Contains("web address") || key.Contains("web"))
                    card.WebAddress = value;
            }

            return card;
        }

        public async Task<string> SendRawToLLM(CardRequestDto requestDto, string workspaceSlug)
        {
            _logger.LogInformation("=== [LLMService.SendRawToLLM] Başladı ===");

            string? apiKey = _config["API_KEY"];
            string? baseUrl = _config["BaseUrl"];
            string requestUrl = $"{baseUrl}/{workspaceSlug}/chat";

            if (string.IsNullOrWhiteSpace(requestDto.Mode))
            {
                requestDto.Mode = "chat";
            }

            // AI modelden JSON formatında cevap almak için mesajın başına ek bilgi ekleniyor.
            requestDto.Message = "find to “full name”, “titles”,“organization” , “phone”, “email”,“Adress”, “webAddress”\n" + requestDto.Message;

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            _logger.LogInformation("API isteği gönderiliyor. URL: {Url}", requestUrl);
            _logger.LogInformation("İstek Body (RequestDto): {RequestBody}", JsonSerializer.Serialize(requestDto));

            var response = await _httpClient.PostAsJsonAsync(requestUrl, requestDto);
            _logger.LogInformation("Yanıt Kodu: {StatusCode}", response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Ham Yanıt (JSON): {ResponseJson}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("API Hatası: {StatusCode} - {ResponseJson}", response.StatusCode, responseContent);
                throw new HttpRequestException($"API Hatası: {response.StatusCode}");
            }

            using var rootJson = JsonDocument.Parse(responseContent);
            var rootElement = rootJson.RootElement;

            string? textResponse = null;
            if (rootElement.TryGetProperty("textResponse", out var textRespProp))
            {
                textResponse = textRespProp.GetString();
            }

            _logger.LogInformation("LLM'den gelen ham textResponse: {TextResponse}", textResponse);
            _logger.LogInformation("=== [LLMService.SendRawToLLM] Bitti ===");

            return textResponse ?? string.Empty;
        }
    }
}
