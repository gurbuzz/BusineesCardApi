using System;
using System.Collections.Generic;
using System.Text.Json;
using BusinessCardAPI.Models;
using Microsoft.Extensions.Logging;

namespace BusinessCardAPI.Services
{
    public static class BusinessCardParser
    {
        public static BusinessCard Parse(string extractedText, ILogger logger)
        {
            // Eğer metin JSON formatında ise, "response" alanını çekiyoruz.
            if (extractedText.Trim().StartsWith("{"))
            {
                try
                {
                    using var jsonDoc = JsonDocument.Parse(extractedText);
                    if (jsonDoc.RootElement.TryGetProperty("response", out var responseElement))
                    {
                        extractedText = responseElement.GetString();
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError("JSON ayrıştırma hatası: {Message}", ex.Message);
                }
            }

            var card = new BusinessCard();
            var lines = extractedText.Split('\n');

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                // Sadece "-" ile başlayan satırları işliyoruz.
                if (!trimmedLine.StartsWith("-"))
                    continue;

                // "-" karakterini kaldırıp kalan kısmı alıyoruz.
                string content = trimmedLine.Substring(1).Trim();

                // İlk ":" karakteri ile anahtar-değer ayrımını yapıyoruz.
                int colonIndex = content.IndexOf(':');
                if (colonIndex <= 0)
                    continue;

                string key = content.Substring(0, colonIndex).Trim().ToLower();
                string value = content.Substring(colonIndex + 1).Trim();

                logger.LogInformation("Ayrıştırılan Alan -> Key: {Key}, Value: {Value}", key, value);

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
    }
}
