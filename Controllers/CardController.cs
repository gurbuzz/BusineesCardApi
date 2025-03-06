using Microsoft.AspNetCore.Mvc;
using BusinessCardAPI.Services;
using BusinessCardAPI.DTOs;
using System.Threading.Tasks;

namespace BusinessCardAPI.Controllers
{
    [Route("api/v1/cards")]
    [ApiController]
    public class CardController : ControllerBase
    {
        private readonly LLMService _llmService;

        public CardController(LLMService llmService)
        {
            _llmService = llmService;
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessCard([FromBody] CardRequestDto requestDto)
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
                var result = await _llmService.SendToLLM(requestDto, string.Empty);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
