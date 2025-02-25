namespace devDynast.ViewModels
{
    public class MenuViewModel
    {
        public IEnumerable<devDynast.Models.Menu>? Menus { get; set; }

        public devDynast.Models.User? CurrentUser { get; set; }
        public IEnumerable<devDynast.Models.MenuItem>? MenuItems { get; set; }
        public IEnumerable<devDynast.Models.Cart>? Carts { get; set; }
        public IEnumerable<devDynast.ViewModels.CartItemViewModel>? CartItems { get; set; } 
    }
}
