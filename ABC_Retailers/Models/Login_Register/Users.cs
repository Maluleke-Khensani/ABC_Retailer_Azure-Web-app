using System.ComponentModel.DataAnnotations;

namespace ABC_Retailers.Models.Login_Register
{
    public class Users
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [MaxLength(50, ErrorMessage = "Username cannot exceed 50 characters")]
        [Display(Name = "Username")]    
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [MaxLength(100, ErrorMessage = "Password cannot exceed 100 characters")]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Role is required")]
        [MaxLength(20, ErrorMessage = "Role cannot exceed 20 characters")]
        [Display(Name = "Role")]
        public string Role { get; set; }    
    }
}
