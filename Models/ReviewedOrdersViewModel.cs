using devDynast.ViewModels;

namespace devDynast.Models
{
    public class ReviewedOrderViewModel
{
    public string? OrderNumber { get; set; }
    public int RatingValue { get; set; }
    public string? Comment { get; set; }
    public string? UserId { get; set; }
     public string? UserName { get; set; }
     public List<OrderDetailsViewModel>? OrderItems { get; set; }
}

}