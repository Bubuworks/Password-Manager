//                                                         .-'''-.                                     
//                                                        '   _    \                                   
//    /|                  /|                            /   /` '.   \              .                   
//    ||                  ||                    _     _.   |     \  '            .'|                   
//    ||                  ||              /\    \\   //|   '      |  '.-,.--.  .'  |                   
//    ||  __              ||  __          `\\  //\\ // \    \     / / |  .-. |<    |                   
//    ||/'__ '.   _    _  ||/'__ '.   _    _\`//  \'/   `.   ` ..' /  | |  | | |   | ____         _    
//    |:/`  '. ' | '  / | |:/`  '. ' | '  / |\|   |/       '-...-'`   | |  | | |   | \ .'       .' |   
//    ||     | |.' | .' | ||     | |.' | .' | '                       | |  '-  |   |/  .       .   | / 
//    ||\    / '/  | /  | ||\    / '/  | /  |                         | |      |    /\  \    .'.'| |// 
//    |/\'..' /|   `'.  | |/\'..' /|   `'.  |                         | |      |   |  \  \ .'.'.-'  /  
//    '  `'-'` '   .'|  '/'  `'-'` '   .'|  '/                        |_|      '    \  \  \.'   \_.'   
//              `-'  `--'           `-'  `--'                                 '------'  '---'          


using PasswordManager.Crypto;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PasswordManager.Vault;

public class Vault
{
    private readonly List<VaultEntry> _entries = new();
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

    public static Vault Load(ReadOnlySpan<char> password, string path)
    {
        var fileData = VaultFile.Read(path);

        var masterKey = Argon2Kdf.DeriveKey(password, fileData.Salt);

        try
        {
            var plaintext = AesGcmCrypto.Decrypt(
                fileData.Ciphertext,
                masterKey,
                fileData.Nonce,
                fileData.Tag);

            var entries = JsonSerializer.Deserialize<List<VaultEntry>>(plaintext)
                          ?? new List<VaultEntry>();

            var vault = new Vault(fileData.Salt, masterKey);
            vault._entries.AddRange(entries);

            SecureMemory.Wipe(plaintext);
            return vault;
        }
        catch (CryptographicException)
        {
            SecureMemory.Wipe(masterKey);
            throw new InvalidOperationException("Incorrect password or corrupted vault.");
        }
    }

    public IEnumerable<VaultEntry> ListEntries() => _entries.AsReadOnly();

    public VaultEntry? FindEntry(string site)
        => _entries.FirstOrDefault(e => e.Site.Equals(site, StringComparison.OrdinalIgnoreCase));
}

