using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Azure;
using Azure.Data.Tables;

namespace ABC_Retailers.Models
{
    public class ProductCatalog : ITableEntity
    {
        [Required]
        public string Category { get; set; } = string.Empty; // PartitionKey

        [Required]
        public string ProductName { get; set; } = string.Empty; // Display + search name

        public string Description { get; set; } = string.Empty;

        [Range(0.0, double.MaxValue)]
        public double Price { get; set; }

        [Range(0, int.MaxValue)]
        public int Stock { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        // Azure Table Storage required fields
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = Guid.NewGuid().ToString(); // Unique product ID
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
