namespace MedicalInfoSystem.Models
{
    public class ResponseModel
    {
        public string Status { get; set; }
        public string Message { get; set; }

        public ResponseModel(string status, string message)
        {
            Status = status;
            Message = message;
        }
    }
}
