using Microsoft.AspNetCore.Mvc;
using Supabase;
using AttendanceChecker.Models;
using ClosedXML.Excel;
using System.Globalization;
using Supabase.Postgrest.Exceptions;
using static Supabase.Postgrest.Constants;

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

        // GET /kolegiji/{id}
        [HttpGet("kolegiji/{id:int}")]
        public async Task<IActionResult> GetKolegij(int id)
        {
            var kolegij = await _supabaseClient.From<Kolegij>().Where(x => x.KolegijId == id).Single();

            if (kolegij is null)
                return NotFound(new { error = "Kolegij not found" });

            return Ok(kolegij);
        }

        [HttpGet("students")]
        public async Task<IActionResult> GetAllStudents()
        {
            try
            {
                // Fetch all students from the "studenti" table
                var studentsResponse = await _supabaseClient.From<Student>().Get();

                // Check if the request was successful
                if (!studentsResponse.ResponseMessage.IsSuccessStatusCode)
                    return StatusCode(500, new { error = "Error fetching students." });

                var students = studentsResponse.Models;

                // If no students found, return an empty list
                if (students == null || !students.Any())
                    return Ok(new List<Student>());

                return Ok(students);
            }
            catch (Exception ex)
            {
                // Log the exception and return an error
                Console.WriteLine($"Error fetching students: {ex.Message}");
                return StatusCode(500, new { error = "An error occurred while fetching students." });
            }
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

        [HttpPost("zapocni-termin/{kolegijId:int}")]
        public async Task<IActionResult> StartTermin(int kolegijId)
        {
            try
            {
                //Fetch the highest current termin_id from the "termini" table
                var latestTermin = await _supabaseClient
                    .From<Termin>()
                    .Order("TerminId", Ordering.Descending)
                    .Limit(1)
                    .Single();

                int nextTerminId = (latestTermin != null) ? latestTermin.TerminId + 1 : 85;

                //Create a new Termin with the current date and time
                var newTermin = new Termin
                {
                    TerminId = nextTerminId,
                    KolegijId = kolegijId,
                    Vrijeme = DateTime.Now
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

        [HttpGet("termini")]
        public async Task<IActionResult> GetAll()
        {
            var response = await _supabaseClient.From<Termin>().Get();
            var termini = response.Models;
            return Ok(termini);
        }

        [HttpGet("termini/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var termin = await _supabaseClient.From<Termin>().Where(x => x.TerminId == id).Single();


            if (termin == null)
                return NotFound();

            return Ok(termin);
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
                    slikaUrl = student.Slika, // Add the Slika (photo URL) field here
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
