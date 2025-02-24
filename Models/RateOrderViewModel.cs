namespace devDynast.ViewModels
{
    public class RateOrderViewModel
{
    public string OrderNumber { get; set; }
    public List<OrderDetailsViewModel> OrderDetails { get; set; }
    public int Rating { get; set; } // Rating 1 to 5
    public string Comment { get; set; } // User's comment

     public bool HasRated { get; set; } 
}

}