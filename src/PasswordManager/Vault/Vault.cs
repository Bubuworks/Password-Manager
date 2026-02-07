using SecurePasswordManager.Crypto;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SecurePasswordManager.Vault;

public class Vault
{
    private readonly List<VaultEntry> _entries = [];
    private readonly byte[] _salt;
    private readonly byte[] _masterKey;

    private Vault(byte[] salt, byte[] masterKey)
    {
        _salt = salt;
        _masterKey = masterKey;
    }

    public static Vault CreateNew(ReadOnlySpan<char> password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var key = Argon2Kdf.DeriveKey(password, salt);

        return new Vault(salt, key);
    }

    public void AddEntry(string site, string username, string password)
        => _entries.Add(new VaultEntry(site, username, password));

    public void Save(string path)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(_entries);

        var ciphertext = AesGcmCrypto.Encrypt(
            json,
            _masterKey,
            out var nonce,
            out var tag);

        VaultFile.Write(path, _salt, nonce, ciphertext, tag);

        SecureMemory.Wipe(json);
    }
}
