using System.ComponentModel.DataAnnotations;

namespace MedicalInfoSystem.Models
{
    public class CommentCreateModel
    {
        [MinLength(1), MaxLength(1000), Required]
        public string content { get; set; }
        public Guid parentId { get; set; }
    }
}
