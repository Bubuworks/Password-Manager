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

Console.WriteLine("=== Password Manager ===\n");

Console.Write("Master password: ");
var masterPassword = ReadPassword();

Vault vault;
try
{
    if (File.Exists(VaultPath))
    {
        vault = Vault.Load(masterPassword, VaultPath);
        Console.WriteLine("Vault unlocked.\n");
    }
    else
    {
        vault = Vault.CreateNew(masterPassword);
        Console.WriteLine("New vault created.\n");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to open vault: {ex.Message}");
    return;
}

bool running = true;
while (running)
{
    ShowMenu();
    Console.Write("Select option: ");
    var choice = Console.ReadLine();

    switch (choice)
    {
        case "1":
            AddEntry(vault);
            break;
        case "2":
            ListEntries(vault);
            break;
        case "3":
            ViewEntry(vault);
            break;
        case "4":
            vault.Save(VaultPath);
            Console.WriteLine("Vault saved. Goodbye.");
            running = false;
            break;
        default:
            Console.WriteLine("Invalid option.\n");
            break;
    }
}

static void ShowMenu()
{
    Console.WriteLine("""
    1) Add entry
    2) List entries
    3) View entry password
    4) Save & exit
    """);
}

static void AddEntry(Vault vault)
{
    Console.Write("Site: ");
    var site = Console.ReadLine() ?? "";

    Console.Write("Username: ");
    var username = Console.ReadLine() ?? "";

    Console.Write("Password: ");
    var password = ReadPassword();

    vault.AddEntry(site, username, new string(password));
    Console.WriteLine("Entry added.\n");
}

static void ListEntries(Vault vault)
{
    var entries = vault.ListEntries().ToList();

    if (entries.Count == 0)
    {
        Console.WriteLine("Vault is empty.\n");
        return;
    }

    Console.WriteLine("Stored entries:");
    foreach (var entry in entries)
    {
        Console.WriteLine($"- {entry.Site} ({entry.Username})");
    }
    Console.WriteLine();
}

static void ViewEntry(Vault vault)
{
    Console.Write("Site to view: ");
    var site = Console.ReadLine() ?? "";

    var entry = vault.FindEntry(site);
    if (entry == null)
    {
        Console.WriteLine("Entry not found.\n");
        return;
    }

    Console.WriteLine($"Username: {entry.Username}");
    Console.WriteLine($"Password: {entry.Password}\n");
}

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
