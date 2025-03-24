using System.ComponentModel.DataAnnotations;

namespace MedicalInfoSystem.Models
{
    public class PatientModel
    {
        [Required]
        public Guid ID { get; set; }

        [Required]
        public DateTime createTime { get; set; }

        [MinLength(1), Required]
        public string fullName { get; set; }
        public DateTime birthDate { get; set; }

        [Required]
        public Gender gender { get; set; }
    }
}
