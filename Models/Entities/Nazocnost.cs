using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AttendanceChecker.Models.Entities
{
    [Table("nazocnost")]
    public class Nazocnost : BaseModel
    {
        [PrimaryKey("nazocnost_id", false)]
        public int NazocnostId { get; set; }

        [Column("termin_id")]
        public int TerminId { get; set; }

        [Column("student_id")]
        public int StudentId { get; set; }

        [Column("date_scanned")]
        public DateTime DateScanned { get; set; }

        // Foreign key relationships
        [Reference(typeof(Student))]
        public Student Student { get; set; }

        [Reference(typeof(Termin))]
        public Termin Termin { get; set; }
    }
}