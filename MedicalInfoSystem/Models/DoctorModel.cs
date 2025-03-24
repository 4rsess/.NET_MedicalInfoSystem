using System.ComponentModel.DataAnnotations;

namespace MedicalInfoSystem.Models
{
    public class DoctorModel
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public DateTime CreateTime { get; set; }

        [MinLength(1), Required]
        public string Name { get; set; }
        public DateTime BirthDate { get; set; }

        [Required]
        public Gender Gender { get; set; }

        [MinLength(1), EmailAddress, Required]
        public string Email { get; set; }
        public string Phone { get; set; }
        public Guid Speciality { get; set; }
    }
}
