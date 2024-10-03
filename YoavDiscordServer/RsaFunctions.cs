using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace YoavDiscordServer
{
    public class RsaFunctions
    {
        public static RSAParameters PublicKey;  

        public static string Encrypt(string plainText)
        {
            byte[] encrypted;
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(PublicKey);
                byte[] dataToEncrypt = Encoding.UTF8.GetBytes(plainText);
                encrypted = rsa.Encrypt(dataToEncrypt, true);
            }
            return Convert.ToBase64String(encrypted);
        }

       


    }
}
