using System.Text.Json;
using System.Text.RegularExpressions;
using System.Linq;

namespace BusinessCardAPI.Services
{
    public partial class LLMService
    {
        /// <summary>
        /// "phone_number":0324 gibi değerleri "phone_number":"0324" olarak dönüştürür.
        /// </summary>
        protected string SanitizeLeadingZerosInNumbers(string inputJson)
        {
            var pattern = new Regex(@"(""[A-Za-z_]*phone_number""\s*:\s*)(\d+)");
            string sanitized = pattern.Replace(inputJson, match =>
            {
                string keyPart = match.Groups[1].Value;
                string numberPart = match.Groups[2].Value;
                return $"{keyPart}\"{numberPart}\"";
            });

            return sanitized;
        }

        /// <summary>
        /// JSON içerisindeki yapısal hataları (eksik parantez vb.) düzeltir.
        /// </summary>
        protected string TryFixJsonStructure(string inputJson)
        {
            try
            {
                JsonDocument.Parse(inputJson);
                return inputJson;
            }
            catch (JsonException)
            {
                int openBraces = inputJson.Count(c => c == '{');
                int closeBraces = inputJson.Count(c => c == '}');
                int missingBraces = openBraces - closeBraces;

                if (missingBraces > 0)
                {
                    inputJson += new string('}', missingBraces);
                }

                return inputJson;
            }
        }
    }
}
