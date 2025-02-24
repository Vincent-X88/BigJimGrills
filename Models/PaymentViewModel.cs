namespace devDynast.Models
{
    public class PaymentViewModel
{
    public decimal TotalAmount { get; set; }
    public bool IsScheduled { get; set; } 
    public string? ScheduledDate { get; set; } 
    public string? ScheduledTime { get; set; } 
}

}
