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


using SecurePasswordManager.Vault;

const string VaultPath = "vault.bin";

Console.Write("Master password: ");
var password = ReadPassword();

Vault vault;

try
{
    if (File.Exists(VaultPath))
    {
        vault = Vault.Load(password, VaultPath);
        Console.WriteLine("Vault loaded successfully.");
    }
    else
    {
        vault = Vault.CreateNew(password);
        Console.WriteLine("New vault created.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error opening vault: {ex.Message}");
    return;
}

Console.Write("Site: ");
var site = Console.ReadLine() ?? "";

Console.Write("Username: ");
var username = Console.ReadLine() ?? "";

Console.Write("Password: ");
var entryPassword = ReadPassword();

vault.AddEntry(site, username, new string(entryPassword));

vault.Save(VaultPath);
Console.WriteLine("Vault saved.");

Console.WriteLine("\nVault entries:");
foreach (var entry in vault.ListEntries())
{
    Console.WriteLine($"- {entry.Site} ({entry.Username})");
}

Console.WriteLine("\nDone.");

static char[] ReadPassword()
{
    var buffer = new List<char>();
    ConsoleKeyInfo key;

    while ((key = Console.ReadKey(true)).Key != ConsoleKey.Enter)
    {
        if (key.Key == ConsoleKey.Backspace && buffer.Count > 0)
        {
            buffer.RemoveAt(buffer.Count - 1);
            continue;
        }

        if (!char.IsControl(key.KeyChar))
            buffer.Add(key.KeyChar);
    }

    Console.WriteLine();
    return buffer.ToArray();
}
