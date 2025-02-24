using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace devDynast.Models
{
    [Table("cart")]
    public class Cart
    {
        [Key]
        [Column("id")] 
        public int Id { get; set; }

        [Required]
        [Column("user_id")] 
        public string? UserId { get; set; }

        [Required]
        [Column("product_id")] 
        public string? ProductId { get; set; }

        [Required]
        [Column("quantity")] 
        public int Quantity { get; set; }

        [Required]
        [Column("price")] 
        public double Price { get; set; }

        [Required]
        [Column("status")] 
        public string? Status { get; set; }

        [Column("extra_ids")]
        public string? ExtraIds { get; set; } 

        [Column("order_number")] 
        public string? OrderNumber { get; set; } 

        [Column("scheduled_pickup_date")]
        public DateTime? ScheduledPickupDate { get; set; }

        [Column("scheduled_pickup_time")]
        public TimeSpan? ScheduledPickupTime { get; set; } 

        [Required]
        [Column("is_scheduled")]
        public bool IsScheduled { get; set; } 

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("special_note")]
        public string? SpecialNote { get; set; }
    }
}
