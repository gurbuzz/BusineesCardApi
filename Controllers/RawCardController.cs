using Microsoft.AspNetCore.Mvc;
using BusinessCardAPI.DTOs;
using BusinessCardAPI.Services;
using System.Threading.Tasks;

namespace BusinessCardAPI.Controllers
{
    [Route("api/v1/cards")]
    [ApiController]
    public class RawCardController : ControllerBase
    {
        private readonly LLMService _llmService;

        public RawCardController(LLMService llmService)
        {
            _llmService = llmService;
        }

        [HttpPost("raw")]
        public async Task<IActionResult> GetRawResponse([FromBody] CardRequestDto requestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(requestDto.Message))
            {
                return BadRequest(new { error = "Mesaj boş olamaz." });
            }

            // API Key doğrulama
            var configuredApiKey = _llmService.GetConfiguredApiKey();
            if (string.IsNullOrWhiteSpace(requestDto.ApiKey) || requestDto.ApiKey != configuredApiKey)
            {
                return Unauthorized(new { error = "Geçersiz API anahtarı." });
            }

            try
            {
                var rawText = await _llmService.SendRawToLLM(requestDto, string.Empty);
                return Ok(new RawResponseDto { RawText = rawText });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
