using System.ComponentModel.DataAnnotations;

namespace MedicalInfoSystem.Models
{
    public class CommentModel
    {
        [Required]
        public Guid id { get; set; }

        [Required]
        public DateTime createTime { get; set; }
        public DateTime? modifiedDate { get; set; } = DateTime.Now;

        [MinLength(1), Required]
        public string content { get; set; }

        [Required]
        public Guid authorId { get; set; }

        [MinLength(1), Required]
        public string author { get; set; }
        public Guid? parentId { get; set; } = new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa6");

    }
}
