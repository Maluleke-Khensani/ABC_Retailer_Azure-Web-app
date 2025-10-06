using System.ComponentModel.DataAnnotations;
using Azure;
using Azure.Data.Tables;

namespace ABC_Retailers.Models
{
    public class Customers : ITableEntity
    {
        [Display(Name = "Customer ID")]
        public string CustomerId => RowKey;   // RowKey acts as CustomerId 

        [Display(Name = "First Name")]
        [Required, StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Last Name")]
        [Required, StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Customer Type")]
        [Required]
        public string CustomerType { get; set; } = "Retail";
        // stored in table, shown in UI, used for PartitionKey

        [Display(Name = "Username")]
        [Required, StringLength(20)]
        public string Username { get; set; } = string.Empty;

        [Display(Name = "Email Address")]
        [EmailAddress, Required]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Shipping Address")]
        [Required, StringLength(200)]
        public string ShippingAddress { get; set; } = string.Empty;


        // Azure Table required fields
        public string PartitionKey { get; set; } = "Retail"; 
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        
        public DateTimeOffset? Timestamp { get; set; }// Auto-filled by Azure
        public ETag ETag { get; set; } // Tracks row version for concurrency
    }
}
