using System.ComponentModel.DataAnnotations;

namespace MedicalInfoSystem.Models
{
    public class LoginCredentialsModel
    {
        [MinLength(1), EmailAddress, Required]
        public string email { get; set; }

        [MinLength(1), Required]
        public string password { get; set; }
    }
}
