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

using Konscious.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;

namespace PasswordManager.Crypto;

public static class Argon2Kdf
{
    public static byte[] DeriveKey(ReadOnlySpan<char> password, byte[] salt)
    {
        var pwdBytes = Encoding.UTF8.GetBytes(password.ToArray());

        try
        {
            using var argon2 = new Argon2id(pwdBytes)
            {
                Salt = salt,
                Iterations = 4,
                MemorySize = 256 * 1024,
                DegreeOfParallelism = Environment.ProcessorCount
            };

            return argon2.GetBytes(32);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pwdBytes);
        }
    }
}
