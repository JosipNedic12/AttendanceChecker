using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AttendanceChecker.Models
{
    [Table("studenti")]
    public class Student : BaseModel
    {
        [PrimaryKey("student_id")]
        public int StudentId { get; set; }

        [Column("ime")]
        public string Ime { get; set; }

        [Column("prezime")]
        public string Prezime { get; set; }

        [Column("oib")]
        public string Oib { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("slika")]
        public string? Slika { get; set; }

        [Column("br_kartice")]
        public string BrKartice { get; set; }
    }
}