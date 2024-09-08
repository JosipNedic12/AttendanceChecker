using AttendanceChecker.Components;
using AttendanceChecker.Models;
var builder = WebApplication.CreateBuilder(args);

// Register HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Register HttpClient
builder.Services.AddHttpClient("API", (serviceProvider, client) =>
{
    var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
    var request = httpContextAccessor.HttpContext?.Request;
    if (request != null)
    {
        var baseUrl = $"{request.Scheme}://{request.Host}";
        client.BaseAddress = new Uri(baseUrl);
    }
});

// Add Swagger services
builder.Services.AddEndpointsApiExplorer(); // Necessary for minimal APIs
builder.Services.AddSwaggerGen(); // Add Swagger generation services

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

// Add Newtonsoft.Json
builder.Services.AddControllers().AddNewtonsoftJson();

var app = builder.Build();

string lastUID = null;

// Enable Swagger middleware in the request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // Enable middleware to serve Swagger
    app.UseSwaggerUI(); // Enable middleware to serve Swagger UI
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Map Razor Components (UI components)
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapControllers();

app.Run();