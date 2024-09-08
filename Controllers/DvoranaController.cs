using AttendanceChecker.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Supabase;

namespace AttendanceChecker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DvoranaController : ControllerBase
    {
        private readonly Client _supabaseClient;

        public DvoranaController(Client supabaseClient)
        {
            _supabaseClient = supabaseClient;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetDvorane()
        {
            var response = await _supabaseClient.From<Dvorana>().Get();
            var dvorane = response.Models;
            return Ok(dvorane);
        }
    }
}