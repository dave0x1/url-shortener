using System.Security.Cryptography;
using System.Text;

public class TruncatedHasher
{
    public static string GetTruncatedHash(string input, int length = 7)
    {
        // 1. Compute standard SHA-256
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = SHA256.HashData(inputBytes);
        string fullHexHash = Convert.ToHexString(hashBytes).ToLower();

        // 2. Truncate to the desired number of characters
        return fullHexHash[..length];
    }
}
