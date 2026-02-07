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

using System.Security.Cryptography;

namespace PasswordManager.Crypto;

public static class AesGcmCrypto
{
    public static byte[] Encrypt(
        byte[] plaintext,
        byte[] key,
        out byte[] nonce,
        out byte[] tag)
    {
        nonce = RandomNumberGenerator.GetBytes(12);
        tag = new byte[16];
        var ciphertext = new byte[plaintext.Length];

        using var aes = new AesGcm(key);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        return ciphertext;
    }

    public static byte[] Decrypt(
        byte[] ciphertext,
        byte[] key,
        byte[] nonce,
        byte[] tag)
    {
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }

}

