
namespace devDynast.Models
{
 public class SalesFilterViewModel
{
    public List<TopSellingItemsByMonthViewModel> TopSellingItems { get; set; }
}

public class TopSellingItemsByMonthViewModel
{
    public string MonthYear { get; set; }
    public List<TopSellingItemsViewModel> TopItems { get; set; }
}


}

