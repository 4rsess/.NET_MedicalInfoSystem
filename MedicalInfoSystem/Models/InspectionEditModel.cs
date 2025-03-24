using System.ComponentModel.DataAnnotations;

namespace MedicalInfoSystem.Models
{
    public class InspectionEditModel
    {
        [MaxLength(5000)]
        public string anamnesis { get; set; }

        [MinLength(1), MaxLength(5000), Required]
        public string complaints { get; set; }

        [MinLength(1), MaxLength(5000), Required]
        public string treatment { get; set; }

        [Required]
        public Conclusion conclusion { get; set; }
        public DateTime? nextVisitDate { get; set; }
        public DateTime? deathDate { get; set; }

        [Required]
        public List<DiagnosisCreateModel> diagnoses { get; set; }
    }
}
