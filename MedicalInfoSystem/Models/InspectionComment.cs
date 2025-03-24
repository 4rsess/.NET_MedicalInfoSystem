namespace MedicalInfoSystem.Models
{
    public class InspectionComment
    {
        public Guid ID { get; set; }
        public Guid consultationId { get; set; }
        public string content { get; set; }
        public DateTime createTime { get; set; } = DateTime.Now;
    }

}
