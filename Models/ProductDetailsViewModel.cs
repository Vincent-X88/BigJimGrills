

namespace devDynast.Models
{
    public class ProductDetailsViewModel
{
    public MenuItem? Product { get; set; }
    public List<MenuIngredient>? Extras { get; set; }
    public bool IsAvailable { get; set; }
}

}