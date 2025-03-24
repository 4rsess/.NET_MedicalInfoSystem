namespace MedicalInfoSystem.Models
{
    public class TokenBlackList
    {
        private static List<string> deactivatedTokens = new List<string>();

        public static bool IsTokenDeactivated(string token)
        {
            return deactivatedTokens.Contains(token);
        }

        public static void DeactivateToken(string token)
        {
            if (!deactivatedTokens.Contains(token))
            {
                deactivatedTokens.Add(token);
            }
        }
    }
}
