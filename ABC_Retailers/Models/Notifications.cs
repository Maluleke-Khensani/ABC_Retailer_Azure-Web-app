using System;
using System.ComponentModel.DataAnnotations;
using Azure;
using Azure.Data.Tables;

namespace ABC_Retailers.Models
{
    public class Notifications : ITableEntity
    {
        [Display(Name = "Notification ID")]
        public string Id => RowKey;

        [Display(Name = "Message")]
        [Required]
        public string Message { get; set; } = string.Empty;

        [Display(Name = "Category")]
        public string? Category { get; set; }

        [Display(Name = "Product Name")]
        public string? ProductName { get; set; }

        [Display(Name = "New Stock")]
        public int? NewStock { get; set; }

        [Display(Name = "Created At")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        
        public string PartitionKey { get; set; } = "Notifications";
        public string RowKey { get; set; } 

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}

