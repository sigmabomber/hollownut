using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

public static class PlayerSaveSystem
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "player.dat");
    private static readonly SemaphoreSlim fileLock = new SemaphoreSlim(1, 1);

    private static void DeriveKeys(string password, out byte[] aesKey, out byte[] hmacKey)
    {
        byte[] salt = Encoding.UTF8.GetBytes("GameSaltHere");
        using (var kdf = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256))
        {
            byte[] keyMaterial = kdf.GetBytes(64);
            aesKey = new byte[32];
            hmacKey = new byte[32];
            Array.Copy(keyMaterial, 0, aesKey, 0, 32);
            Array.Copy(keyMaterial, 32, hmacKey, 0, 32);
        }
    }

    private static byte[] AesEncrypt(byte[] data, byte[] key, out byte[] iv)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();
            iv = aes.IV;
            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                cs.Write(data, 0, data.Length);
                cs.FlushFinalBlock();
                return ms.ToArray();
            }
        }
    }

    private static byte[] AesDecrypt(byte[] data, byte[] key, byte[] iv)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
            {
                cs.Write(data, 0, data.Length);
                cs.FlushFinalBlock();
                return ms.ToArray();
            }
        }
    }

    private static byte[] ComputeHMAC(byte[] key, byte[] data)
    {
        using (var hmac = new HMACSHA256(key))
        {
            return hmac.ComputeHash(data);
        }
    }

    public static async Task SaveAsync(PlayerData playerData, string password)
    {
        await fileLock.WaitAsync(); // Wait for exclusive access
        try
        {
            // Run heavy operations on background thread
            byte[] fileData = await Task.Run(() =>
            {
                string json = JsonConvert.SerializeObject(playerData, Formatting.Indented);
                byte[] plain = Encoding.UTF8.GetBytes(json);

                DeriveKeys(password, out byte[] aesKey, out byte[] hmacKey);

                byte[] cipher = AesEncrypt(plain, aesKey, out byte[] iv);

                byte[] ivAndCipher = new byte[iv.Length + cipher.Length];
                Buffer.BlockCopy(iv, 0, ivAndCipher, 0, iv.Length);
                Buffer.BlockCopy(cipher, 0, ivAndCipher, iv.Length, cipher.Length);

                byte[] hmac = ComputeHMAC(hmacKey, ivAndCipher);

                using (MemoryStream ms = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(ms))
                {
                    writer.Write(iv.Length);
                    writer.Write(iv);
                    writer.Write(cipher.Length);
                    writer.Write(cipher);
                    writer.Write(hmac.Length);
                    writer.Write(hmac);
                    return ms.ToArray();
                }
            });

            // Write to file asynchronously
            await File.WriteAllBytesAsync(SavePath, fileData);

        }
        catch (Exception e)
        {
            Debug.LogError("Failed to save player data: " + e.Message);
        }
        finally
        {
            fileLock.Release(); // Always release the lock
        }
    }

    public static async Task<PlayerData> LoadAsync(string password)
    {
        if (!File.Exists(SavePath))
        {
            Debug.LogWarning("No save file found at: " + SavePath);
            return null;
        }

        await fileLock.WaitAsync(); // Wait for exclusive access
        try
        {
            // Read file asynchronously
            byte[] fileData = await File.ReadAllBytesAsync(SavePath);

            // Decrypt and deserialize on background thread
            PlayerData result = await Task.Run(() =>
            {
                DeriveKeys(password, out byte[] aesKey, out byte[] hmacKey);

                using (MemoryStream ms = new MemoryStream(fileData))
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    int ivLength = reader.ReadInt32();
                    byte[] iv = reader.ReadBytes(ivLength);

                    int cipherLength = reader.ReadInt32();
                    byte[] cipher = reader.ReadBytes(cipherLength);

                    int hmacLength = reader.ReadInt32();
                    byte[] hmac = reader.ReadBytes(hmacLength);

                    // Verify HMAC
                    byte[] ivAndCipher = new byte[iv.Length + cipher.Length];
                    Buffer.BlockCopy(iv, 0, ivAndCipher, 0, iv.Length);
                    Buffer.BlockCopy(cipher, 0, ivAndCipher, iv.Length, cipher.Length);

                    byte[] actualHmac = ComputeHMAC(hmacKey, ivAndCipher);

                    if (hmac.Length != actualHmac.Length)
                    {
                        Debug.LogWarning("HMAC length mismatch - possible tampering");
                        return null;
                    }

                    bool hmacValid = true;
                    for (int i = 0; i < hmac.Length; i++)
                    {
                        if (hmac[i] != actualHmac[i])
                        {
                            hmacValid = false;
                            break;
                        }
                    }

                    if (!hmacValid)
                    {
                        Debug.LogWarning("Save file failed HMAC verification - possible tampering");
                        return null;
                    }

                    byte[] plain = AesDecrypt(cipher, aesKey, iv);
                    string json = Encoding.UTF8.GetString(plain);

                    return JsonConvert.DeserializeObject<PlayerData>(json);
                }
            });

            return result;
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to load player data: " + e.Message);
            return null;
        }
        finally
        {
            fileLock.Release(); // Always release the lock
        }
    }

    // Synchronous versions (for backwards compatibility)
    public static void Save(PlayerData playerData, string password)
    {
        SaveAsync(playerData, password).Wait();
    }

    public static PlayerData Load(string password)
    {
        return LoadAsync(password).Result;
    }
}