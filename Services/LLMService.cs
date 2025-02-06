using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using BusinessCardAPI.DTOs;
using BusinessCardAPI.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Linq;

namespace BusinessCardAPI.Services
{
    public class LLMService
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
            // Log başlasın
            _logger.LogInformation("=== [LLMService.SendToLLM] Başladı ===");

            string? apiKey = _config["API_KEY"];
            string? baseUrl = _config["BaseUrl"];
            string requestUrl = $"{baseUrl}/{workspaceSlug}/chat";

            if (string.IsNullOrWhiteSpace(requestDto.Mode))
            {
                requestDto.Mode = "chat";
            }

            // always respond in json format ekliyoruz
            requestDto.Message = "always respond in json format " + requestDto.Message;

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            // Log - İstek detayları
            _logger.LogInformation("API isteği gönderiliyor. URL: {Url}", requestUrl);
            _logger.LogInformation("İstek Body (RequestDto): {RequestBody}", System.Text.Json.JsonSerializer.Serialize(requestDto));

            // ANYTHINGLLM'ye POST
            var response = await _httpClient.PostAsJsonAsync(requestUrl, requestDto);
            _logger.LogInformation("Yanıt Kodu: {StatusCode}", response.StatusCode);

            // Gelen yanıtın ham içeriği
            string responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Ham Yanıt (JSON): {ResponseJson}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("API Hatası: {StatusCode} - {ResponseJson}", response.StatusCode, responseContent);
                throw new HttpRequestException($"API Hatası: {response.StatusCode}");
            }

            // JSON parse
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

            // DETAYLI LOG -> textResponse ham hali
            _logger.LogInformation("textResponse Ham İçerik: {Text}", textResponse);

            // 1) phone_number vb. numeric alanlarda sıfırla başlayan sayıları stringe dönüştürelim
            string sanitizedJson = SanitizeLeadingZerosInNumbers(textResponse);
            // 2) JSON yapısını düzeltmeye çalışalım
            sanitizedJson = TryFixJsonStructure(sanitizedJson);
            _logger.LogInformation("textResponse Sanitized ve Düzeltilmiş İçerik: {Sanitized}", sanitizedJson);

            var card = new BusinessCard();
            try
            {
                using var innerDoc = JsonDocument.Parse(sanitizedJson);
                // JSON düzgün parse edildi, TraverseJson ile card'a aktaracağız
                TraverseJson(innerDoc.RootElement, card);
            }
            catch (JsonException ex)
            {
                _logger.LogError("Düzeltilmiş JSON parse edilemedi: {Ex}", ex.Message);
                // parse edilemezse mecburen AdditionalInfo'ya ham veriyi koyuyoruz
                card.AdditionalInfo = "Parse Hatası: " + sanitizedJson;
            }

            var resultDto = new CardResponseDto { CardData = card };
            _logger.LogInformation("Çıkan Son CardData: {Result}", System.Text.Json.JsonSerializer.Serialize(card));
            _logger.LogInformation("=== [LLMService.SendToLLM] Bitti ===");
            return resultDto;
        }

        /// <summary>
        /// Bu metot, '":0xxx' gibi numaraları tespit edip onları stringe çeviriyor.
        /// Örnek: phone_number:0324 -> phone_number:"0324"
        /// </summary>
        private string SanitizeLeadingZerosInNumbers(string inputJson)
        {
            // 1) phone_number, faxes, vb. gibi alanlarda :0 ile başlayan sayıları string'e çevir
            // Örnek regex ->  \"phone_number\":(\\d+)
            // Bu, "phone_number":12345 gibi kısımları bulur.
            // Ardından eğer 0 ile başlıyorsa -> "phone_number":"0324"
            var pattern = new Regex(@"(""[A-Za-z_]*phone_number""\s*:\s*)(\d+)");
            // Gruplar:
            // 1. => "phone_number":
            // 2. => sayi (ör. 032436100002)

            string sanitized = pattern.Replace(inputJson, match =>
            {
                string keyPart = match.Groups[1].Value; // "phone_number":
                string numberPart = match.Groups[2].Value; // 032436100002
                // Tırnak içinde dön
                return $"{keyPart}\"{numberPart}\"";
            });

            return sanitized;
        }

        /// <summary>
        /// JSON yapısını düzeltmeye çalışır. Eksik kapanan parantezleri tamamlar.
        /// </summary>
        private string TryFixJsonStructure(string inputJson)
        {
            try
            {
                // JSON'un geçerli olup olmadığını kontrol et
                JsonDocument.Parse(inputJson);
                return inputJson;
            }
            catch (JsonException)
            {
                // Eksik kapanan parantezleri say
                int openBraces = inputJson.Count(c => c == '{');
                int closeBraces = inputJson.Count(c => c == '}');
                int missingBraces = openBraces - closeBraces;

                if (missingBraces > 0)
                {
                    // Eksik kapanan parantezleri ekle
                    inputJson += new string('}', missingBraces);
                }

                return inputJson;
            }
        }

        /// <summary>
        /// JSON içerisindeki alanları tarayıp BusinessCard modeline dolduruyoruz.
        /// </summary>
        private void TraverseJson(JsonElement element, BusinessCard card)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var prop in element.EnumerateObject())
                    {
                        string propertyName = prop.Name.ToLower().Trim();

                        // Örnek
                        if (propertyName.Contains("title"))
                        {
                            card.Title = prop.Value.GetString();
                        }
                        else if (propertyName.Contains("phone") || propertyName.Contains("telefon"))
                        {
                            // Tek string
                            if (prop.Value.ValueKind == JsonValueKind.String)
                            {
                                card.Phone = prop.Value.GetString();
                            }
                            // Array vs. handle
                            else if (prop.Value.ValueKind == JsonValueKind.Array)
                            {
                                // phone array ise, ilkini al
                                var phones = prop.Value.EnumerateArray().Select(e => e.GetString()).Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
                                if (phones.Count > 0) card.Phone = phones[0];
                            }
                        }
                        else if (propertyName.Contains("email") && prop.Value.ValueKind == JsonValueKind.Array)
                        {
                            card.Email = prop.Value.EnumerateArray()
                                .Select(e => e.GetString() ?? string.Empty)
                                .Where(e => !string.IsNullOrEmpty(e))
                                .ToList();
                        }
                        else if (propertyName.Contains("adres") || propertyName.Contains("address"))
                        {
                            // object olabilir {city, country,...} vs.
                            if (prop.Value.ValueKind == JsonValueKind.String)
                            {
                                card.Address = prop.Value.GetString();
                            }
                            else if (prop.Value.ValueKind == JsonValueKind.Object)
                            {
                                // city, country vs. topla
                                string combined = "";
                                foreach (var subProp in prop.Value.EnumerateObject())
                                {
                                    combined += $"{subProp.Name}:{subProp.Value}, ";
                                }
                                card.Address = combined.TrimEnd(' ', ',');
                            }
                        }
                        else if (propertyName.Contains("fax"))
                        {
                            if (prop.Value.ValueKind == JsonValueKind.String)
                            {
                                card.Fax = prop.Value.GetString();
                            }
                            else if (prop.Value.ValueKind == JsonValueKind.Object)
                            {
                                // phone_number vb.
                                if (prop.Value.TryGetProperty("phone_number", out var pNumVal))
                                {
                                    card.Fax = pNumVal.GetString();
                                }
                            }
                        }
                        else
                        {
                            // alt node'ları taramak için:
                            TraverseJson(prop.Value, card);
                        }
                    }
                    break;

                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        TraverseJson(item, card);
                    }
                    break;

                default:
                    // Diğer tipleri atlıyoruz
                    break;
            }
        }
    }
}