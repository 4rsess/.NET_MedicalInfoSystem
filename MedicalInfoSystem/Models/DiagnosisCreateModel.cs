using System.ComponentModel.DataAnnotations;

namespace MedicalInfoSystem.Models
{
    public class DiagnosisCreateModel
    {
        [Required]
        public Guid icdDiagnosisId { get; set; }

        [MaxLength(5000)]
        public string description { get; set; }

        [Required]
        public DiagnosisType type { get; set; }
    }
}
