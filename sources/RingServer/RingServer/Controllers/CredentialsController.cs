using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using Vonage.Messaging;
using Vonage;
using Microsoft.EntityFrameworkCore;

namespace RingServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CredentialsController : Controller
    {
        private readonly IDataProtector _publicProtector;
        private readonly IDataProtector _privateProtector;

        private readonly string _config;
        private AppSettingsUpdater _updater;
        private Vonage.Request.Credentials vonageCredentials;

        private readonly RingDBContext _dbContext;

        public CredentialsController(IDataProtectionProvider provider, RingDBContext dbContext)
        {
            _publicProtector = provider.CreateProtector("PublicKeyProtector");
            _privateProtector = provider.CreateProtector("PrivateKeyProtector");

            _config = "appsettings.json";
            _updater = new AppSettingsUpdater(_config);
            string vonageKey = _updater.GetSetting("vonageKey");
            string vonageSecret = _updater.GetSetting("vonageSecret");
            vonageCredentials = Vonage.Request.Credentials.FromApiKeyAndSecret(vonageKey, vonageSecret);

            _dbContext = dbContext;
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

        public class VerifyRequest
        {
            public string Code { get; set; }
            public string Number { get; set; }

            public VerifyRequest(string code, string number)
            {
                Code = code;
                Number = number;
            }
        }


        [HttpGet]
        [Route("/api/v1/auth/publickey")]
        public IActionResult GetKeys()
        {
            string publicKey = _updater.GetSetting("publicKey");
            if (publicKey != string.Empty)
            {
                
                if (publicKey.Length == 0)
                {
                    return BadRequest("Key length = 0");
                }
                else
                {
                    string unprotectedPublicKey = _publicProtector.Unprotect(publicKey);
                    return Ok(unprotectedPublicKey);
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
                EncryptedRequest request = JsonConvert.DeserializeObject<EncryptedRequest>(requestBody);

                string protectedPrivateKeyPem = _updater.GetSetting("privateKey");
                string privateKeyPem = _privateProtector.Unprotect(protectedPrivateKeyPem);

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
                                if (userInfo != null)
                                {
                                    string verificationCode = new Random().Next(10000000, 99999999).ToString();
                                    if (_dbContext.Users.All(u => u.phoneNumber != userInfo.Number))
                                    {
                                        _dbContext.Users.Add(new User
                                        {
                                            phoneNumber = userInfo.Number,
                                            deviceId = userInfo.Id,
                                            verificationCode = string.Empty,
                                            verificationExpire = DateTime.MinValue,
                                            lastLogin = DateTime.MinValue,
                                            publicKey = userInfo.PKey
                                        });
                                    }
                                    else
                                    {
                                        _dbContext.Users.Where(u => u.phoneNumber == userInfo.Number).First().verificationCode = string.Empty;
                                        _dbContext.Users.Where(u => u.phoneNumber == userInfo.Number).First().lastLogin = DateTime.MinValue;
                                    }

                                    try
                                    {
                                        await _dbContext.SaveChangesAsync();
                                    }
                                    catch (Exception ex)
                                    {
                                        return StatusCode(500, "An error occurred while saving the user to the database");
                                    }
                                    return Ok();
                                }
                                else
                                {
                                    return BadRequest("Invalid request payload");
                                }
                            }
                        }
                    }
                }
                else
                {
                    return BadRequest("Invalid request payload");
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
                        _dbContext.Users.Where(u => u.phoneNumber == to).First().verificationCode = verificationCode;
                        _dbContext.Users.Where(u => u.phoneNumber == to).First().verificationExpire = DateTime.Now.AddMinutes(5);
                        try
                        {
                            await _dbContext.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            return StatusCode(500, "An error occurred while saving the user to the database");
                        }

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
                    try
                    {
                        var info = JsonConvert.DeserializeObject<VerifyRequest>(requestBody);
                        if (info == null)
                        {
                            return BadRequest("Invalid request payload");
                        }

                        string code = info.Code;
                        string number = info.Number;

                        string verificationCode = _dbContext.Users.Where(u => u.phoneNumber == number).Select(u => u.verificationCode).First();
                        DateTime verificationExpire = _dbContext.Users.Where(u => u.phoneNumber == number).Select(u => u.verificationExpire).First();

                        if (DateTime.Now > verificationExpire)
                        {
                            return BadRequest("Verification code expired");
                        }
                        if (code == verificationCode)
                        {
                            _dbContext.Users.Where(u => u.phoneNumber == info.Number).First().lastLogin = DateTime.Now;
                            try
                            {
                                await _dbContext.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {
                                return StatusCode(500, "An error occurred while saving the user to the database");
                            }
                            return Ok("Code verified");
                        }
                        else
                        {
                            return BadRequest("Invalid code");
                        }
                    }
                    catch (Exception e)
                    {
                        return BadRequest(e.Message);
                    }
                }
            }
        }
    }
}
