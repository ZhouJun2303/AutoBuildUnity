using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Scripts_AOT.Utility
{
    public class EncryptHelper
    {

        #region AES加密
        public const string DllHLWD = "MmojZSqO7JZYDduC";
        public const string JsonHLWD = "3gJOdtu2wqcc4muO";
        //默认密钥向量 
        public const string AESIV = "Td/Py5yxdyGy3Srt";


        /// <summary>
        /// AES加密算法
        /// </summary>
        /// <param name="plainText">明文字符串</param>
        /// <param name="strKey">密钥</param>
        /// <returns>返回加密后的密文字节数组</returns>
        public static byte[] AESEncrypt(byte[] inputByteArray, string strKey)
        {
            //分组加密算法
            SymmetricAlgorithm des = Rijndael.Create();
            des.KeySize = 128;
            des.Key = Encoding.UTF8.GetBytes(strKey);
            des.IV = Encoding.UTF8.GetBytes(AESIV);
            des.Mode = CipherMode.CBC;
            des.Padding = PaddingMode.PKCS7;
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            byte[] cipherBytes = ms.ToArray();//得到加密后的字节数组
            cs.Close();
            ms.Close();
            return cipherBytes;
        }

        public static byte[] AESEncrypt(string plainText, string strKey)
        {
            byte[] inputByteArray = Encoding.UTF8.GetBytes(plainText);
            return AESEncrypt(inputByteArray, strKey);
        }

        /// <summary>
        /// AES解密
        /// </summary>
        /// <param name="cipherText">密文字节数组</param>
        /// <param name="strKey">密钥</param>
        /// <returns>返回解密后的字符串</returns>
        public static byte[] AESDecrypt(byte[] cipherText, string strKey)
        {
            //SymmetricAlgorithm des = Rijndael.Create();
            //des.Key = Encoding.UTF8.GetBytes(strKey);
            //des.IV = Encoding.UTF8.GetBytes("Td/Py5yxdyGy3Srt");
            //des.Mode = CipherMode.CBC;
            //des.Padding = PaddingMode.PKCS7;
            //byte[] decryptBytes = new byte[cipherText.Length];
            //MemoryStream ms = new MemoryStream(cipherText);
            //CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Read);
            //cs.Read(decryptBytes, 0, decryptBytes.Length);
            //cs.Close();
            //ms.Close();
            //return decryptBytes;

            byte[] toEncryptArray = cipherText;
            RijndaelManaged rm = new RijndaelManaged
            {
                KeySize = 128,
                Key = Encoding.UTF8.GetBytes(strKey),
                IV = Encoding.UTF8.GetBytes(AESIV),
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            };

            ICryptoTransform cTransform = rm.CreateDecryptor();
            Byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return resultArray;
        }
        #endregion
    }
}
