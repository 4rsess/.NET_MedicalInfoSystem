using System.ComponentModel.DataAnnotations;

namespace MedicalInfoSystem.Models
{
    public class SpecialityModel
    {
        [Required]
        public Guid ID { get; set; }

        [Required]
        public DateTime createTime { get; set; } = DateTime.Now;

        [MinLength(1), Required]
        public string fullName { get; set; }
    }
}
