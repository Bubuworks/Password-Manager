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


using System.IO;

namespace PasswordManager.Vault;

public record VaultFileData(
    byte[] Salt,
    byte[] Nonce,
    byte[] Ciphertext,
    byte[] Tag
);

public static class VaultFile
{
    private const uint Magic = 0x53504D56; // "SPMV"
    private const byte Version = 1;

    public static void Write(string path, byte[] salt, byte[] nonce, byte[] ciphertext, byte[] tag)
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

    public static VaultFileData Read(string path)
    {
        using var fs = File.OpenRead(path);
        using var br = new BinaryReader(fs);

        if (br.ReadUInt32() != Magic)
            throw new InvalidDataException("Invalid vault file format.");

        if (br.ReadByte() != Version)
            throw new InvalidDataException("Unsupported vault version.");

        var saltLength = br.ReadInt32();
        var salt = br.ReadBytes(saltLength);

        var nonce = br.ReadBytes(12);

        var ciphertextLength = br.ReadInt32();
        var ciphertext = br.ReadBytes(ciphertextLength);

        var tag = br.ReadBytes(16);

        return new VaultFileData(salt, nonce, ciphertext, tag);
    }
}
