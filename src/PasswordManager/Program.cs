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

using PasswordManager.Vault;
using TextCopy;
using System.Timers;
using System.Security.Cryptography;

const string VaultPath = "vault.bin";
const int AutoLockTimeoutMs = 60000;

Vault vault;
Console.Write("Master password: ");
var masterPassword = ReadPassword();

vault = File.Exists(VaultPath) ? Vault.Load(masterPassword, VaultPath) : Vault.CreateNew(masterPassword);

var inactivityTimer = new Timer(AutoLockTimeoutMs);
inactivityTimer.Elapsed += (_, _) =>
{
    Console.WriteLine("\nVault auto-locked due to inactivity.\n");
    vault = null!;
    ClipboardService.SetText(string.Empty);
};
inactivityTimer.Start();

bool running = true;
while (running)
{
    if (vault == null)
    {
        Console.Write("Vault locked. Re-enter master password: ");
        masterPassword = ReadPassword();
        try
        {
            vault = Vault.Load(masterPassword, VaultPath);
        }
        catch
        {
            Console.WriteLine("Incorrect password.");
            continue;
        }
    }

    ShowMenu();
    Console.Write("Select option: ");
    var choice = Console.ReadLine();
    inactivityTimer.Stop();
    inactivityTimer.Start();

    switch (choice)
    {
        case "1": AddEntry(vault); break;
        case "2": ListEntries(vault); break;
        case "3": ViewEntry(vault); break;
        case "4": UpdateEntry(vault); break;
        case "5": DeleteEntry(vault); break;
        case "6": CopyPassword(vault); break;
        case "7": GeneratePasswordMenu(); break;
        case "8": vault.Save(VaultPath); running = false; break;
    }
}

static void ShowMenu()
{
    Console.WriteLine("""
    1) Add entry
    2) List entries
    3) View entry
    4) Update entry
    5) Delete entry
    6) Copy password to clipboard
    7) Generate strong password
    8) Save & exit
    """);
}

static void AddEntry(Vault vault)
{
    Console.Write("Site: "); var site = Console.ReadLine() ?? "";
    Console.Write("Username: "); var username = Console.ReadLine() ?? "";
    Console.Write("Password: "); var password = ReadPassword();
    vault.AddEntry(site, username, new string(password));
}

static void UpdateEntry(Vault vault)
{
    Console.Write("Site to update: "); var site = Console.ReadLine() ?? "";
    Console.Write("New username: "); var username = Console.ReadLine() ?? "";
    Console.Write("New password: "); var password = ReadPassword();
    vault.UpdateEntry(site, username, new string(password));
}

static void DeleteEntry(Vault vault)
{
    Console.Write("Site to delete: "); var site = Console.ReadLine() ?? "";
    vault.DeleteEntry(site);
}

static void ListEntries(Vault vault)
{
    foreach (var e in vault.ListEntries())
        Console.WriteLine($"{e.Site} ({e.Username})");
}

static void ViewEntry(Vault vault)
{
    Console.Write("Site: "); var site = Console.ReadLine() ?? "";
    var entry = vault.FindEntry(site);
    if (entry != null)
        Console.WriteLine($"{entry.Username} / {entry.Password}");
}

static void CopyPassword(Vault vault)
{
    Console.Write("Site: "); var site = Console.ReadLine() ?? "";
    var entry = vault.FindEntry(site);
    if (entry == null) return;
    ClipboardService.SetText(entry.Password);
    _ = Task.Run(async () =>
    {
        await Task.Delay(10000);
        ClipboardService.SetText(string.Empty);
    });
}

static void GeneratePasswordMenu()
{
    Console.Write("Length: "); int.TryParse(Console.ReadLine(), out int length); 
    Console.Write("Include uppercase? (y/n): "); bool uc = (Console.ReadLine() ?? "").ToLower() == "y";
    Console.Write("Include digits? (y/n): "); bool digits = (Console.ReadLine() ?? "").ToLower() == "y";
    Console.Write("Include symbols? (y/n): "); bool symbols = (Console.ReadLine() ?? "").ToLower() == "y";

    string password = GeneratePassword(length, uc, digits, symbols);
    Console.WriteLine($"Generated: {password}");
    ClipboardService.SetText(password);
    _ = Task.Run(async () =>
    {
        await Task.Delay(10000);
        ClipboardService.SetText(string.Empty);
    });
    Console.WriteLine("Password copied to clipboard for 10 seconds.");
}

static string GeneratePassword(int length, bool uppercase, bool digits, bool symbols)
{
    const string lc = "abcdefghijklmnopqrstuvwxyz";
    const string uc = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    const string dg = "0123456789";
    const string sym = "!@#$%^&*()-_=+[]{};:,.<>?";

    string charset = lc + (uppercase ? uc : "") + (digits ? dg : "") + (symbols ? sym : "");
    if (string.IsNullOrEmpty(charset)) charset = lc;

    var bytes = RandomNumberGenerator.GetBytes(length);
    var result = new char[length];
    for (int i = 0; i < length; i++)
        result[i] = charset[bytes[i] % charset.Length];
    return new string(result);
}

static char[] ReadPassword()
{
    var buffer = new List<char>();
    ConsoleKeyInfo key;
    while ((key = Console.ReadKey(true)).Key != ConsoleKey.Enter)
    {
        if (key.Key == ConsoleKey.Backspace && buffer.Count > 0)
            buffer.RemoveAt(buffer.Count - 1);
        else if (!char.IsControl(key.KeyChar))
            buffer.Add(key.KeyChar);
    }
    Console.WriteLine();
    return buffer.ToArray();
}
