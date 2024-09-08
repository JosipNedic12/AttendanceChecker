using AttendanceChecker.Models.Entities;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using System.Globalization;

namespace AttendanceChecker.Controllers
{
    [ApiController]
	[Route("api/[controller]")]
	public class AttendanceController : ControllerBase
	{
		private readonly Client _supabaseClient;

		public AttendanceController(Client supabaseClient)
		{
			_supabaseClient = supabaseClient;
		}

        // GET /kolegij/{kolegijId}
        [HttpGet("kolegij/{kolegijId:int}")]
		public async Task<IActionResult> GetAttendanceByKolegijId(int kolegijId)
		{
			var attendanceData = await FetchAttendanceAsyncByKolegijId(kolegijId);
			return Ok(attendanceData);
		}

		// GET /kolegij/{kolegijId}/export
		[HttpGet("kolegij/{kolegijId:int}/export")]
		public async Task<IActionResult> ExportAttendanceByKolegijId(int kolegijId)
		{
			var attendanceData = await FetchAttendanceAsyncByKolegijId(kolegijId);
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
			return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Prisutnost_po_kolegiju.xlsx");
		}

        // GET /termin/terminId}
        [HttpGet("termin/{terminId:int}")]
        public async Task<IActionResult> GetAttendanceByTerminId(int terminId)
        {
			var response = await _supabaseClient
				.From<Nazocnost>()
				.Where(x => x.TerminId == terminId)
				.Get();

			var nazocnosti = response.Models;

            return Ok(nazocnosti);
        }

        // GET /termin/terminId}
        [HttpGet("termin/{terminId:int}/export")]
        public async Task<IActionResult> ExportAttendanceByTerminId(int terminId)
        {
            var response = await _supabaseClient
                .From<Nazocnost>()
                .Where(x => x.TerminId == terminId)
                .Get();

            var nazocnosti = response.Models;

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Attendance");

            worksheet.Cell(1, 1).Value = "Ime";
            worksheet.Cell(1, 2).Value = "Prezime";
            worksheet.Cell(1, 3).Value = "OIB";
            worksheet.Cell(1, 4).Value = "Vrijeme prijave karticom";

            int row = 2;
            foreach (var nazocnost in nazocnosti)
            {
                worksheet.Cell(row, 1).Value = nazocnost.Student.Ime;
                worksheet.Cell(row, 2).Value = nazocnost.Student.Prezime;
                worksheet.Cell(row, 3).Value = nazocnost.Student.Oib.ToString();
				worksheet.Cell(row, 4).Value = nazocnost.DateScanned.ToString("dd.MM.yyyy HH:mm");
                row++;
            }

            using var memoryStream = new MemoryStream();
            workbook.SaveAs(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Prisutnost_po_terminu.xlsx");
        }

		#region Helper methods

		private async Task<IEnumerable<dynamic>> FetchAttendanceAsyncByKolegijId(int kolegijId)
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
