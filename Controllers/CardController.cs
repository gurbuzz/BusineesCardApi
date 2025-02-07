using Microsoft.AspNetCore.Mvc;
using BusinessCardAPI.Services;
using BusinessCardAPI.DTOs;
using System;
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
            if (string.IsNullOrWhiteSpace(requestDto.Message))
            {
                return BadRequest(new { error = "Mesaj boş olamaz." });
            }

            // Yeni ollama endpointi kullanıldığı için workspaceSlug artık kullanılmıyor.
            try
            {
                var result = await _llmService.SendToLLM(requestDto, string.Empty);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
