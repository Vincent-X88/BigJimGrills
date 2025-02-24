using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace devDynast.Models
{
    [Table("menu_ingredients")]
    public class MenuIngredient
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("name")]
        [StringLength(100)]
        public string? Name { get; set; }

        [Required]
        [Column("category")]
        [StringLength(100)]
        public string? Category { get; set; }

        [Required]
        [Column("price")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public double Price { get; set; }

        [Required]
        [Column("quantity")]
        public int Quantity { get; set; }
    }
}
