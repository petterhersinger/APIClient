using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace APIClient.Models;

public class PrenumerantModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Prenumerationsnummer { get; set; }
    public string Personnummer { get; set; }
    public string Fornamn { get; set; }
    public string Efternamn { get; set; }
    public string Utdelningsadress { get; set; }
    public string Postnummer { get; set; }
    public string Telefonnummer { get; set; }
    public string Ort { get; set; }
}