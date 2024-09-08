using AttendanceChecker.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Supabase;

namespace AttendanceChecker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentController : ControllerBase
    {
        private readonly Client _supabaseClient;

        public StudentController(Client supabaseClient)
        {
            _supabaseClient = supabaseClient;
        }

        [HttpGet("")]
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
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetStudent(int id)
        {
            var student = await _supabaseClient.From<Student>().Where(x => x.StudentId == id).Single();

            if (student is null)
                return NotFound(new { error = "Student not found" });

            return Ok(student);
        }
    }
}