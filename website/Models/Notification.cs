using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace devDynast.Models
{
    [Table("notifications")]
    public class Notification
    {
        [Key]
        [Column("id")] 
        public int Id { get; set; }

        [Required]
        [Column("userid")] 
        public int UserId { get; set; }

        [Required]
        [Column("message")] 
        public string? Message { get; set; }

        [Required]
        [Column("createdat")] 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("isread")] 
        public bool IsRead { get; set; }
        
        [Required]
        [Column("orderid")] 
        public string? OrderId { get; set; }
        
       
        [Column("intended")]  
        public string? Intended { get; set; }  
    }

    public class NotificationViewModel
    {
        public string? Message { get; set; }
        public int UserId { get; set; }
        public bool IsRead { get; set; }
        public string? OrderId { get; set; }
        public DateTime CreatedAt { get; set; }
        
        
        public string? Intended { get; set; } 
    }
}
