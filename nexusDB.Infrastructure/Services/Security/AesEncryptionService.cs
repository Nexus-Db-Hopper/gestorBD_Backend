using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using nexusDB.Application.Interfaces.Security;


namespace nexusDB.Infrastructure.Services.Security;

public class AesEncryptionService : IAesEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;
    
    // Agregar al appsettings un "Encrypt":{"Key" = "Clave", "IV" : "clave de Iv")
    // Ambas tienen que ser de 64 bits y la key es de 32 caracteres y la IV de 16
    public AesEncryptionService(IConfiguration config)
    {
        _key = Convert.FromBase64String(config["Encryption:Key"]);
        _iv = Convert.FromBase64String(config["Encryption:IV"]);
    }

    
    // Este metodo permite encriptar la contraseña de la instancia para poder mantenerla segura
    // No se puede hacer con bcrypt pues despues se tiene que desencriptar para leerla
    
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

    //Esto desencripta la clave usando las mismas claves
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