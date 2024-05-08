using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using Vonage.Messaging;
using Vonage;
using Microsoft.EntityFrameworkCore;
using RingServer.Utils;
using Jose;
using Microsoft.IdentityModel.Tokens;

namespace RingServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CredentialsController : Controller
    {
        // Protettori per la chiave pubblica e privata
        private readonly IDataProtector _publicProtector;
        private readonly IDataProtector _privateProtector;

        // Percorso del file di configurazione e updater per la modifica, prelievo o rimozione dei dati
        private readonly string _config;
        private AppSettingsUpdater _updater;

        // Credenziali per l'invio di SMS
        private Vonage.Request.Credentials vonageCredentials;

        // Contesto del database
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


        [HttpGet]
        [Route("/api/v1/auth/publickey")]
        public IActionResult GetKeys()
        {
            // Controllo se la chiave pubblica esiste
            string publicKey = _updater.GetSetting("publicKey");
            if (publicKey != string.Empty)
            {
                
                if (publicKey.Length == 0)
                {
                    return BadRequest("Key length = 0");
                }
                else
                {
                    // Ritorno la chiave pubblica dopo aver rimosso il protettore
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

                try
                {
                    // Decifro il payload ricevuto
                    string protectedPrivateKeyPem = _updater.GetSetting("privateKey");
                    string privateKeyPem = _privateProtector.Unprotect(protectedPrivateKeyPem);

                    bool outcome;
                    string plaintext;
                    (outcome, plaintext) = CryptographyTools.DecryptString(privateKeyPem, requestBody);
                    if(!outcome)
                    {
                        return BadRequest(plaintext);
                    }

                    var userInfo = JsonConvert.DeserializeObject<CommonClasses.SignInRequest>(plaintext);
                    if (userInfo != null)
                    {
                        // Aggiungo l'utente al database se non esiste
                        if (_dbContext.Users.All(u => u.phoneNumber != userInfo.Number || u.deviceId != userInfo.Id))
                        {
                            if (!_dbContext.Users.Where(u => u.phoneNumber == userInfo.Number && u.deviceId != userInfo.Id).ToList().IsNullOrEmpty())
                            {
                                _dbContext.Users.Add(new User
                                {
                                    phoneNumber = userInfo.Number,
                                    deviceId = userInfo.Id,
                                    verificationCode = string.Empty,
                                    verificationExpire = DateTime.MinValue,
                                    lastLogin = DateTime.MinValue,
                                    publicKey = userInfo.PKey,
                                    rememberLogin = userInfo.RememberLogin
                                });

                                // devo aggiungere tutti i cancelli già presenti nel database a quel numero anche a quel device id
                                var gates = _dbContext.UsersGates.Where(u => u.phoneNumber == userInfo.Number).ToList();
                                foreach (var gate in gates)
                                {
                                    _dbContext.UsersGates.Add(new UserGate
                                    {
                                        usergateId = Guid.NewGuid().ToString(),
                                        phoneNumber = userInfo.Number,
                                        deviceId = userInfo.Id,
                                        gateId = gate.gateId,
                                        role = gate.role
                                    });
                                }
                            }
                            else
                            {
                                _dbContext.Users.Add(new User
                                {
                                    phoneNumber = userInfo.Number,
                                    deviceId = userInfo.Id,
                                    verificationCode = string.Empty,
                                    verificationExpire = DateTime.MinValue,
                                    lastLogin = DateTime.MinValue,
                                    publicKey = userInfo.PKey,
                                    rememberLogin = userInfo.RememberLogin
                                });
                            }
                        }
                        // Altrimenti aggiorno i dati dell'utente
                        else
                        {
                            _dbContext.Users.Where(u => u.phoneNumber == userInfo.Number && u.deviceId == userInfo.Id).First().verificationCode = string.Empty;
                            _dbContext.Users.Where(u => u.phoneNumber == userInfo.Number && u.deviceId == userInfo.Id).First().lastLogin = DateTime.MinValue;
                            _dbContext.Users.Where(u => u.phoneNumber == userInfo.Number && u.deviceId == userInfo.Id).First().publicKey = userInfo.PKey;
                            _dbContext.Users.Where(u => u.phoneNumber == userInfo.Number && u.deviceId == userInfo.Id).First().rememberLogin = userInfo.RememberLogin;
                        }

                        try
                        {
                            await _dbContext.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            return StatusCode(500, "An error occurred while saving the user to the database: " + ex.Message);
                        }
                        return Ok();
                    }
                    else
                    {
                        return BadRequest("Invalid request payload");
                    }
                }
                catch (Exception e)
                {
                    return BadRequest(e.Message);
                }
            }
        }

        [HttpPost]
        [Route("/api/v1/auth/sendcode")]
        public async Task<IActionResult> SendSMS()
        {
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
                        // Decifro il payload ricevuto
                        string protectedPrivateKeyPem = _updater.GetSetting("privateKey");
                        string privateKeyPem = _privateProtector.Unprotect(protectedPrivateKeyPem);

                        bool outcome;
                        string plaintext;
                        CommonClasses.Identifier userInfo;
                        (outcome, plaintext, userInfo) = CryptographyTools.TotalDecrypt(privateKeyPem, requestBody, _dbContext);
                        if (!outcome || string.IsNullOrEmpty(plaintext) || userInfo == null)
                        {
                            return BadRequest(plaintext);
                        }

                        string to = userInfo.Number;

                        // Genero un codice di verifica e lo salvo nel database con la scadenza
                        string verificationCode = new Random().Next(10000000, 99999999).ToString();
                        _dbContext.Users.Where(u => u.phoneNumber == to && u.deviceId == userInfo.Id).First().verificationCode = verificationCode;
                        _dbContext.Users.Where(u => u.phoneNumber == to && u.deviceId == userInfo.Id).First().verificationExpire = DateTime.Now.AddMinutes(5);
                        try
                        {
                            await _dbContext.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            return StatusCode(500, "An error occurred while saving the user to the database: " + ex.Message);
                        }

                        // TEMP UNAVAIBLE
                        /*
                        string message = "Your verification code is: " + verificationCode;
                        var vonageClient = new VonageClient(vonageCredentials);
                        var smsResponse = vonageClient.SmsClient.SendAnSms(new SendSmsRequest
                        {
                            To = to,
                            From = "Ring",
                            Text = message
                        });
                        */
                        return Ok("SMS sent");
                    }
                    catch (Exception ex)
                    {
                        return BadRequest("SMS not sent: " + ex.Message);
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
                        // Decifro il payload ricevuto
                        string protectedPrivateKeyPem = _updater.GetSetting("privateKey");
                        string privateKeyPem = _privateProtector.Unprotect(protectedPrivateKeyPem);

                        bool outcome;
                        string plaintext;
                        CommonClasses.Identifier userInfo;
                        (outcome, plaintext, userInfo) = CryptographyTools.TotalDecrypt(privateKeyPem, requestBody, _dbContext);
                        if (!outcome || string.IsNullOrEmpty(plaintext) || userInfo == null)
                        {
                            return BadRequest(plaintext);
                        }

                        var inserted = JsonConvert.DeserializeObject<string>(plaintext);

                        // Prendo il codice di verifica e la scadenza dal database
                        string verificationCode = _dbContext.Users.Where(u => u.phoneNumber == userInfo.Number && u.deviceId == userInfo.Id).Select(u => u.verificationCode).First();
                        DateTime verificationExpire = _dbContext.Users.Where(u => u.phoneNumber == userInfo.Number && u.deviceId == userInfo.Id).Select(u => u.verificationExpire).First();
                        
                        // Controllo se il codice è scaduto
                        if (DateTime.Now > verificationExpire)
                        {
                            return BadRequest("Verification code expired");
                        }
                        // Controllo se il codice è corretto
                        else if (inserted == verificationCode)
                        {
                            // Aggiorno il database con l'ultimo accesso
                            _dbContext.Users.Where(u => u.phoneNumber == userInfo.Number && u.deviceId == userInfo.Id).First().lastLogin = DateTime.Now;
                            try
                            {
                                await _dbContext.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {
                                return StatusCode(500, ex.Message);
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

        [HttpPost]
        [Route("/api/v1/auth/checklogout")]
        public async Task<IActionResult> CheckLogout()
        {
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
                        // Decifro il payload ricevuto
                        string protectedPrivateKeyPem = _updater.GetSetting("privateKey");
                        string privateKeyPem = _privateProtector.Unprotect(protectedPrivateKeyPem);

                        bool outcome;
                        string plaintext;
                        CommonClasses.Identifier userInfo;
                        (outcome, plaintext, userInfo) = CryptographyTools.TotalDecrypt(privateKeyPem, requestBody, _dbContext);
                        if (!outcome || string.IsNullOrEmpty(plaintext) || userInfo == null)
                        {
                            return BadRequest(plaintext);
                        }

                        // Controllo se l'utente esiste
                        if (_dbContext.Users.All(u => u.phoneNumber != userInfo.Number || u.deviceId != userInfo.Id))
                        {
                            return BadRequest("User not found");
                        }
                        else
                        {
                            // Prendo l'ultimo accesso dal database
                            DateTime lastLogin = _dbContext.Users.Where(u => u.phoneNumber == userInfo.Number && u.deviceId == userInfo.Id).First().lastLogin;
                            string rememberLogin = _dbContext.Users.Where(u => u.phoneNumber == userInfo.Number && u.deviceId == userInfo.Id).First().rememberLogin;
                            // Controllo se l'utente ha richiesto di non ricordare il login
                            if (rememberLogin == "n")
                            {
                                // Controllo se sono passati 10 minuti dall'ultimo accesso, dato che non è stato richiesto di ricordare il login
                                if (lastLogin.AddMinutes(10) <= DateTime.Now)
                                {
                                    _dbContext.Users.Where(u => u.phoneNumber == userInfo.Number && u.deviceId == userInfo.Id).First().lastLogin = DateTime.MinValue;
                                    await _dbContext.SaveChangesAsync();
                                    return BadRequest("Login Expired");
                                }
                                else
                                {
                                    return Ok("Login Valid");
                                }
                            }
                            else
                            {
                                // Controllo se sono passati 60 giorni dall'ultimo accesso, dato che è stato richiesto di ricordare il login
                                if (lastLogin.AddDays(60) <= DateTime.Now)
                                {
                                    _dbContext.Users.Where(u => u.phoneNumber == userInfo.Number && u.deviceId == userInfo.Id).First().lastLogin = DateTime.MinValue;
                                    await _dbContext.SaveChangesAsync();
                                    return BadRequest("Login Expired");
                                }
                                else
                                {
                                    return Ok("Login Valid");
                                }
                            }
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
