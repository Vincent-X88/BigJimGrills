using devDynast.ViewModels; 


namespace devDynast.Models 
{
    public class FeedbackViewModel
{
    public Feedback Feedback { get; set; }

    public int Id { get; set; }
    public string OrderNumber { get; set; }
    public string UserId { get; set; }
    public string Content { get; set; }
    public DateTime SubmittedAt { get; set; }
    public bool IsAdminResponded { get; set; }
    public string AdminResponse { get; set; }
    public IEnumerable<OrderSummaryViewModel> Orders { get; set; }
    public IEnumerable<OrderDetailsViewModel> OrderItems { get; set; }
}


}