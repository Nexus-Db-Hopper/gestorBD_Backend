namespace nexusDB.Application.Interfaces.Security;

public interface IAesEncryptionService
{
    public string Encrypt(string plainText);
    public string Decrypt(string cipherText);
}