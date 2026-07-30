using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CefClient.Common
{
    public class Aes128
    {


        static readonly string RESET = "\u001b[0m";
        static readonly string RED = "\u001b[31m";
        static readonly string key = "279cac9ad46c7bd1";


        public static void Test(string[] args)
        {

            List<string> testData = new()
        {
            "e5e263b05872278eee7bd3c8397694ce", "158",
            "66a7063944831333ed5633d04a885c62", "2158",
            "9b25a57896c58290d57019d2fc9c9dde", "3158",
            "d5d7b9b60c0ad0d2d95e5d0fbf2af004", "4158",
            "b2fb4d6f5a6a1f108a6fe927c0e0c609", "12301",
            "ce69816a138e6eeccc15052b35a8be3a", "51",
            "8705eba23ad0ccfb54652b37113b249b", "5601",
        };


      


            for (int i = 0; i < testData.Count; i += 2)
            {
                string cipherExpect = testData[i];
                string price = testData[i + 1];


                string encrypt = AesEncrypt(price, key);


                if (encrypt == cipherExpect)
                {
                    Console.WriteLine(
                        $"{RESET}加密成功 ：{encrypt}");
                }
                else
                {
                    Console.WriteLine(
                        $"{RED}加密失败 ：{encrypt}");
                }


                string decrypt = AesDecrypt(encrypt, key);


                if (decrypt == price)
                {
                    Console.WriteLine(
                        $"{RESET}解密成功 ：{decrypt}");
                }
                else
                {
                    Console.WriteLine(
                        $"{RED}解密失败 ：{decrypt}");
                }
            }
        }


        /// <summary>
        /// AES-128 ECB PKCS7 Hex
        /// </summary>
        public static string AesEncrypt(string plainText, string key)
        {

            using Aes aes = Aes.Create();


            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;


            using ICryptoTransform encryptor =
                aes.CreateEncryptor();


            byte[] input =
                Encoding.UTF8.GetBytes(plainText);


            byte[] output =
                encryptor.TransformFinalBlock(
                    input,
                    0,
                    input.Length);


            return Convert.ToHexString(output)
                .ToLowerInvariant();
        }



        /// <summary>
        /// AES-128 ECB PKCS7 Hex
        /// </summary>
        public static string AesDecrypt(string hexCipher, string key)
        {

            using Aes aes = Aes.Create();


            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;


            byte[] cipher =
                Convert.FromHexString(hexCipher);


            using ICryptoTransform decryptor =
                aes.CreateDecryptor();


            byte[] output =
                decryptor.TransformFinalBlock(
                    cipher,
                    0,
                    cipher.Length);


            return Encoding.UTF8.GetString(output);
        }
    }
}
