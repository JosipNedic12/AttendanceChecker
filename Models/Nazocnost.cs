using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AttendanceChecker.Models
{
    [Table("nazocnost")]
    public class Nazocnost : BaseModel
    {
        [PrimaryKey("nazocnost_id")]
        public string NazocnostId { get; set; }

        [Column("termin_id")]
        public int? TerminId { get; set; }

        [Column("student_id")]
        public int? StudentId { get; set; }

        // Foreign key relationships
        [Reference(typeof(Student))]
        public Student Student { get; set; }

        [Reference(typeof(Termin))]
        public Termin Termin { get; set; }
    }
}