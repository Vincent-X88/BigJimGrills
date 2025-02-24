using System;
using System.Collections.Generic;

namespace devDynast.ViewModels
{
    public class OrderDetailsViewModel
    {
        public int Id { get; set; } 
        public string? ProductName { get; set; }
        public double Price { get; set; } 
        public double BasePrice { get; set; } 
        public int Quantity { get; set; } 
        public DateTime CreatedAt { get; set; } 
        public string? ProductImage { get; set; }

        public DateTime Date { get; set; }
        public string? Category { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Status { get; set; }
        public string? OrderNumber { get; set; }

        public string? SpecialNote { get; set; }
        public List<ExtraViewModel>? Extras { get; set; } 
        public string? ExtraIds { get; set; } 

        
    }

    public class ExtraViewModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public double Price { get; set; }
    }
}
