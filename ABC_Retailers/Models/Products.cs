
using System.ComponentModel.DataAnnotations;
using Azure;
using Azure.Data.Tables;



namespace ABC_Retailers.Models

{

    public class Products : ITableEntity

    {
        [Display(Name = "Product ID")]
        public string ProductId => RowKey;   // Unique per inventory entry

        [Display(Name = "Category")]
        [Required]
        public string Category { get; set; } = string.Empty;
 // Will also be PartitionKey (groups products by category)

        [Display(Name = "Product Name")]
        [Required]
        public string ProductName { get; set; } = string.Empty;
        // Must exist in ProductCatalog

        [Display(Name = "Stock Quantity")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative")]
        public int Stock { get; set; }

        [Display(Name = "Selling Price")]
        [Range(0.01, 1000000, ErrorMessage = "Price must be greater than 0")]
        public double Price { get; set; }


        // Azure Table Storage required properties
        public string PartitionKey { get; set; } = string.Empty;  // = Category
        public string RowKey { get; set; } = Guid.NewGuid().ToString(); // Unique ID
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}