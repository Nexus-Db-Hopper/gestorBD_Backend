using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using nexusDB.Application.Interfaces.Security;


namespace nexusDB.Infrastructure.Services.Security;

public class AesEncryptionService : IAesEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesEncryptionService(IConfiguration config)
    {
        _key = Convert.FromBase64String(config["Encryption:Key"]);
        _iv = Convert.FromBase64String(config["Encryption:IV"]);
    }

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var writer = new StreamWriter(cs))
        {
            writer.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        var buffer = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(buffer);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var reader = new StreamReader(cs);

        return reader.ReadToEnd();
    }
}