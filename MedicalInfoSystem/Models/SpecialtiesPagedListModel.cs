namespace MedicalInfoSystem.Models
{
    public class SpecialtiesPagedListModel
    {
        public List<SpecialityModel> specialties { get; set; }
        public PageInfoModel pagination { get; set; }
    }
}
