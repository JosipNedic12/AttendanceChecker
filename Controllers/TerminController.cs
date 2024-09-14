using AttendanceChecker.Models;
using AttendanceChecker.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using Supabase.Postgrest.Exceptions;

namespace AttendanceChecker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TerminController : ControllerBase
    {
        private readonly Client _supabaseClient;

        public TerminController(Client supabaseClient)
        {
            _supabaseClient = supabaseClient;
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartTermin(StartTerminRequest request)
        {
            try
            {
                //Create a new Termin with the current date and time
                var newTermin = new Termin
                {
                    KolegijId = request.KolegijId,
                    StartTime = DateTime.Now,
                    EndTime = DateTime.Now.AddHours(1.5),
                    DvoranaId = request.DvoranaId
                };

                // Insert the new Termin into Supabase
                var response = await _supabaseClient.From<Termin>().Insert(newTermin);

                // Ensure response content is fully read before disposing the response object
                if (response.ResponseMessage.IsSuccessStatusCode)
                {
                    return Ok(new { message = "Termin started successfully" });
                }
                else
                {
                    var responseContent = await response.ResponseMessage.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error starting termin: {response.ResponseMessage.StatusCode}");
                    Console.WriteLine($"Response Content: {responseContent}");
                    return Problem("Error starting termin");
                }
            }

            catch (PostgrestException ex)
            {
                // Read the content from the response early to avoid disposed exceptions
                if (ex.Response != null && !ex.Response.Content.Headers.ContentLength.HasValue)
                {
                    var errorContent = await ex.Response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Supabase Error Details: {errorContent}");
                }

                return StatusCode(500, new { error = ex.Message });
            }
            catch (ObjectDisposedException ex)
            {
                // Handle ObjectDisposedException explicitly
                Console.WriteLine($"Object disposed unexpectedly: {ex.Message}");
                return StatusCode(500, new { error = "Object disposed unexpectedly: " + ex.Message });
            }
            catch (Exception ex)
            {
                // General error logging
                Console.WriteLine($"Unexpected error: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("{terminId:int}/end")]
        public async Task<IActionResult> EndTermin(int terminId)
        {
            var termin = await _supabaseClient
                .From<Termin>()
                .Where(x => x.TerminId == terminId)
                .Single();

            if (termin == null)
                return NotFound();

            termin.EndTime = DateTime.Now;

            await termin.Update<Termin>();

            return Ok(termin);
        }

        [HttpGet("")]
        public async Task<IActionResult> GetAllTermini()
        {
            var response = await _supabaseClient.From<Termin>().Get();

            if (response.ResponseMessage.IsSuccessStatusCode)
            {
                var termini = response.Models;
                return Ok(termini);
            }
            else
                return Problem("Error starting termin");
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTerminById(int id)
        {
            var termin = await _supabaseClient.From<Termin>().Where(x => x.TerminId == id).Single();

            if (termin == null)
                return NotFound();

            return Ok(termin);
        }

        [HttpGet("kolegij/{kolegijId}")]
        public async Task<IActionResult> GetTerminByKolegijId(int kolegijId)
        {
			var response = await _supabaseClient
	            .From<Termin>()
	            .Where(x => x.KolegijId == kolegijId)
	            .Order(x => x.StartTime, Supabase.Postgrest.Constants.Ordering.Descending)
	            .Get();

			if (response.ResponseMessage.IsSuccessStatusCode)
            {
                List<Termin> termini = response.Models;
                return Ok(termini);
            }
            else
                return Problem("Error starting termin");
        }
    }
}