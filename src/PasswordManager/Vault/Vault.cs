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

    public static Vault Load(ReadOnlySpan<char> password, string path)
    {
        var fileData = VaultFile.Read(path);
        var masterKey = Argon2Kdf.DeriveKey(password, fileData.Salt);

        var plaintext = AesGcmCrypto.Decrypt(
            fileData.Ciphertext,
            masterKey,
            fileData.Nonce,
            fileData.Tag);

        var entries = JsonSerializer.Deserialize<List<VaultEntry>>(plaintext) ?? new();
        SecureMemory.Wipe(plaintext);

        var vault = new Vault(fileData.Salt, masterKey);
        vault._entries.AddRange(entries);
        return vault;
    }

    public void AddEntry(string site, string username, string password)
        => _entries.Add(new VaultEntry(site, username, password));

    public bool UpdateEntry(string site, string username, string password)
    {
        var index = _entries.FindIndex(e => e.Site.Equals(site, StringComparison.OrdinalIgnoreCase));
        if (index < 0) return false;

        _entries[index] = new VaultEntry(site, username, password);
        return true;
    }

    public bool DeleteEntry(string site)
    {
        var index = _entries.FindIndex(e => e.Site.Equals(site, StringComparison.OrdinalIgnoreCase));
        if (index < 0) return false;

        _entries.RemoveAt(index);
        return true;
    }

    public IEnumerable<VaultEntry> ListEntries() => _entries.AsReadOnly();

    public VaultEntry? FindEntry(string site)
        => _entries.FirstOrDefault(e => e.Site.Equals(site, StringComparison.OrdinalIgnoreCase));

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
