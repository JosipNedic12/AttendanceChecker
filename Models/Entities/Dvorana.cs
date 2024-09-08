using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AttendanceChecker.Models.Entities
{
    [Table("dvorane")]
    public class Dvorana : BaseModel
    {
        [PrimaryKey("dvorana_id", false)]
        public int DvoranaId { get; set; }

        [Column("naziv")]
        public string Naziv { get; set; }

        [Column("lokacija")]
        public string Lokacija { get; set; }
    }
}