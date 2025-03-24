using System.ComponentModel.DataAnnotations;

namespace MedicalInfoSystem.Models
{
    public class DiagnosisModel
    {
        [Required]
        public Guid ID { get; set; }

        [Required]
        public DateTime createTime { get; set; }

        [MinLength(1), Required]
        public string code { get; set; }

        [MinLength(1), Required]
        public string name { get; set; }
        public string description { get; set; }

        [Required]
        public DiagnosisType type { get; set; }
    }
}
