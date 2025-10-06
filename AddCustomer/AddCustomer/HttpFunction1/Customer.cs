using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;


namespace ABC_Retails_Functions.HttpFunction1
{
    public class Customer : ITableEntity
    {
  
        public string CustomerId => RowKey;   // RowKey acts as CustomerId 

    
        [Required, StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

       
        [Required, StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        
        [Required]
        public string CustomerType { get; set; } = "Retail";
        // stored in table, shown in UI, used for PartitionKey

        [Required, StringLength(20)]
        public string Username { get; set; } = string.Empty;

        [EmailAddress, Required]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string ShippingAddress { get; set; } = string.Empty;


        // Azure Table required fields
        public string PartitionKey { get; set; } = "Retail";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        public DateTimeOffset? Timestamp { get; set; }// Auto-filled by Azure
        public ETag ETag { get; set; } // Tracks row version for concurrency
    }
}
