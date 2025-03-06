using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BusinessCardAPI.Models;
using Microsoft.Extensions.Logging;

namespace BusinessCardAPI.Services
{
    public static class BusinessCardParser
    {
        public static async Task<BusinessCard> ParseAsync(string extractedText, ILogger logger)
        {
            // 1. Dış JSON’dan "rawText" alanını almaya çalışıyoruz.
            try
            {
                using var outerDoc = JsonDocument.Parse(extractedText);
                if (outerDoc.RootElement.TryGetProperty("rawText", out var rawTextElement))
                {
                    extractedText = rawTextElement.GetString() ?? "";
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Dış JSON ayrıştırma hatası: {Message}", ex.Message);
            }

            // 2. İç JSON’dan "response" alanını almaya çalışıyoruz.
            try
            {
                using var innerDoc = JsonDocument.Parse(extractedText);
                if (innerDoc.RootElement.TryGetProperty("response", out var responseElement))
                {
                    extractedText = responseElement.GetString() ?? "";
                }
                else
                {
                    // Eğer "response" alanı yoksa, tüm inner JSON'ı kullanıyoruz.
                    extractedText = innerDoc.RootElement.GetRawText();
                }
            }
            catch (Exception ex)
            {
                logger.LogError("İç JSON ayrıştırma hatası: {Message}", ex.Message);
            }

            var card = new BusinessCard();

            // Eğer içerik JSON formatında değilse (düz metin ise)
            if (IsPlainTextFormat(extractedText))
            {
                // ParsePlainText metodu CPU-bound olduğundan Task.Run ile asenkron hale getiriyoruz.
                var fields = await Task.Run(() => ParsePlainText(extractedText));
                card.Name = GetValue(fields, "name");
                card.Surname = GetValue(fields, "surname");
                card.Titles = GetValue(fields, "titles");
                card.Organization = GetValue(fields, "organization");
                card.Phone = GetValue(fields, "phone");
                var email = GetValue(fields, "email");
                if (!string.IsNullOrWhiteSpace(email))
                {
                    card.Email = new List<string> { email };
                }
                card.Address = GetValue(fields, "address");
                card.WebAddress = GetValue(fields, "webAddress");
            }
            else
            {
                // İçerik JSON formatındaysa doğrudan JSON ayrıştırması yapıyoruz.
                try
                {
                    using var cardDoc = JsonDocument.Parse(extractedText);
                    var root = cardDoc.RootElement;

                    if (root.TryGetProperty("name", out var nameElement))
                        card.Name = nameElement.GetString();

                    if (root.TryGetProperty("surname", out var surnameElement))
                        card.Surname = surnameElement.GetString();

                    if (root.TryGetProperty("titles", out var titlesElement))
                        card.Titles = titlesElement.GetString();

                    if (root.TryGetProperty("organization", out var orgElement))
                        card.Organization = orgElement.GetString();

                    if (root.TryGetProperty("phone", out var phoneElement))
                        card.Phone = phoneElement.GetString();

                    if (root.TryGetProperty("email", out var emailElement))
                    {
                        var emailStr = emailElement.GetString();
                        if (!string.IsNullOrEmpty(emailStr))
                            card.Email = new List<string> { emailStr };
                    }

                    if (root.TryGetProperty("address", out var addressElement))
                        card.Address = addressElement.GetString();

                    if (root.TryGetProperty("webAddress", out var webElement))
                        card.WebAddress = webElement.GetString();
                }
                catch (Exception ex)
                {
                    logger.LogError("BusinessCard JSON ayrıştırma hatası: {Message}", ex.Message);
                }
            }

            // Soyadı çıkarma yöntemi: Eğer Name alanı soyadı içeriyorsa, soyadı Name'den çıkartıyoruz.
            if (!string.IsNullOrEmpty(card.Name) && !string.IsNullOrEmpty(card.Surname) &&
                card.Name.EndsWith(card.Surname, StringComparison.OrdinalIgnoreCase))
            {
                card.Name = card.Name.Substring(0, card.Name.LastIndexOf(card.Surname, StringComparison.OrdinalIgnoreCase)).Trim();
            }

            return card;
        }


        /// Metin JSON formatında değilse düz metin olarak kabul ediyoruz.
        private static bool IsPlainTextFormat(string text)
        {
            text = text.Trim();
            return !(text.StartsWith("{") && text.EndsWith("}"));
        }

        /// Düz metin içeriğini satır satır ayrıştırır ve alanları bir sözlükte toplar.
        /// Hem "- field: value" hem de "1. field - value" gibi numaralı formatları destekler.
        private static Dictionary<string, string> ParsePlainText(string text)
        {
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var pattern = @"^(?:-|\d+\.)\s*(.+?)\s*[:-]\s*(.*)$";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.StartsWith("Here are the extracted", StringComparison.OrdinalIgnoreCase))
                    continue;

                var match = regex.Match(line);
                if (match.Success)
                {
                    var fieldName = match.Groups[1].Value.Trim();
                    var fieldValue = match.Groups[2].Value.Trim();
                    fields[fieldName] = fieldValue;
                }
            }

            if (fields.TryGetValue("email", out var emailValue) && emailValue.Contains("assuming"))
            {
                emailValue = emailValue.Replace("@egmail.com", "@gmail.com");
                fields["email"] = emailValue;
            }

            return fields;
        }

        /// Sözlükten belirli bir alanın değerini döndürür; bulunamazsa boş string verir.
        private static string GetValue(Dictionary<string, string> dict, string key)
        {
            return dict.TryGetValue(key, out var value) ? value : string.Empty;
        }
    }
}
