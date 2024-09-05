using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("nazocnost", Schema = "public")]
public class Nazocnost
{
    [Key]
    [Column("nazocnost_id")]
    public string NazocnostId { get; set; }

    [Column("termin_id")]
    public int? TerminId { get; set; }

    [Column("student_id")]
    public int? StudentId { get; set; }

    // Foreign key relationships
    [ForeignKey("StudentId")]
    public virtual Student Student { get; set; }

    [ForeignKey("TerminId")]
    public virtual Termin Termin { get; set; }
}
