using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace Ventafacil.Desktop;

public sealed class PasswordStore
{
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private readonly string _filePath;

    public PasswordStore()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Ventafacil");
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "security.json");
    }

    public bool HasPassword => File.Exists(_filePath);

    public bool TrySetPassword(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        ValidatePassword(password);
        if (HasPassword)
            return false;

        SavePassword(password, overwrite: false);
        return true;
    }

    public bool TryChangePassword(string currentPassword, string newPassword)
    {
        ArgumentNullException.ThrowIfNull(currentPassword);
        ArgumentNullException.ThrowIfNull(newPassword);
        ValidatePassword(newPassword);
        if (!HasPassword || !VerifyPassword(currentPassword))
            return false;

        SavePassword(newPassword, overwrite: true);
        return true;
    }

    private void SavePassword(string password, bool overwrite)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        var entry = new PasswordEntry(Iterations, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
        var temporaryPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(entry));
            File.Move(temporaryPath, _filePath, overwrite);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private static void ValidatePassword(string password)
    {
        if (password.Length < 6)
            throw new ArgumentException("La contraseña debe tener al menos 6 caracteres.", nameof(password));
    }

    public bool VerifyPassword(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        if (!HasPassword)
            return false;

        var entry = JsonSerializer.Deserialize<PasswordEntry>(File.ReadAllText(_filePath))
                    ?? throw new InvalidDataException("El archivo de configuración de contraseña está vacío.");
        if (entry.Iterations < 1_000 || entry.Iterations > 2_000_000)
            throw new InvalidDataException("El archivo de configuración de contraseña contiene datos inválidos.");

        byte[] salt;
        byte[] expectedHash;
        try
        {
            salt = Convert.FromBase64String(entry.Salt);
            expectedHash = Convert.FromBase64String(entry.Hash);
        }
        catch (FormatException exception)
        {
            throw new InvalidDataException("El archivo de configuración de contraseña está dañado.", exception);
        }

        if (salt.Length != SaltSize || expectedHash.Length != HashSize)
            throw new InvalidDataException("El archivo de configuración de contraseña contiene datos inválidos.");

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            entry.Iterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    private sealed record PasswordEntry(int Iterations, string Salt, string Hash);
}
