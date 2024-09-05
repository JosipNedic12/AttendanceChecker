using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("kolegiji", Schema = "public")]
public class Kolegij
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("kolegij_id")]
    public int KolegijId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("naziv")]
    public string Naziv { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("profesor")]
    public string Profesor { get; set; }

    [MaxLength(255)]
    [Column("asistent")]
    public string? Asistent { get; set; }

    [Column("br_sati")]
    public long? BrojSati { get; set; }
}
