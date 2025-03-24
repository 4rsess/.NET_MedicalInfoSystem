using System.ComponentModel.DataAnnotations;

namespace MedicalInfoSystem.Models
{
    public class DoctorEditModel
    {
        [MinLength(1), EmailAddress, Required]
        public string email { get; set; }

        [MinLength(1), MaxLength(1000), Required]
        public string name { get; set; }
        public DateTime birthDate { get; set; }

        [Required]
        public Gender gender { get; set; }
        public string phone { get; set; }
    }
}
