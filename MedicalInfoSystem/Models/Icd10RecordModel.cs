using System.ComponentModel.DataAnnotations;

namespace MedicalInfoSystem.Models
{
    public class Icd10RecordModel
    {
        [Required]
        public Guid ID { get; set; }

        [Required]
        public DateTime createTime { get; set; } = DateTime.Now;
        public string code { get; set; }
        public string fullName { get; set; }
    }
}
