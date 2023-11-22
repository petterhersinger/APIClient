using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace APIClient.Models;

public class AnnonsModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AnnonsId { get; set; }
    public int AnnonsorId { get; set; }
    public string Rubrik { get; set; }
    public string Innehall { get; set; }
    public decimal Pris { get; set; }
    public decimal VaransPris { get; set; }
}