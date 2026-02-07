using PasswordManager.Vault;

Console.Write("Master password: ");
var password = ReadPassword();

var vault = Vault.CreateNew(password);
vault.AddEntry("github.com", "user@example.com", "supersecret");

vault.Save("vault.bin");

Console.WriteLine("Vault saved.");

static char[] ReadPassword()
{
    var pwd = new Stack<char>();
    ConsoleKeyInfo key;

    while ((key = Console.ReadKey(true)).Key != ConsoleKey.Enter)
    {
        if (key.Key == ConsoleKey.Backspace && pwd.Count > 0)
        {
            pwd.Pop();
            continue;
        }
        pwd.Push(key.KeyChar);
    }

    Console.WriteLine();
    return pwd.Reverse().ToArray();
}
