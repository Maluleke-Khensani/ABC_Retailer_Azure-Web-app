using System.Collections.Generic;

namespace ABCRetailers.Models.ViewModels
{
    public class OrderCreateViewModel
    {
        public string CustomerRowKey { get; set; }
        public string ProductRowKey { get; set; }
        public int Quantity { get; set; } = 1;
        public string RowKey { get; set; }

        public IEnumerable<(string Id, string Name)> Customers { get; set; } = new List<(string, string)>();
        public IEnumerable<(string Id, string ProductName, double Price, int Stock)> Products { get; set; } = new List<(string, string, double, int)>();
    }
}
