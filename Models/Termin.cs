using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AttendanceChecker.Models
{
    [Table("termini")]
    public class Termin : BaseModel
    {
        [PrimaryKey("termin_id")]
        public int TerminId { get; set; }

        [Column("kolegij_id")]
        public int? KolegijId { get; set; }

        [Column("vrijeme")]
        public DateTime Vrijeme { get; set; }

        // Foreign key relationship to Kolegij
        [Reference(typeof(Kolegij))]
        public Kolegij Kolegij { get; set; }
    }
}