using System.ComponentModel.DataAnnotations;

namespace ABC_Retailers.Models.ViewModels
{
    public class LoginViewModel
    {
            [Required(ErrorMessage = "Username is required.")]
            public string Username { get; set; } = string.Empty;


            [Required(ErrorMessage = "Password is required.")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty; // "Admin" or "Customer"
        }
    }