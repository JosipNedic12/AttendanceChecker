using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("termini", Schema = "public")]
public class Termin
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("termin_id")]
    public int TerminId { get; set; }

    [Column("kolegij_id")]
    public int? KolegijId { get; set; }

    [Required]
    [Column("vrijeme")]
    public DateTime Vrijeme { get; set; }

    // Foreign key relationship to Kolegij
    [ForeignKey("KolegijId")]
    public virtual Kolegij Kolegij { get; set; }
}
