namespace devDynast.Models
{
public class SalesCompareViewModel
{
   
    public string? ProductId { get; set; }

   
    public string? GroupedBy { get; set; } 

    // Total count of items sold
    public int SalesCount { get; set; }

    // Total revenue generated
    public double TotalRevenue { get; set; }
    public int? Month { get; set; }

   
    public string? DateGroup { get; set; }

    
}

}
