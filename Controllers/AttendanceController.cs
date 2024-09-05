using Microsoft.AspNetCore.Mvc;
using Supabase;
using AttendanceChecker.Models;
using ClosedXML.Excel;
using System.Globalization;

namespace AttendanceChecker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttendanceController : ControllerBase
    {
        private readonly Client _supabaseClient;
        private static string _lastUID;

        public AttendanceController(Client supabaseClient)
        {
            _supabaseClient = supabaseClient;
        }

        // POST /rfid-scan
        [HttpPost("rfid-scan")]
        public async Task<IActionResult> ScanRfid([FromBody] RfidRequest request)
        {
            var uid = request?.Uid;
            _lastUID = uid;

            if (!string.IsNullOrEmpty(uid))
            {
                var student = await _supabaseClient
                    .From<Student>()
                    .Where(x => x.BrKartice == uid)
                    .Single();

                if (student is null)
                    return NotFound(new { error = "Student not found" });

                var now = DateTime.UtcNow;

                var terminResponse = await _supabaseClient
                    .From<Termin>()
                    .Where(x => x.Vrijeme >= now.AddMinutes(-15) && x.Vrijeme <= now)
                    .Get();

                if (terminResponse.Models.Count > 0)
                {
                    foreach (var termin in terminResponse.Models)
                    {
                        await _supabaseClient
                            .From<Nazocnost>()
                            .Insert(new Nazocnost
                            {
                                NazocnostId = Guid.NewGuid().ToString(),
                                TerminId = termin.TerminId,
                                StudentId = student.StudentId
                            });
                    }
                    return Ok(new { message = "Record inserted successfully" });
                }
                else
                {
                    return NotFound(new { error = "No termin found within the last 15 minutes" });
                }
            }

            return BadRequest(new { error = "UID is missing" });
        }

        // GET /attendance-percentage/{kolegijId}
        [HttpGet("attendance-percentage/{kolegijId:int}")]
        public async Task<IActionResult> GetAttendancePercentage(int kolegijId)
        {
            var attendanceData = await FetchAttendanceAsync(kolegijId);
            return Ok(attendanceData);
        }

        // GET /export-attendance/{kolegijId}
        [HttpGet("export-attendance/{kolegijId:int}")]
        public async Task<IActionResult> ExportAttendance(int kolegijId)
        {
            var attendanceData = await FetchAttendanceAsync(kolegijId);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Attendance");

            worksheet.Cell(1, 1).Value = "Ime";
            worksheet.Cell(1, 2).Value = "Prezime";
            worksheet.Cell(1, 3).Value = "OIB";
            worksheet.Cell(1, 4).Value = "Postotak Prisustva";

            int row = 2;
            foreach (var student in attendanceData)
            {
                worksheet.Cell(row, 1).Value = student.ime;
                worksheet.Cell(row, 2).Value = student.prezime;
                worksheet.Cell(row, 3).Value = string.IsNullOrEmpty(student.oib) ? "N/A" : student.oib.Trim();
                worksheet.Cell(row, 4).Value = $"{student.percentage}%";
                row++;
            }

            using var memoryStream = new MemoryStream();
            workbook.SaveAs(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "attendance.xlsx");
        }

        // GET /last-uid
        [HttpGet("last-uid")]
        public IActionResult GetLastUID()
        {
            return string.IsNullOrEmpty(_lastUID) ? Ok("No UID received yet.") : Ok($"Last UID received: {_lastUID}");
        }

        // GET /kolegiji
        [HttpGet("kolegiji")]
        public async Task<IActionResult> GetKolegiji()
        {
            var kolegijiResponse = await _supabaseClient.From<Kolegij>().Get();

            if (!kolegijiResponse.ResponseMessage.IsSuccessStatusCode)
                return Problem("Error fetching kolegiji: " + kolegijiResponse.ResponseMessage);

            return Ok(kolegijiResponse.Models);
        }

        // GET /student/{id}
        [HttpGet("student/{id:int}")]
        public async Task<IActionResult> GetStudent(int id)
        {
            var student = await _supabaseClient.From<Student>().Where(x => x.StudentId == id).Single();

            if (student is null)
                return NotFound(new { error = "Student not found" });

            return Ok(student);
        }

        #region Helper methods

        private async Task<IEnumerable<dynamic>> FetchAttendanceAsync(int kolegijId)
        {
            var totalCount = await GetTotalTerminiForKolegijAsync(kolegijId);
            var students = await GetAllStudentsAsync();
            var nazocnosti = await GetAttendanceRecordsAsync();
            var termini = await GetTerminiForKolegijAsync(kolegijId);

            var result = students.Select(student =>
            {
                var attendedCount = nazocnosti
                    .Where(n => n.StudentId == student.StudentId && termini.Any(t => t.TerminId == n.TerminId))
                    .Count();

                var percentage = totalCount > 0 ? (attendedCount / (double)totalCount) * 100 : 0;

                return new
                {
                    student_id = student.StudentId,
                    ime = student.Ime,
                    prezime = student.Prezime,
                    oib = student.Oib,
                    attended_count = attendedCount,
                    total_count = totalCount,
                    percentage = percentage.ToString("F2", CultureInfo.InvariantCulture)
                };
            });

            return result;
        }

        private async Task<int> GetTotalTerminiForKolegijAsync(int kolegijId)
        {
            var response = await _supabaseClient
                .From<Termin>()
                .Where(t => t.KolegijId == kolegijId)
                .Get();

            if (!response.ResponseMessage.IsSuccessStatusCode)
                throw new Exception("Error querying total termin count.");

            return response.Models.Count;
        }

        private async Task<List<Student>> GetAllStudentsAsync()
        {
            var response = await _supabaseClient.From<Student>().Get();
            if (!response.ResponseMessage.IsSuccessStatusCode)
                throw new Exception("Error querying students.");
            return response.Models;
        }

        private async Task<List<Nazocnost>> GetAttendanceRecordsAsync()
        {
            var response = await _supabaseClient.From<Nazocnost>().Get();
            if (!response.ResponseMessage.IsSuccessStatusCode)
                throw new Exception("Error querying attendance records.");
            return response.Models;
        }

        private async Task<List<Termin>> GetTerminiForKolegijAsync(int kolegijId)
        {
            var response = await _supabaseClient
                .From<Termin>()
                .Where(t => t.KolegijId == kolegijId)
                .Get();
            if (!response.ResponseMessage.IsSuccessStatusCode)
                throw new Exception("Error querying termini for the given kolegij.");
            return response.Models;
        }

        #endregion
    }
}
