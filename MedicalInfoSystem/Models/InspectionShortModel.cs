using System.ComponentModel.DataAnnotations;

namespace MedicalInfoSystem.Models
{
    public class InspectionShortModel
    {
        [Required]
        public Guid ID { get; set; }

        [Required]
        public DateTime createTime { get; set; }

        [Required]
        public DateTime date { get; set; }

        [Required]
        public DiagnosisModel diagnosis { get; set; }
    }
}
