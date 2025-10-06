using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ABC_Retailers.Models
{
    public class ProofOfPaymentRecord
    {
    
        [Display(Name = "Proof of Payment")]
        [Required(ErrorMessage = "Please upload a proof of payment file.")]
        public IFormFile ProofOfPayment { get; set; } = default!;

        [Display(Name = "Order ID")]
        [Required(ErrorMessage = "Order ID is required.")]
        public string OrderId { get; set; } = string.Empty;

        [Display(Name = "Customer Username")]
        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; } = string.Empty;

        public DateOnly PaymentDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        // dropdown list of orders
        public List<SelectListItem> Orders { get; set; } = new();
    }
}
