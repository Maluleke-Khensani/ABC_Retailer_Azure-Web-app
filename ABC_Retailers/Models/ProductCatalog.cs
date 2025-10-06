using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Azure;
using Azure.Data.Tables;

namespace ABC_Retailers.Models
{
    public class ProductCatalog : ITableEntity
    {
       
        public string Category { get; set; } = string.Empty; // PartitionKey

        [JsonPropertyName("ProductName")] // Force PascalCase in JSON
        [Display(Name = "Product Name")]
        [Required]
        public string ProductName
        {
            get => RowKey;
            set => RowKey = value;
        }
        // RowKey stores the unique product name

        public string Description { get; set; } = string.Empty;

        public double Price { get; set; }

        public int Stock { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        // Azure Table Storage requirements
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
