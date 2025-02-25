namespace devDynast.Models
{
    public class CustomerPurchaseFrequencyViewModel
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public int PurchaseCount { get; set; }
    public string Segment { get; set; }
    public DateTime LastPurchaseDate { get; set; }
}

}