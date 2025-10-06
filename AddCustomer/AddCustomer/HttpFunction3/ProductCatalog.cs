using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ABC_Retails_Functions.HttpFunction3
{ 
        public class ProductCatalog : ITableEntity
        {
       
        public string Category { get; set; } = string.Empty; // PartitionKey

        [JsonPropertyName("ProductName")] // Force PascalCase in JSON
        
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
