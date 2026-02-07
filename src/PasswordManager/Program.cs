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
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

const string VaultPath = "vault.bin";
const int AutoLockTimeoutMs = 60000;

int ClipboardTimeoutMs = 10000;

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
        try { vault = Vault.Load(masterPassword, VaultPath); }
        catch { Console.WriteLine("Incorrect password."); continue; }
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
        case "3": ViewEntryMasked(vault); break;
        case "4": UpdateEntry(vault); break;
        case "5": DeleteEntry(vault); break;
        case "6": CopyPassword(vault); break;
        case "7": GeneratePasswordMenu(); break;
        case "8": ConfigureClipboardTimeout(); break;
        case "9": UploadVault(VaultPath); break;
        case "10": DownloadVault(VaultPath); vault = Vault.Load(masterPassword, VaultPath); break;
        case "11": vault.Save(VaultPath); running = false; break;
    }
}

static void ShowMenu()
{
    Console.WriteLine($"""
    1) Add entry
    2) List entries
    3) View entry (masked, press Space to reveal)
    4) Update entry
    5) Delete entry
    6) Copy password to clipboard
    7) Generate strong password
    8) Configure clipboard timeout (current: {ClipboardTimeoutMs / 1000}s)
    9) Upload vault to cloud
    10) Download vault from cloud
    11) Save & exit
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

static void ViewEntryMasked(Vault vault)
{
    Console.Write("Site: "); var site = Console.ReadLine() ?? "";
    var entry = vault.FindEntry(site);
    if (entry == null) return;

    Console.WriteLine($"Username: {entry.Username}");
    Console.Write("Password: ");
    MaskedReveal(entry.Password);
}

static void MaskedReveal(string password)
{
    ConsoleKeyInfo key;
    int revealDurationMs = 5000;
    DateTime start = DateTime.Now;

    while ((DateTime.Now - start).TotalMilliseconds < revealDurationMs)
    {
        if (Console.KeyAvailable)
        {
            key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Spacebar)
            {
                Console.Write($"\rPassword: {password}");
            }
        }
        else
        {
            Console.Write($"\rPassword: {new string('*', password.Length)}");
        }
        Thread.Sleep(100);
    }
    Console.WriteLine($"\rPassword: {new string('*', password.Length)}");
}

static void CopyPassword(Vault vault)
{
    Console.Write("Site: "); var site = Console.ReadLine() ?? "";
    var entry = vault.FindEntry(site);
    if (entry == null) return;
    ClipboardService.SetText(entry.Password);
    _ = Task.Run(async () =>
    {
        await Task.Delay(ClipboardTimeoutMs);
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
        await Task.Delay(ClipboardTimeoutMs);
        ClipboardService.SetText(string.Empty);
    });
    Console.WriteLine($"Password copied to clipboard for {ClipboardTimeoutMs / 1000} seconds.");
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

static void ConfigureClipboardTimeout()
{
    Console.Write("Enter clipboard timeout in seconds: ");
    if (int.TryParse(Console.ReadLine(), out int seconds) && seconds > 0)
    {
        ClipboardTimeoutMs = seconds * 1000;
        Console.WriteLine($"Clipboard timeout set to {seconds} seconds.");
    }
    else Console.WriteLine("Invalid input. Timeout unchanged.");
}

static void UploadVault(string path)
{
    Console.Write("Enter cloud endpoint URL for upload: ");
    var url = Console.ReadLine();
    if (string.IsNullOrEmpty(url) || !File.Exists(path)) return;

    using var client = new HttpClient();
    using var content = new MultipartFormDataContent();
    using var fileStream = File.OpenRead(path);
    content.Add(new StreamContent(fileStream), "file", "vault.bin");

    try
    {
        var response = client.PostAsync(url, content).Result;
        Console.WriteLine(response.IsSuccessStatusCode ? "Vault uploaded successfully." : "Upload failed.");
    }
    catch (Exception ex) { Console.WriteLine($"Upload error: {ex.Message}"); }
}

static void DownloadVault(string path)
{
    Console.Write("Enter cloud endpoint URL for download: ");
    var url = Console.ReadLine();
    if (string.IsNullOrEmpty(url)) return;

    using var client = new HttpClient();
    try
    {
        var bytes = client.GetByteArrayAsync(url).Result;
        File.WriteAllBytes(path, bytes);
        Console.WriteLine("Vault downloaded successfully.");
    }
    catch (Exception ex) { Console.WriteLine($"Download error: {ex.Message}"); }
}

static string ReadPassword()
{
    var password = new StringBuilder();
    while (true)
    {
        var keyInfo = Console.ReadKey(intercept: true);
        if (keyInfo.Key == ConsoleKey.Enter)
            break;

        if (keyInfo.Key == ConsoleKey.Backspace)
        {
            if (password.Length > 0)
                password.Remove(password.Length - 1, 1);
        }
        else
        {
            password.Append(keyInfo.KeyChar);
        }
        Console.Write("*");
    }
    Console.WriteLine();
    return password.ToString();
}
