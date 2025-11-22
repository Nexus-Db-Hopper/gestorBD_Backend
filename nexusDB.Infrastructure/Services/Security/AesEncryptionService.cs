using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using nexusDB.Application.Interfaces.Security;

namespace nexusDB.Infrastructure.Services.Security;

public class AesEncryptionService : IAesEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesEncryptionService(IConfiguration configuration)
    {
        // CORREGIDO: Leer de la sección "Encryption" para coincidir con appsettings.json
        var key = configuration["Encryption:Key"];
        var iv = configuration["Encryption:IV"];

        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(iv))
        {
            throw new ArgumentException("AES Key or IV is not configured in appsettings.json under the 'Encryption' section.");
        }

        _key = Convert.FromBase64String(key);
        _iv = Convert.FromBase64String(iv);
    }

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            using var sw = new StreamWriter(cs);
            sw.Write(plainText);
        }
        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

        using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }
}
