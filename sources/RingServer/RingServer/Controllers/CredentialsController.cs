using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using Vonage.Messaging;
using Vonage;

namespace RingServer.Controllers
{
    public class CredentialsController : Controller
    {
        private readonly IDataProtector _protector;
        private readonly string publicKeyPath;
        private readonly string privateKeyPath;

        private Vonage.Request.Credentials vonageCredentials;

        public CredentialsController(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector("Keys");
            publicKeyPath = "RingServer/Keys/serverPublicKey.pem";
            privateKeyPath = "RingServer/Keys/serverPrivateKey.pem";
            vonageCredentials = Vonage.Request.Credentials.FromApiKeyAndSecret("dcd0a6fb", "4QJBjH1opdIZg3Xj");
        }

        public class EncryptedRequest
        {
            public string EncryptedData { get; set; }
            public string EncryptedKey { get; set; }
            public string EncryptedIV { get; set; }

            public EncryptedRequest(string encryptedData, string encryptedKey, string encryptedIV)
            {
                EncryptedData = encryptedData;
                EncryptedKey = encryptedKey;
                EncryptedIV = encryptedIV;
            }
        }

        public class DecryptedRequest
        {
            public string PKey { get; set; }
            public string Number { get; set; }
            public string Id { get; set; }

            public DecryptedRequest(string pKey, string number, string id)
            {
                PKey = pKey;
                Number = number;
                Id = id;
            }
        }


        [HttpGet]
        [Route("/api/v1/auth/publickey")]
        public IActionResult GetKeys()
        {

            if (System.IO.File.Exists(publicKeyPath))
            {
                var publicKey = System.IO.File.ReadAllText(publicKeyPath);
                if (publicKey.Length == 0)
                {
                    return BadRequest("Key length = 0");
                }
                else
                {
                    return Ok(publicKey);
                }
            }
            else
            {
                return NotFound("Public key not found");
            }
        }

        [HttpPost]
        [Route("/api/v1/auth/signin")]
        public async Task<IActionResult> SignIn()
        {
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var requestBody = await reader.ReadToEndAsync();
                if (requestBody == null)
                {
                    return BadRequest("Outer invalid request payload");
                }
                else
                {
                    EncryptedRequest request = JsonConvert.DeserializeObject<EncryptedRequest>(requestBody);

                    // da gestire connessione con db
                    string protectedPrivateKeyPem = System.IO.File.ReadAllText(privateKeyPath);
                    string privateKeyPem = _protector.Unprotect(protectedPrivateKeyPem);

                    using RSA rsa = RSA.Create();
                    rsa.ImportFromPem(privateKeyPem.ToCharArray());

                    if (request != null)
                    {
                        byte[] key = rsa.Decrypt(Convert.FromBase64String(request.EncryptedKey), RSAEncryptionPadding.Pkcs1);
                        byte[] iv = rsa.Decrypt(Convert.FromBase64String(request.EncryptedIV), RSAEncryptionPadding.Pkcs1);

                        byte[] encryptedData = Convert.FromBase64String(request.EncryptedData);

                        using Aes aes = Aes.Create();
                        aes.Key = key;
                        aes.IV = iv;

                        ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                        using (MemoryStream msDecrypt = new MemoryStream(encryptedData))
                        {
                            using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                            {
                                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                                {
                                    string plaintext = srDecrypt.ReadToEnd();
                                    var userInfo = JsonConvert.DeserializeObject<DecryptedRequest>(plaintext);
                                    return Ok(userInfo);
                                }
                            }
                        }
                    }
                    else
                    {
                        return BadRequest("Inner invalid request payload");
                    }
                }
            }
        }

        [HttpPost]
        [Route("/api/v1/auth/sendcode")]
        public async Task<IActionResult> SendSMS() {
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                string requestBody = await reader.ReadToEndAsync();

                if (requestBody == null)
                {
                    return BadRequest("Invalid request payload");
                }
                else
                {
                    try
                    {
                        var to = JsonConvert.DeserializeObject<string>(requestBody);
                        string verificationCode = new Random().Next(10000000, 99999999).ToString();

                        HttpContext.Session.SetString("VerificationCode", verificationCode);
                        string message = "Your verification code is: " + verificationCode;
                        
                        var vonageClient = new VonageClient(vonageCredentials);
                        var smsResponse = vonageClient.SmsClient.SendAnSms(new SendSmsRequest
                        {
                            To = to,
                            From = "Ring",
                            Text = message
                        });

                        return Ok("SMS sent");
                    }
                    catch (Exception e)
                    {
                        return BadRequest("SMS not sent: " + e.Message);
                    }
                }
            }
        }

        [HttpPost]
        [Route("/api/v1/auth/verifycode")]
        public async Task<IActionResult>VerifyCode()
        {
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var requestBody = await reader.ReadToEndAsync();
                if (requestBody == null)
                {
                    return BadRequest("Invalid request payload");
                }
                else
                {
                    var code = JsonConvert.DeserializeObject<string>(requestBody);
                    string verificationCode = HttpContext.Session.GetString("VerificationCode");
                    if (code == verificationCode)
                    {
                        return Ok("Code verified");
                    }
                    else
                    {
                        return BadRequest("Invalid code");
                    }
                }
            }
        }
    }
}
