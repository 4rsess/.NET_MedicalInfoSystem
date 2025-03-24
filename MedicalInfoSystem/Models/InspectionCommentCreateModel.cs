using System.ComponentModel.DataAnnotations;

namespace MedicalInfoSystem.Models
{
    public class InspectionCommentCreateModel
    {
        [Required]
        public string content { get; set; }
    }
}
