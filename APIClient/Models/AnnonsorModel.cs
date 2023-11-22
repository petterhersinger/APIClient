using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace APIClient.Models;

public class AnnonsorModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AnnonsorId { get; set; }
    public int? PrenumerantId { get; set; }
    public bool ForetagsAnnonsor { get; set; }
    public string Namn { get; set; }
    public string? Organisationsnummer { get; set; }
    public string Telefonnummer { get; set; }
    public string Utdelningsadress { get; set; }
    public string Postnummer { get; set; }
    public string Ort { get; set; }
    public string? Fakturaadress { get; set; }
}