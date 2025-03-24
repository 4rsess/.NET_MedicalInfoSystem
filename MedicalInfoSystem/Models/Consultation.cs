namespace MedicalInfoSystem.Models
{
    public class Consultation
    {
        public Guid ID { get; set; }
        public Guid inspectionId { get; set; }
        public Guid specialityId { get; set; }
        public DateTime createTime { get; set; } = DateTime.Now;

        public InspectionComment comment { get; set; }
    }

}
