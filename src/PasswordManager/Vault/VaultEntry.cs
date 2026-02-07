namespace PasswordManager.Vault;

public record VaultEntry(
    string Site,
    string Username,
    string Password
);
