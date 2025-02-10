using System.Text.Json;
using BusinessCardAPI.DTOs;
using BusinessCardAPI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessCardAPI.Services
{
    public partial class LLMService
    {
        private readonly OllamaClient _ollamaClient;
        private readonly IConfiguration _config;
        private readonly ILogger<LLMService> _logger;

        public LLMService(OllamaClient ollamaClient, IConfiguration config, ILogger<LLMService> logger)
        {
            _ollamaClient = ollamaClient;
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// API Key'i appsettings.json'dan çeker
        /// </summary>
        public string GetConfiguredApiKey()
        {
            return _config["API_KEY"] ?? string.Empty;
        }

        /// <summary>
        /// Ollama'ya istek atarak işlenmiş kart verisini döndürür.
        /// </summary>
        public async Task<CardResponseDto> SendToLLM(CardRequestDto requestDto, string workspaceSlug)
        {
            _logger.LogInformation("=== [LLMService.SendToLLM] Başladı ===");

            string prompt = "only find to 'full name', 'titles', 'organization', 'phone', 'email', 'address', 'webAddress' in the text. \n\n text: " + requestDto.Message;
            _logger.LogInformation("Ollama prompt: {Prompt}", prompt);

            string extractedText = await _ollamaClient.SendRequestAsync(prompt);
            _logger.LogInformation("Ollama'dan alınan extracted text: {ExtractedText}", extractedText);

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                _logger.LogWarning("Extracted text boş. Boş BusinessCard döndürülüyor.");
                return new CardResponseDto { CardData = new BusinessCard() };
            }

            BusinessCard card = BusinessCardParser.Parse(extractedText, _logger);
            _logger.LogInformation("Ayrıştırılan BusinessCard: {CardData}", JsonSerializer.Serialize(card));
            _logger.LogInformation("=== [LLMService.SendToLLM] Bitti ===");

            return new CardResponseDto { CardData = card };
        }

        /// <summary>
        /// Ollama'dan ham yanıtı döndürür.
        /// </summary>
        public async Task<string> SendRawToLLM(CardRequestDto requestDto, string workspaceSlug)
        {
            _logger.LogInformation("=== [LLMService.SendRawToLLM] Başladı ===");

            string prompt = "only find to 'full name', 'titles', 'organization', 'phone', 'email', 'address', 'webAddress' in the text. \n\n text: " + requestDto.Message;
            _logger.LogInformation("Ollama prompt: {Prompt}", prompt);

            string extractedText = await _ollamaClient.SendRawRequestAsync(prompt);
            _logger.LogInformation("Ham extracted text: {ExtractedText}", extractedText);
            _logger.LogInformation("=== [LLMService.SendRawToLLM] Bitti ===");

            return extractedText;
        }
    }
}
