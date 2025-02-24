using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace devDynast.Models
{
    [Table("menu")]
    public class MenuItem
    {
        [Key]
        [Column("id")] 
        public int Id { get; set; }

        [Required]
        [Column("name")]
        [StringLength(80)]
        public string? Name { get; set; }

        [Required]
        [Column("category")]
        [StringLength(80)]
        public string? Category { get; set; }

        [Required]
        [Column("description")]
        [StringLength(255)]
        public string? Description { get; set; }

        [Required]
        [Column("imageurl")]
        [StringLength(255)]
        public string? ImageUrl { get; set; }

        [Required]
        [Column("price")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public float? Price { get; set; }
    }
}
