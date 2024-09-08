using AttendanceChecker.Models;
using AttendanceChecker.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Supabase;

namespace AttendanceChecker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RfidController : ControllerBase
    {
        private readonly Client _supabaseClient;

        public RfidController(Client supabaseClient)
        {
            _supabaseClient = supabaseClient;
        }

        // POST /rfid-scan
        [HttpPost("scan")]
        public async Task<IActionResult> ScanRfid([FromBody] RfidRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Uid) || request.DvoranaId <= 0)
                return BadRequest("Request is not valid");

            var student = await _supabaseClient
                .From<Student>()
                .Where(x => x.BrKartice == request.Uid)
                .Single();

            if (student is null)
                return NotFound(new { error = "Student not found" });

            var now = DateTime.UtcNow;
            var startTime = now.AddMinutes(-15);
            var endTime = now;

            var termin = await _supabaseClient
                .From<Termin>()
                .Where(x => x.StartTime >= startTime && x.StartTime <= endTime)
                .Where(x => x.EndTime >= now)
                .Where(x => x.DvoranaId == request.DvoranaId)
                .Single();


            if (termin is null)
                return NotFound(new { error = "No termin found within the last 15 minutes" });

            await _supabaseClient
                .From<Nazocnost>()
                .Insert(new Nazocnost
                {
                    TerminId = termin.TerminId,
                    StudentId = student.StudentId,
                    DateScanned = DateTime.Now,
                });

            return Ok(new { message = "Record inserted successfully" });
        }
    }
}
