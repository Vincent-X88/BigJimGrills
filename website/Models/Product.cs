using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace devDynast.Models
{
    [Table("product")]
    public class Product
    {
        [Key]
        [Column("id")]
        public string? Id { get; set; } 

        [Required]
        [Column("name")]
        public string? Name { get; set; } 
    }
}
