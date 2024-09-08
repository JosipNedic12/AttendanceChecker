using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AttendanceChecker.Models.Entities
{
    [Table("kolegiji")]
    public class Kolegij : BaseModel
    {
        [PrimaryKey("kolegij_id", false)]
        public int KolegijId { get; set; }

        [Column("naziv")]
        public string Naziv { get; set; }

        [Column("profesor")]
        public string Profesor { get; set; }

        [Column("asistent")]
        public string? Asistent { get; set; }
    }
}
