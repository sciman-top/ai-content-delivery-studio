using System.Security.Cryptography;
using System.Text;
using System.Runtime.Versioning;

namespace ImageSeriesStudio.Infrastructure.OpenAI;

[SupportedOSPlatform("windows")]
public sealed class DpapiOpenAiSecretStore : IWritableOpenAiSecretStore
{
    private static readonly byte[] Entropy = SHA256.HashData(
        Encoding.UTF8.GetBytes("ImageSeriesStudio.OpenAI.SecretStore.v1"));

    private readonly string _secretsDirectory;
    private readonly DataProtectionScope _scope;

    public DpapiOpenAiSecretStore(
        string? secretsDirectory = null,
        DataProtectionScope scope = DataProtectionScope.CurrentUser)
    {
        _secretsDirectory = string.IsNullOrWhiteSpace(secretsDirectory)
            ? DefaultSecretsDirectory
            : secretsDirectory;
        _scope = scope;
    }

    public static string DefaultSecretsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ImageSeriesStudio",
        "secrets",
        "openai");

    public async Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = GetSecretPath(secretName);
        if (!File.Exists(path))
        {
            return null;
        }

        var protectedBytes = await File.ReadAllBytesAsync(path, cancellationToken);
        var plainBytes = ProtectedData.Unprotect(protectedBytes, Entropy, _scope);
        return Encoding.UTF8.GetString(plainBytes);
    }

    public async Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(secretValue))
        {
            throw new ArgumentException("Secret value cannot be empty.", nameof(secretValue));
        }

        Directory.CreateDirectory(_secretsDirectory);
        var path = GetSecretPath(secretName);
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        var plainBytes = Encoding.UTF8.GetBytes(secretValue);
        var protectedBytes = ProtectedData.Protect(plainBytes, Entropy, _scope);

        try
        {
            await File.WriteAllBytesAsync(temporaryPath, protectedBytes, cancellationToken);
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    public Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = GetSecretPath(secretName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string GetSecretPath(string secretName)
    {
        var normalizedName = NormalizeSecretName(secretName);
        var nameHash = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedName));
        var fileName = Convert.ToHexString(nameHash).ToLowerInvariant() + ".dpapi";
        return Path.Combine(_secretsDirectory, fileName);
    }

    private static string NormalizeSecretName(string secretName)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be empty.", nameof(secretName));
        }

        return secretName.Trim();
    }
}
