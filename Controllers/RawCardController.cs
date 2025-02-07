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
            if (string.IsNullOrWhiteSpace(requestDto.Message))
            {
                return BadRequest(new { error = "Mesaj boş olamaz." });
            }

            // Yeni ollama endpointi kullanıldığı için workspaceSlug artık kullanılmıyor.
            try
            {
                var rawText = await _llmService.SendRawToLLM(requestDto, string.Empty);
                return Ok(new RawResponseDto { RawText = rawText });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
