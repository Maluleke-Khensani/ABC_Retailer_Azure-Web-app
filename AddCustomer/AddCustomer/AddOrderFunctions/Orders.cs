using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;

namespace ABC_Retails_Functions.AddOrderFunctions
{
    public class Orders : ITableEntity
    {
        public string OrderId => RowKey;   // RowKey acts as the unique OrderId 

        public string CustomerId { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
        public double TotalPrice { get; set; }
        public string Status { get; set; } = "Pending";

        // Grouped by CustomerId
        public string PartitionKey { get; set; } = string.Empty;

        // RowKey= OrderId 
        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        // Auto-filled by Azure 
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

    }
}