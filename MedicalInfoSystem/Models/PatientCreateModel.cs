using System.ComponentModel.DataAnnotations;

namespace MedicalInfoSystem.Models
{
    public class PatientCreateModel
    {
        [MinLength(1), MaxLength(1000), Required]
        public string fullName { get; set; }
        public DateTime birthDate { get; set; }

        [Required]
        public Gender gender { get; set; }
    }
}
