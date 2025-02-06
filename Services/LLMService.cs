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

            // LLM'e daima JSON formatında cevap istiyoruz
            requestDto.Message = "always respond in json format " + requestDto.Message;

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

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

            // JSON verisini temizle ve düzelt
            string sanitizedJson = SanitizeLeadingZerosInNumbers(textResponse);
            sanitizedJson = TryFixJsonStructure(sanitizedJson);
            _logger.LogInformation("Düzeltilmiş JSON: {Sanitized}", sanitizedJson);

            var card = new BusinessCard();
            try
            {
                using var innerDoc = JsonDocument.Parse(sanitizedJson);
                TraverseJson(innerDoc.RootElement, card);
            }
            catch (JsonException ex)
            {
                _logger.LogError("Düzeltilmiş JSON parse edilemedi: {Ex}", ex.Message);
                card.AdditionalInfo = "Parse Hatası: " + sanitizedJson;
            }

            var resultDto = new CardResponseDto { CardData = card };
            _logger.LogInformation("Çıkan Son CardData: {Result}", JsonSerializer.Serialize(card));
            _logger.LogInformation("=== [LLMService.SendToLLM] Bitti ===");

            return resultDto;
        }
    }
}
