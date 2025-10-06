using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;

namespace ABC_Retails_Functions.AddProductFunctions
{
    public class Products : ITableEntity
    {
        
        public string ProductId => RowKey;   // Unique per inventory entry
        public string Category { get; set; } = string.Empty;   //Will also be PartitionKey (groups products by category)
        public string ProductName { get; set; } = string.Empty;
        public int Stock { get; set; }
        public double Price { get; set; }
       
        // Azure Table Storage required properties
        public string PartitionKey { get; set; } = string.Empty;  // = Category
        public string RowKey { get; set; } = Guid.NewGuid().ToString(); // Unique ID
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
