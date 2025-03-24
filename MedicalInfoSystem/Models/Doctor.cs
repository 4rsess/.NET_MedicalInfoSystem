using System.ComponentModel.DataAnnotations;

namespace MedicalInfoSystem.Models
{
    public class Doctor
    {
        public Guid ID { get; set; }
        public string fullName { get; set; }
        public DateTime birthDate { get; set; }
        public Gender gender { get; set; }
        public string phoneNumber { get; set; }
        public Guid speciality { get; set; }
        public string password { get; set; }
        public DateTime createTime { get; set; } = DateTime.Now;
        public string email { get; set; }
    }
}
