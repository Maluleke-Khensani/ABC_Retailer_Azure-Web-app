namespace ABC_Retailers.Models.ViewModels
{
    public class CartItemViewModel
    {
        public string ProductId { get; set; } 
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
        public string? ImageUrl { get; set; }

        public double Subtotal { get; set; }
    }
}
