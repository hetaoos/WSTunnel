using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WSTunnel
{
    /// <summary>
    /// 连接参数
    /// </summary>
    public class TunnelParam
    {
        /// <summary>
        /// 参数加密密码
        /// </summary>
        [JsonIgnore]
        public static byte[] param_key = Encoding.UTF8.GetBytes("x6nkUPpSyAVxCTWW");

#if __WSCLIENT__

        /// <summary>
        /// 服务器地址
        /// </summary>
        [JsonIgnore]
        public static Uri server { get; set; }

        /// <summary>
        /// 主机名
        /// </summary>
        [JsonIgnore]
        public IPAddress bind_host { get; set; } = IPAddress.Any;

        /// <summary>
        /// 端口
        /// </summary>
        [JsonIgnore]
        public int bind_port { get; set; }

#endif

        /// <summary>
        /// 时间戳，服务端会校验该时间，如果相差超过2分钟，将关闭
        /// </summary>
        public DateTime time { get; set; }

        /// <summary>
        /// 目标主机名
        /// </summary>
        public string host { get; set; }

        /// <summary>
        /// 目标端口
        /// </summary>
        public int port { get; set; }

        /// <summary>
        /// 密钥，随机生成
        /// </summary>
        public string key { get; set; }

        private Aes aes;
        private ICryptoTransform decryptor;
        private ICryptoTransform encryptor;

        public string ToAccessToken()
        {
            var json = System.Text.Json.JsonSerializer.Serialize(this);
            var bytes = Encoding.UTF8.GetBytes(json);
            using var aes = Aes.Create();
            aes.Key = param_key;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;
            using var cTransform = aes.CreateEncryptor();
            var bytes2 = cTransform.TransformFinalBlock(bytes, 0, bytes.Length);
            return $"Bearer {Convert.ToBase64String(bytes2)}";
        }

        private const string key_chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~";
        private static Random rnd = new Random();

        /// <summary>
        /// 生成随机加密密钥
        /// </summary>
        /// <returns></returns>
        public static string GetRandomKey()
        {
            int len = key_chars.Length;
            lock (rnd)
                return new string(Enumerable.Range(0, 16).Select(o => key_chars[rnd.Next(len)]).ToArray());
        }

        public static TunnelParam? ParseAccessToken(string access_token)
        {
            if (string.IsNullOrWhiteSpace(access_token))
                return null;

            var bytes = Convert.FromBase64String(access_token.Split(' ', StringSplitOptions.RemoveEmptyEntries).Last());
            using var aes = Aes.Create();
            aes.Key = param_key;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;
            using var decryptor = aes.CreateDecryptor();
            var bytes2 = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
            var json = Encoding.UTF8.GetString(bytes2);
            return JsonSerializer.Deserialize<TunnelParam>(json);
        }

        /// <summary>
        /// 初始化加密服务
        /// </summary>
        public void InitializeEncryptionService()
        {
            aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;
            decryptor = aes.CreateDecryptor();
            encryptor = aes.CreateEncryptor();
        }

        /// <summary>
        /// 销毁加密服务
        /// </summary>
        public void DestroyEncryptionService()
        {
            decryptor?.Dispose();
            encryptor?.Dispose();
            aes?.Dispose();
        }

        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="inputBuffer"></param>
        /// <param name="inputOffset"></param>
        /// <param name="inputCount"></param>
        /// <returns></returns>
        public byte[]? Encrypt(byte[] inputBuffer, int inputOffset, int inputCount)
            => encryptor?.TransformFinalBlock(inputBuffer, inputOffset, inputCount);

        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="inputBuffer"></param>
        /// <param name="inputOffset"></param>
        /// <param name="inputCount"></param>
        /// <returns></returns>
        public byte[]? Decrypt(byte[] inputBuffer, int inputOffset, int inputCount)
            => decryptor?.TransformFinalBlock(inputBuffer, inputOffset, inputCount);
    }
}