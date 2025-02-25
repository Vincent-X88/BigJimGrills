using devDynast.ViewModels;

namespace devDynast.Models

{
    public class OrderSummaryViewModel
{
    public string? OrderNumber { get; set; }
    public int TotalQuantity { get; set; }
    public double TotalPrice { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool HasComplained { get; set; } 

    public bool HasRated { get; set; }

    public string? SpecialNote { get; set; }
    public List<OrderDetailsViewModel>? OrderItems { get; set; }
}

}