using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("studenti", Schema = "public")]
public class Student
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("student_id")]
    public int StudentId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("ime")]
    public string Ime { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("prezime")]
    public string Prezime { get; set; }

    [Required]
    [MaxLength(11)]
    [Column("oib")]
    public string Oib { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("email")]
    public string Email { get; set; }

    [Column("slika")]
    public string? Slika { get; set; }

    [Required]
    [Column("br_kartice")]
    public string BrKartice { get; set; }
}
