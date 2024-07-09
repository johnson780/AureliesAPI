using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Aurelius_API
{
    public class JwtToken
    {
        private static readonly string secret = GenerateRandomSecretKey(32);
        private static readonly string iv = GenerateRandomIV();

        public static string GenerateJwtToken(string companyID, string userID)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim("CompanyID", companyID),
                    new Claim("UserID", userID),
                    //new Claim("Password", password)
                }),
                Expires = DateTime.UtcNow.AddMinutes(60),
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        public static bool IsTokenExpired(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;

            if (jwtToken == null)
                return true;

            var expirationTimeUnix = long.Parse(jwtToken.Claims.FirstOrDefault(claim => claim.Type == "exp")?.Value);
            var expirationTime = DateTimeOffset.FromUnixTimeSeconds(expirationTimeUnix).DateTime;

            return expirationTime <= DateTime.UtcNow;
        }

        public static bool ValidateJwtToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret));

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);
                return validatedToken != null;
            }
            catch
            {
                return false;
            }
        }

        public static string GetCompanyIdFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;

            if (jwtToken == null)
                return null;

            return jwtToken.Claims.FirstOrDefault(claim => claim.Type == "CompanyID")?.Value;
        }

        public static string GetUserIdFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;

            if (jwtToken == null)
                return null;

            return jwtToken.Claims.FirstOrDefault(claim => claim.Type == "UserID")?.Value;
        }

        //public static string GetPasswordFromToken(string token)
        //{
        //    var tokenHandler = new JwtSecurityTokenHandler();
        //    var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;

        //    if (jwtToken == null)
        //        return null;

        //    return jwtToken.Claims.FirstOrDefault(claim => claim.Type == "Password")?.Value;
        //}

        private static string GenerateRandomSecretKey(int length)
        {
            const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            StringBuilder stringBuilder = new StringBuilder();
            Random random = new Random();

            for (int i = 0; i < length; i++)
            {
                stringBuilder.Append(validChars[random.Next(validChars.Length)]);
            }

            return stringBuilder.ToString();
        }

        private static string GenerateRandomIV()
        {
            using (Aes aes = Aes.Create())
            {
                aes.GenerateIV();
                return Convert.ToBase64String(aes.IV);
            }
        }

        public static class AesEncryption
        {
            public static string EncryptToken(string token)
            {
                byte[] keyBytes = Encoding.UTF8.GetBytes(secret);
                byte[] ivBytes = Convert.FromBase64String(iv);

                byte[] tokenBytes = Encoding.UTF8.GetBytes(token);

                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = keyBytes;
                    aesAlg.IV = ivBytes;
                    aesAlg.Padding = PaddingMode.PKCS7;

                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            csEncrypt.Write(tokenBytes, 0, tokenBytes.Length);
                            csEncrypt.FlushFinalBlock();
                        }

                        byte[] encryptedBytes = msEncrypt.ToArray();
                        return Convert.ToBase64String(encryptedBytes);
                    }
                }
            }

            public static string DecryptToken(string encryptedToken)
            {

                byte[] keyBytes = Encoding.UTF8.GetBytes(secret);
                byte[] ivBytes = Convert.FromBase64String(iv);
                byte[] encryptedBytes = Convert.FromBase64String(encryptedToken);

                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = keyBytes;
                    aesAlg.IV = ivBytes;
                    aesAlg.Padding = PaddingMode.PKCS7;

                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    using (MemoryStream msDecrypt = new MemoryStream(encryptedBytes))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                return srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
            }
        }
    }
}