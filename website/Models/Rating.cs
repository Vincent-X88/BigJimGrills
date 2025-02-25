using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace devDynast.Models
{
    [Table("ratings")]
    public class Rating
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("order_number")]
        public string? OrderNumber { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Please select a rating between 1 and 5.")]
        [Column("rating_value")]
        public int RatingValue { get; set; }  

        [Required]
        [Column("comment")]
        public string? Comment { get; set; }

        [Column("user_id")] 
        public string? UserId { get; set; } 

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
