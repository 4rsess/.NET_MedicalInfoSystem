namespace MedicalInfoSystem.Models
{
    public class Diagnosis
    {
        public Guid ID { get; set; }
        public Guid icdDiagnosisId { get; set; }
        public string description { get; set; }
        public DiagnosisType type { get; set; }
        public DateTime createTime { get; set; } = DateTime.Now;
    }
}
