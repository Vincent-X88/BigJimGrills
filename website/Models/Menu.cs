using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace devDynast.Models
{
    [Table("menus")]
    public class Menu
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")] 
        public int Id { get; set; }

        [Required]
        [Column("name")] 
        [StringLength(100)]
        public string? Name { get; set; }

        [Required]
        [Column("imageurl")] 
        [StringLength(15)]
        public string? ImageUrl { get; set; }
    }
}
