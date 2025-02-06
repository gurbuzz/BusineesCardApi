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

            string workspaceSlug = "llama";
            try
            {
                var rawText = await _llmService.SendRawToLLM(requestDto, workspaceSlug);
                return Ok(new RawResponseDto { RawText = rawText });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
