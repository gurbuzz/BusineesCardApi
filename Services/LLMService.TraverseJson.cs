using System.Text.Json;
using System.Linq;
using BusinessCardAPI.Models;

namespace BusinessCardAPI.Services
{
    public partial class LLMService
    {
        /// <summary>
        /// JSON içeriğini gezerek BusinessCard nesnesine aktarır.
        /// </summary>
        protected void TraverseJson(JsonElement element, BusinessCard card)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var prop in element.EnumerateObject())
                    {
                        string propertyName = prop.Name.ToLower().Trim();

                        if (propertyName.Contains("title"))
                        {
                            card.Title = prop.Value.GetString();
                        }
                        else if (propertyName.Contains("phone") || propertyName.Contains("telefon"))
                        {
                            if (prop.Value.ValueKind == JsonValueKind.String)
                            {
                                card.Phone = prop.Value.GetString();
                            }
                            else if (prop.Value.ValueKind == JsonValueKind.Array)
                            {
                                var phones = prop.Value.EnumerateArray()
                                    .Select(e => e.GetString())
                                    .Where(e => !string.IsNullOrWhiteSpace(e))
                                    .ToList();
                                if (phones.Count > 0)
                                    card.Phone = phones[0];
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
                            if (prop.Value.ValueKind == JsonValueKind.String)
                            {
                                card.Address = prop.Value.GetString();
                            }
                            else if (prop.Value.ValueKind == JsonValueKind.Object)
                            {
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
                                if (prop.Value.TryGetProperty("phone_number", out var pNumVal))
                                {
                                    card.Fax = pNumVal.GetString();
                                }
                            }
                        }
                        else
                        {
                            // Diğer alanları gezmek için
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
                    // Diğer veri tiplerini görmezden gel
                    break;
            }
        }
    }
}
