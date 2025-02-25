namespace devDynast.ViewModels
{
    public class CartItemViewModel
    {
        public int Id { get; set; } 
        public int Quantity { get; set; }
        public double Price { get; set; }
        public string? ProductName { get; set; } 
        public string? ProductImage { get; set; } 
        public string? Category { get; set; } 
        public string? ExtraIds { get; set; }
        
        public string? SpecialNote { get; set; }
        public List<ExtraViewModel>? SelectedExtras { get; set; }

         public int CartItemId { get; set; }
         public int MenuItemId { get; set; }
        
        

    }


}
