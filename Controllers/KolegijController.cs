using AttendanceChecker.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Supabase;

namespace AttendanceChecker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KolegijController : ControllerBase
    {
        private readonly Client _supabaseClient;

        public KolegijController(Client supabaseClient)
        {
            _supabaseClient = supabaseClient;
        }

        // GET /
        [HttpGet("")]
        public async Task<IActionResult> GetKolegiji()
        {
            var kolegijiResponse = await _supabaseClient.From<Kolegij>().Get();

            if (!kolegijiResponse.ResponseMessage.IsSuccessStatusCode)
                return Problem("Error fetching kolegiji: " + kolegijiResponse.ResponseMessage);

            return Ok(kolegijiResponse.Models);
        }

        // GET /{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetKolegij(int id)
        {
            var kolegij = await _supabaseClient.From<Kolegij>().Where(x => x.KolegijId == id).Single();

            if (kolegij is null)
                return NotFound(new { error = "Kolegij not found" });

            return Ok(kolegij);
        }
    }
}
