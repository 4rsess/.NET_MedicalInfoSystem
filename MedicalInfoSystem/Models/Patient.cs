namespace MedicalInfoSystem.Models
{
    public class Patient
    {
        public Guid ID { get; set; }
        public DateTime createTime { get; set; } = DateTime.Now;
        public string fullName { get; set; }
        public DateTime birthDate { get; set; }
        public Gender gender { get; set; }
    }
}
