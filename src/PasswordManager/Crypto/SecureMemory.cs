using System.Security.Cryptography;

namespace SecurePasswordManager.Crypto;

public static class SecureMemory
{
    public static void Wipe(byte[] buffer)
    {
        CryptographicOperations.ZeroMemory(buffer);
    }

    public static void Wipe(char[] buffer)
    {
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = '\0';
    }
}