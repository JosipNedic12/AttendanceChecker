using AttendanceChecker.Components;

var builder = WebApplication.CreateBuilder(args);

// Load Supabase configuration from appsettings.json
var supabaseConfig = builder.Configuration.GetSection("Supabase").Get<SupabaseConfig>();

// Initialize Supabase client
var supabaseClient = new Supabase.Client(supabaseConfig.Url, supabaseConfig.AnonKey);

// Register the Supabase client as a singleton
builder.Services.AddSingleton(supabaseClient);

// Add services to the container for Razor Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts(); // Use HTTP Strict Transport Security in production
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Map Razor Components (UI components)
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Define configuration class for Supabase
public class SupabaseConfig
{
    public string Url { get; set; }
    public string AnonKey { get; set; }
}
