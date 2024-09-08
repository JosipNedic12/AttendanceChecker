using Supabase.Postgrest.Models;

namespace AttendanceChecker.Models
{
    public class KolegijAttendance 
    {
        public string KolegijName { get; set; }
        public string Percentage { get; set; }
    }
}