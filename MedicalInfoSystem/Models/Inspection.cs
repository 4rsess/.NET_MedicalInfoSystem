namespace MedicalInfoSystem.Models
{
    public class Inspection
    {
        public Guid ID { get; set; }
        public Guid patientId { get; set; }
        public Guid doctorId { get; set; }
        public DateTime date { get; set; }
        public string anamnesis { get; set; }
        public string complaints { get; set; }
        public string treatment { get; set; }
        public Conclusion conclusion { get; set; }
        public DateTime? nextVisitDate { get; set; }
        public DateTime? deathDate { get; set; }
        public Guid? previousInspectionId { get; set; }
        public DateTime createTime { get; set; } = DateTime.Now;

        public List<Diagnosis> diagnoses { get; set; }
        public Consultation consultation { get; set; }
    }
}
