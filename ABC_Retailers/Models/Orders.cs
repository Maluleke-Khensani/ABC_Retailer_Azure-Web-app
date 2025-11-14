using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ABC_Retailers.Models;
using Azure;
using Azure.Data.Tables;
using Microsoft.EntityFrameworkCore;



namespace ABC_Retailers.Models

{

    public class Orders : ITableEntity

    {

        [Display(Name = "Order ID")]
        [Required]
        public string OrderId => RowKey; // Azure Table RowKey

        [Display(Name = "Customer ID")]
        [Required]
        public string CustomerId { get; set; } = string.Empty;

    [Display(Name = "Customer Username")]
    public string Username { get; set; } = string.Empty;

    [Display(Name = "Product ID")]
    public string ProductId { get; set; } 

    [Display(Name = "Product Name")]
    public string ProductName { get; set; } = string.Empty;

    [Display(Name = "Order Date")]
    public DateTime OrderDate { get; set; } = DateTime.Now;

    [Display(Name = "Quantity")]
    public int Quantity { get; set; }

    [Display(Name = "Unit Price")]
    public double UnitPrice { get; set; }

    [Display(Name = "Total Price")]
    public double TotalPrice { get; set; }

    [Display(Name = "Order Status")]
    public string Status { get; set; } = "Pending";

     public string? ProofFileName { get; set; }
    
        [NotMapped]
        public string DisplayOrderId { get; set; } = string.Empty;

        public enum OrderStatus
        {
            Pending,      // when customer first places order
            Processing,   // when admin processes it
            Processed,    // when admin marks it complete
            Cancelled     // optional for future use
        }

        // Grouped by CustomerId
        public string PartitionKey { get; set; } = string.Empty;

    // RowKey= OrderId 
    public string RowKey { get; set; } = Guid.NewGuid().ToString();

    // Auto-filled by Azure 
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    }
}
