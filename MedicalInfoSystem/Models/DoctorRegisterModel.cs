using System.ComponentModel.DataAnnotations;

namespace MedicalInfoSystem.Models
{
    public class DoctorRegisterModel
    {
        [MinLength(1), MaxLength(1000), Required]
        public string fullName { get; set; }

        [MinLength(6), Required]
        public string Password { get; set; }

        [MinLength(1), EmailAddress, Required]
        public string email { get; set; }
        public DateTime birthDate { get; set; }

        [Required]
        public Gender gender { get; set; }
        public string phoneNumber { get; set; }

        [Required]
        public Guid speciality { get; set; }

        public Doctor ToDoctor()
        {
            return new Doctor
            {
                fullName = this.fullName,
                password = this.Password,
                email = this.email,
                birthDate = this.birthDate,
                gender = this.gender,
                phoneNumber = this.phoneNumber,
                speciality = this.speciality
            };
        }
    }
}
