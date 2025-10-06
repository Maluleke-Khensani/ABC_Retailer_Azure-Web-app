using System;
using System.ComponentModel.DataAnnotations;
using Azure;
using Azure.Data.Tables;



namespace ABC_Retailers.Models

{

    public class Orders : ITableEntity

    {


    [Display(Name = "Order ID")]
        [Required]
    public string OrderId => RowKey;   // RowKey acts as the unique OrderId 

    [Display(Name = "Customer ID")]
        [Required]
        public string CustomerId { get; set; } = string.Empty;

    [Display(Name = "Customer Username")]
    public string Username { get; set; } = string.Empty;

    [Display(Name = "Product ID")]
    public string ProductId { get; set; } = string.Empty;

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
    // Example of status: Pending, Processing, Completed, Cancelled 

    public enum OrderStatus
        {
            Pending,
            Processing,
            Completed,
            Cancelled
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