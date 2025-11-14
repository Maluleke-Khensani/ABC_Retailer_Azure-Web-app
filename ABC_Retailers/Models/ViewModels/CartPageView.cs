using ABC_Retailers.Models.ViewModels;

namespace ABC_Retailers.Models.ViewModels
{
    public class CartPageView
    {
        public List<CartItemViewModel> Items { get; set; } = new();
        public double Total => Items.Sum(i => i.Subtotal);
    }
}
