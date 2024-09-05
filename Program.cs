using AttendanceChecker.Components;
using AttendanceChecker.Models;
using ClosedXML.Excel;
using Supabase;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Load Supabase configuration from appsettings.json
var supabaseConfig = builder.Configuration.GetSection("Supabase").Get<SupabaseConfig>();

// Initialize Supabase client
var supabaseClient = new Supabase.Client(supabaseConfig.Url, supabaseConfig.AnonKey);
await supabaseClient.InitializeAsync();

// Register services
builder.Services.AddSingleton(supabaseClient);

// Add services to the container for Razor Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

string lastUID = null;

#region API methods

// POST /rfid-scan
app.MapPost("/rfid-scan", async (HttpContext context) =>
{
    var requestBody = await context.Request.ReadFromJsonAsync<RfidRequest>();
    var uid = requestBody?.Uid;
    lastUID = uid;

    if (!string.IsNullOrEmpty(uid))
    {
        // Get student details based on br_kartice
        var student = await supabaseClient
            .From<Student>()
            .Where(x => x.BrKartice == uid)
            .Single();

        if (student is null)
        {
            return Results.NotFound(new { error = "Student not found" });
        }

        var now = DateTime.UtcNow;

        // Query for termin entries within the past 15 minutes
        var terminResponse = await supabaseClient
            .From<Termin>()
            .Where(x => x.Vrijeme >= now.AddMinutes(-15) && x.Vrijeme <= now)
            .Get();

        if (terminResponse.Models.Count > 0)
        {
            foreach (var termin in terminResponse.Models)
            {
                await supabaseClient
                    .From<Nazocnost>()
                    .Insert(new Nazocnost
                    {
                        NazocnostId = Guid.NewGuid().ToString(),
                        TerminId = termin.TerminId,
                        StudentId = student.StudentId
                    });
            }
            return Results.Ok(new { message = "Record inserted successfully" });
        }
        else
        {
            return Results.NotFound(new { error = "No termin found within the last 15 minutes" });
        }
    }
    else
    {
        return Results.BadRequest(new { error = "UID is missing" });
    }
});

// GET /attendance-percentage/{kolegijId}
app.MapGet("/attendance-percentage/{kolegijId:int}", async (int kolegijId) =>
{
    var attendanceData = await FetchAttendanceAsync(supabaseClient, kolegijId);
    return Results.Ok(attendanceData);
});

// GET /export-attendance/{kolegijId}
app.MapGet("/export-attendance/{kolegijId:int}", async (int kolegijId, HttpResponse response) =>
{
    var attendanceData = await FetchAttendanceAsync(supabaseClient, kolegijId);

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

    response.Headers.Add("Content-Disposition", "attachment; filename=attendance.xlsx");
    response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    using var memoryStream = new MemoryStream();
    workbook.SaveAs(memoryStream);
    memoryStream.Seek(0, SeekOrigin.Begin);
    await memoryStream.CopyToAsync(response.Body);
});

// GET /last-uid
app.MapGet("/last-uid", () =>
{
    return string.IsNullOrEmpty(lastUID) ? Results.Ok("No UID received yet.") : Results.Ok($"Last UID received: {lastUID}");
});

// GET /kolegiji
app.MapGet("/kolegiji", async () =>
{
    var kolegijiResponse = await supabaseClient.From<Kolegij>().Get();

    if (!kolegijiResponse.ResponseMessage.IsSuccessStatusCode)
        return Results.Problem("Error fetching kolegiji");

    return Results.Ok(kolegijiResponse.Models);
});

// GET /student/{id}
app.MapGet("/student/{id:int}", async (int id) =>
{
    var student = await supabaseClient
        .From<Student>()
        .Where(x => x.StudentId == id)
        .Single();

    if (student is null)
        return Results.NotFound(new { error = "Student not found" });

    return Results.Ok(student);
});

#endregion

#region Helper methods

async Task<IEnumerable<dynamic>> FetchAttendanceAsync(Client supabaseClient, int kolegijId)
{
    // Step 1: Fetch the total number of termini for the given kolegij
    var totalCount = await GetTotalTerminiForKolegijAsync(supabaseClient, kolegijId);

    // Step 2: Fetch the students
    var students = await GetAllStudentsAsync(supabaseClient);

    // Step 3: Fetch the attendance for each student
    var nazocnosti = await GetAttendanceRecordsAsync(supabaseClient);

    // Step 4: Fetch the termini related to the given kolegij
    var termini = await GetTerminiForKolegijAsync(supabaseClient, kolegijId);

    // Step 5: Calculate attendance for each student
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

async Task<int> GetTotalTerminiForKolegijAsync(Client supabaseClient, int kolegijId)
{
    var response = await supabaseClient
        .From<Termin>()
        .Where(t => t.KolegijId == kolegijId)
        .Get();

    if (!response.ResponseMessage.IsSuccessStatusCode)
        throw new Exception("Error querying total termin count.");

    // Return the count of termini
    return response.Models.Count;
}

async Task<List<Student>> GetAllStudentsAsync(Client supabaseClient)
{
    var response = await supabaseClient
        .From<Student>()
        .Get();

    if (!response.ResponseMessage.IsSuccessStatusCode)
        throw new Exception("Error querying students.");

    // Return the list of students
    return response.Models;
}

async Task<List<Nazocnost>> GetAttendanceRecordsAsync(Client supabaseClient)
{
    var response = await supabaseClient
        .From<Nazocnost>()
        .Get();

    if (!response.ResponseMessage.IsSuccessStatusCode)
        throw new Exception("Error querying attendance records.");

    // Return the list of attendance records
    return response.Models;
}

async Task<List<Termin>> GetTerminiForKolegijAsync(Client supabaseClient, int kolegijId)
{
    var response = await supabaseClient
        .From<Termin>()
        .Where(t => t.KolegijId == kolegijId)
        .Get();

    if (!response.ResponseMessage.IsSuccessStatusCode)
        throw new Exception("Error querying termini for the given kolegij.");

    // Return the list of termini
    return response.Models;
}

#endregion

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
// Map Razor Components (UI components)
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();