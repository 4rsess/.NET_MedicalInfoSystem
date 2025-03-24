using System.ComponentModel.DataAnnotations;

namespace MedicalInfoSystem.Models
{
    public class TokenResponseModel
    {
        [MinLength(1)]
        public string token { get; set; }

        public TokenResponseModel(string Token)
        {
            token = Token;
        }
    }
}
