using System.Net.Http.Headers;
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

        /// <summary>
        /// Yeni ollama endpointi üzerinden prompt gönderir, gelen yanıtı BusinessCard nesnesine çevirir.
        /// </summary>
        public async Task<CardResponseDto> SendToLLM(CardRequestDto requestDto, string workspaceSlug)
        {
            _logger.LogInformation("=== [LLMService.SendToLLM] Başladı ===");

            // Ollama endpointi kullanılacak, URL sabit:
            string requestUrl = "http://localhost:11434/api/generate";

            // Prompt: LLM'den sadece ilgili alanları çekmesini istiyoruz.
            string prompt = "only find to 'full name', 'titles', 'organization', 'phone', 'email', 'address', 'webAddress' in the text. \n\n text: " + requestDto.Message;
            _logger.LogInformation("Ollama'ya gönderilecek prompt: {Prompt}", prompt);

            // İstek için gerekli JSON gövdesi:
            var requestBody = new
            {
                model = "llama3.2:1b",
                prompt = prompt,
                stream = false,
                max_tokens = 200
            };

            var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody);
            _logger.LogInformation("Yanıt Kodu: {StatusCode}", response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Ollama'dan Gelen Yanıt (JSON): {ResponseContent}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Ollama API Hatası: {StatusCode} - {ResponseContent}", response.StatusCode, responseContent);
                throw new HttpRequestException($"Ollama API Hatası: {response.StatusCode}");
            }

            // Gelen yanıtı JSON olarak parse ediyoruz.
            using var rootJson = JsonDocument.Parse(responseContent);
            var rootElement = rootJson.RootElement;

            string? extractedText = null;
            if (rootElement.TryGetProperty("response", out var respProp))
            {
                extractedText = respProp.GetString();
            }

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                _logger.LogWarning("Gelen extracted text boş. Boş BusinessCard dönüyoruz.");
                _logger.LogInformation("=== [LLMService.SendToLLM] Bitti ===");
                return new CardResponseDto { CardData = new BusinessCard() };
            }

            _logger.LogInformation("Ollama'dan alınan extracted text: {ExtractedText}", extractedText);

            // Gelen metni BusinessCard nesnesine dönüştürüyoruz.
            BusinessCard card = ParseExtractedTextToBusinessCard(extractedText);

            _logger.LogInformation("Ayrıştırılan BusinessCard: {CardData}", JsonSerializer.Serialize(card));
            _logger.LogInformation("=== [LLMService.SendToLLM] Bitti ===");

            return new CardResponseDto { CardData = card };
        }

        /// <summary>
        /// Ollama'dan gelen metni satır satır ayrıştırarak BusinessCard nesnesine çevirir.
        /// Beklenen format örneği:
        /// 
        /// Here is the extracted information:
        ///
        /// - Full name: Fahri ÖZSUNGUR
        /// - Titles: Social Service Professor & Doctor (Dr.)
        /// - Organization: Ministry of Economy and Industry Faculty of Economics and Administration Faculty 
        /// - Phone: +90 324 361 00
        /// - Email: fahriozsungurEgmail.com
        /// - Address: Çiftlikköy Campus, Mersin University, Çiftlikköy Kampüsü, 33343, Mersin, Türkiye
        /// </summary>
        private BusinessCard ParseExtractedTextToBusinessCard(string text)
        {
            var card = new BusinessCard();
            var lines = text.Split('\n');

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Sadece "-" ile başlayan satırları işliyoruz.
                if (!trimmedLine.StartsWith("-"))
                    continue;

                // "-" karakterini kaldırıp kalan kısmı alıyoruz.
                string content = trimmedLine.Substring(1).Trim();

                // İlk ":" karakteri anahtar-değer ayrımını yapmamızı sağlar.
                int colonIndex = content.IndexOf(':');
                if (colonIndex <= 0)
                    continue;

                string key = content.Substring(0, colonIndex).Trim().ToLower();
                string value = content.Substring(colonIndex + 1).Trim();

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

        /// <summary>
        /// Eğer ham yanıtı olduğu gibi görmek istersen, sendRaw metodunu kullanabilirsin.
        /// </summary>
        public async Task<string> SendRawToLLM(CardRequestDto requestDto, string workspaceSlug)
        {
            _logger.LogInformation("=== [LLMService.SendRawToLLM] Başladı ===");

            string requestUrl = "http://localhost:11434/api/generate";
            string prompt = "only find to 'full name', 'titles', 'organization', 'phone', 'email', 'address', 'webAddress' in the text. \n\n text: " + requestDto.Message;
            var requestBody = new
            {
                model = "llama3.2:1b",
                prompt = prompt,
                stream = false,
                max_tokens = 200
            };

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config["API_KEY"]);

            _logger.LogInformation("Ollama'ya gönderilen istek URL: {Url}", requestUrl);
            _logger.LogInformation("Ollama'ya gönderilen istek Body: {RequestBody}", System.Text.Json.JsonSerializer.Serialize(requestBody));

            var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody);
            _logger.LogInformation("Yanıt Kodu: {StatusCode}", response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Ollama'dan Gelen Yanıt (JSON): {ResponseContent}", responseContent);

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

            _logger.LogInformation("Ollama'dan gelen ham extracted text: {ExtractedText}", extractedText);
            _logger.LogInformation("=== [LLMService.SendRawToLLM] Bitti ===");

            return extractedText ?? string.Empty;
        }
    }
}
