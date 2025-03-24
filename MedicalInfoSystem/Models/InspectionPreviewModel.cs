using System.ComponentModel.DataAnnotations;

namespace MedicalInfoSystem.Models
{
    public class InspectionPreviewModel
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public DateTime createTime { get; set; }
        public Guid? previousId { get; set; }

        [Required]
        public DateTime date { get; set; }

        [Required]
        public Conclusion conclusion { get; set; }
        [Required]

        public Guid doctorId { get; set; }

        [MinLength(1), Required]
        public string doctor { get; set; }

        [Required]
        public Guid patientId { get; set; }

        [MinLength(1), Required]
        public string patient { get; set; }

        [Required]
        public DiagnosisModel diagnosis { get; set; }
        public bool hasChain { get; set; }
        public bool hasNested { get; set; }
    }
}
