using System.IO;

namespace PasswordManager.Vault;

public static class VaultFile
{
    private const uint Magic = 0x53504D56; // "V M P S"
    private const byte Version = 1;

    public static void Write(
        string path,
        byte[] salt,
        byte[] nonce,
        byte[] ciphertext,
        byte[] tag)
    {
        using var fs = File.Create(path);
        using var bw = new BinaryWriter(fs);

        bw.Write(Magic);
        bw.Write(Version);
        bw.Write(salt.Length);
        bw.Write(salt);
        bw.Write(nonce);
        bw.Write(ciphertext.Length);
        bw.Write(ciphertext);
        bw.Write(tag);
    }
}
