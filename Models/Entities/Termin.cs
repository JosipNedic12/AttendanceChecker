using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AttendanceChecker.Models.Entities
{
    [Table("termini")]
    public class Termin : BaseModel
    {
        [PrimaryKey("termin_id", false)]
        public int TerminId { get; set; }

        [Column("kolegij_id")]
        public int? KolegijId { get; set; }

        [Column("dvorana_id")]
        public int? DvoranaId { get; set; }

        [Column("start_time")]
        public DateTime StartTime { get; set; }

        [Column("end_time")]
        public DateTime? EndTime { get; set; }

        // Foreign key relationship to Kolegij
        [Reference(typeof(Kolegij))]
        public Kolegij Kolegij { get; set; }

        // Foreign key relationship to Dvorana
        [Reference(typeof(Dvorana))]
        public Dvorana Dvorana { get; set; }
    }
}