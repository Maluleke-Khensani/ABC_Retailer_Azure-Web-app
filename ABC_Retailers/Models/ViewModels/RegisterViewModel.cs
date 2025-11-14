using System.ComponentModel.DataAnnotations;

namespace ABC_Retailers.Models.ViewModels
{
    public class RegisterViewModel
    {

        [Required(ErrorMessage = "Username is reequired")]
        public string Username { get; set; } = string.Empty;


        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;


        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;


        [Required(ErrorMessage = "Confirm Password is required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;


        [Required(ErrorMessage ="First Name is required")]
        public string FirstName { get; set; } = string.Empty;

        
        [Required(ErrorMessage = "Last Name is required")]
        public string LastName { get; set; } = string.Empty;


        [Required(ErrorMessage = "Shipping Address is required")]
        public string ShippingAddress { get; set; } = string.Empty;


        public string Role { get; set; } = "Customer";


    }
}
