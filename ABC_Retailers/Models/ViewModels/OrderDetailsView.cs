using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ABC_Retailers.Models;

namespace ABCRetailers.Models.ViewModels
{
    public class OrderDetailsViewModel
    {
        public string CustomerRowKey { get; set; }
        public string ProductRowKey { get; set; }
        [Required]
        [Display(Name = "Customer")]
        public string CustomerId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Product")]
        public string ProductId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Quantity")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        public Orders Order { get; set; } = new Orders();

        // Add proof info for display
        public string? ProofFileName { get; set; } = null;
        public string? ProofFileUrl { get; set; } = null; // optional, if you generate a link
        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Submitted";

        public string RowKey { get; set; }

        public IEnumerable<(string Id, string Name)> Customers { get; set; } = new List<(string, string)>();
        public IEnumerable<(string Id, string ProductName, double Price, int Stock)> Products { get; set; } = new List<(string, string, double, int)>();
    }
}
