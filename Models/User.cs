using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace devDynast.Models
{
    [Table("user")] 
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int? Id { get; set; }
        

        [Column("first_name")] 
        [StringLength(100)]
        public string? FirstName { get; set; }

        [Column("last_name")] 
        [StringLength(100)]
        public string? LastName { get; set; }

        [Required]
        [Column("email")] 
        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        [Column("password")] 
        [StringLength(100)]
        public string? Password { get; set; }

        [Required]
        [Column("phone_number")]
        [StringLength(15)]
        public string? PhoneNumber { get; set; }


        [Required]
        [Column("Role")]
        [StringLength(15)]
        public string? Role { get; set; } 
        
    
        
    }
}
