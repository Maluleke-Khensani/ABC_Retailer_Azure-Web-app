using System.ComponentModel.DataAnnotations;

namespace ABC_Retailers.Models
{
    public class Cart
    {
        public int Id { get; set; }

        [Required]
        public string CustomerUsername { get; set; }

        [Required]
        public string ProductId { get; set; }

        [Required]
        public int Quantity { get; set; }


    }
}
