using System.ComponentModel.DataAnnotations;

namespace MedicalInfoSystem.Models
{
    public class ConsultationCreateModel
    {
        [Required]
        public Guid specialityId { get; set; }

        [Required]
        public InspectionCommentCreateModel comment { get; set; }
    }
}
