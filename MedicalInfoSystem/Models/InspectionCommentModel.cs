using System.ComponentModel.DataAnnotations;

namespace MedicalInfoSystem.Models
{
    public class InspectionCommentModel
    {
        [Required]
        public Guid ID { get; set; }

        [Required]
        public DateTime CreateTime { get; set; }
        public Guid? ParentId { get; set; }
        public string Content { get; set; }
        public DoctorModel Author { get; set; }
        public DateTime? ModifyTime { get; set; } = DateTime.Now;
    }
}
