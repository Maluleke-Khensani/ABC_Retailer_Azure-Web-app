using ABC_Retailers.Models;
using ABCRetailers.Models;
using System.Collections.Generic;

namespace ABCRetailers.Models.ViewModels
{
    public class HomeViewModel
    {
        public int CustomerCount { get; set; }
        public int ProductCount { get; set; }
        public int OrderCount { get; set; }

        public List<ProductCatalog> FeaturedProducts { get; set; } = new List<ProductCatalog>();
    }
}
