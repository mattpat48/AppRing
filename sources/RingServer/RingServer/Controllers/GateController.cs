using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RingServer.Utils;
using System.Security.Cryptography;
using System.Text;

namespace RingServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GateController : Controller
    {

        private readonly IDataProtector _publicProtector;
        private readonly IDataProtector _privateProtector;

        private readonly string _config;
        private AppSettingsUpdater _updater;

        private readonly RingDBContext _dbContext;

        public GateController(IDataProtectionProvider provider, RingDBContext ringDBContext)
        {
            _publicProtector = provider.CreateProtector("PublicKeyProtector");
            _privateProtector = provider.CreateProtector("PrivateKeyProtector");

            _config = "appsettings.json";
            _updater = new AppSettingsUpdater(_config);

            _dbContext = ringDBContext;
        }

        [HttpPost]
        [Route("/api/v1/gate/getallgates")]
        public async Task<IActionResult> GetAllGates()
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
                    string protectedPrivateKeyPem = _updater.GetSetting("privateKey");
                    string privateKeyPem = _privateProtector.Unprotect(protectedPrivateKeyPem);

                    bool outcome;
                    string plaintext;
                    CommonClasses.Identifier userInfo;
                    (outcome, plaintext, userInfo) = CryptographyTools.TotalDecrypt(privateKeyPem, requestBody, _dbContext);
                    if (!outcome || string.IsNullOrEmpty(plaintext) || userInfo == null)
                    {
                        return BadRequest("Invalid request payload");
                    }

                    string[] gatesIds = _dbContext.UsersGates.Where(ug => ug.phoneNumber == userInfo.Number).Select(ug => ug.gateId).ToArray();

                    if (gatesIds == null)
                    {
                        return BadRequest("No gates found for the given user");
                    }

                    List<Gate> gates = _dbContext.Gates.Where(g => gatesIds.Contains(g.gateId)).ToList();
                    string userKey = _dbContext.Users.Where(u => u.phoneNumber == userInfo.Number && u.deviceId == userInfo.Id).First().publicKey;

                    bool outcome2;
                    object encryptedGates;
                    (outcome2, encryptedGates) = CryptographyTools.TotalEncrypt(privateKeyPem, userKey, JsonConvert.SerializeObject(gates));
                    if (!outcome2)
                    {
                        return BadRequest("Error encrypting gates");
                    }

                    return Ok(encryptedGates);
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }
        }


        [HttpPost]
        [Route("/api/v1/gate/isadmin")]
        public async Task<IActionResult> IsAdmin()
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
                    string protectedPrivateKeyPem = _updater.GetSetting("privateKey");
                    string privateKeyPem = _privateProtector.Unprotect(protectedPrivateKeyPem);

                    bool outcome;
                    string plaintext;
                    CommonClasses.Identifier userInfo;
                    (outcome, plaintext, userInfo) = CryptographyTools.TotalDecrypt(privateKeyPem, requestBody, _dbContext);
                    if (!outcome || string.IsNullOrEmpty(plaintext) || userInfo == null)
                    {
                        return BadRequest("Invalid request payload");
                    }

                    string gateId = JsonConvert.DeserializeObject<string>(plaintext);

                    string requesterRole = _dbContext.UsersGates.Where(u => u.phoneNumber == userInfo.Number && u.gateId == gateId).First().role;

                    return Ok(requesterRole == "a");
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }
        }


        [HttpPost]
        [Route("/api/v1/gate/adduser")]
        public async Task<IActionResult> AddUser()
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
                    string protectedPrivateKeyPem = _updater.GetSetting("privateKey");
                    string privateKeyPem = _privateProtector.Unprotect(protectedPrivateKeyPem);

                    bool outcome;
                    string plaintext;
                    CommonClasses.Identifier userInfo;
                    (outcome, plaintext, userInfo) = CryptographyTools.TotalDecrypt(privateKeyPem, requestBody, _dbContext);
                    if (!outcome || string.IsNullOrEmpty(plaintext) || userInfo == null)
                    {
                        return BadRequest("Invalid request payload");
                    }

                    CommonClasses.AddUserRequest addUserRequest = JsonConvert.DeserializeObject<CommonClasses.AddUserRequest>(plaintext);

                    if (addUserRequest == null)
                    {
                        return BadRequest("Invalid request payload");
                    }

                    string requesterRole = _dbContext.UsersGates.Where(u => u.phoneNumber == userInfo.Number && u.gateId == addUserRequest.GateId).First().role;

                    if (requesterRole != "a")
                    {
                        return BadRequest("User is not an admin");
                    }

                    if (!_dbContext.Gates.Any(g => g.gateId == addUserRequest.GateId))
                    {
                        return BadRequest("Gate not found");
                    }
                    if (!_dbContext.Users.Any(u => u.phoneNumber == addUserRequest.ToAdd))
                    {
                        return BadRequest("User not found");
                    }

                    List<User> usersToAdd = _dbContext.Users.Where(u => u.phoneNumber == addUserRequest.ToAdd).ToList();

                    foreach (User user in usersToAdd)
                    {
                        if (!_dbContext.UsersGates.Any(ug => ug.phoneNumber == user.phoneNumber && ug.gateId == addUserRequest.GateId))
                        {
                            _dbContext.UsersGates.Add(new UserGate
                            {
                                phoneNumber = user.phoneNumber,
                                gateId = addUserRequest.GateId,
                                role = "u"
                            });
                        }
                    }

                    _dbContext.SaveChanges();

                    return Ok("User added to gate");
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }
        }


        [HttpPost]
        [Route("/api/v1/gate/generatelog")]
        public async Task<IActionResult> GenerateLog()
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
                    string protectedPrivateKeyPem = _updater.GetSetting("privateKey");
                    string privateKeyPem = _privateProtector.Unprotect(protectedPrivateKeyPem);

                    bool outcome;
                    string plaintext;
                    CommonClasses.Identifier userInfo;
                    (outcome, plaintext, userInfo) = CryptographyTools.TotalDecrypt(privateKeyPem, requestBody, _dbContext);
                    if (!outcome || string.IsNullOrEmpty(plaintext) || userInfo == null)
                    {
                        return BadRequest("Invalid request payload");
                    }

                    string gateId = JsonConvert.DeserializeObject<string>(plaintext);

                    if (gateId == null)
                    {
                        return BadRequest("Empty Gate ID");
                    }

                    _dbContext.Logs.Add(new Log{
                        phoneNumber = userInfo.Number,
                        deviceId = userInfo.Id,
                        gateId = gateId,
                        date = DateTime.Now
                    });

                    await _dbContext.SaveChangesAsync();

                    return Ok("Log Generated");
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }
        }


        [HttpPost]
        [Route("/api/v1/gate/manuallyaddlogs")]
        public async Task<IActionResult> ManuallyAddLogs()
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
                    string protectedPrivateKeyPem = _updater.GetSetting("privateKey");
                    string privateKeyPem = _privateProtector.Unprotect(protectedPrivateKeyPem);

                    bool outcome;
                    string plaintext;
                    CommonClasses.Identifier userInfo;
                    (outcome, plaintext, userInfo) = CryptographyTools.TotalDecrypt(privateKeyPem, requestBody, _dbContext);
                    if (!outcome || string.IsNullOrEmpty(plaintext) || userInfo == null)
                    {
                        return BadRequest("Invalid request payload");
                    }

                    List<Log> manuallyAddLogRequest = JsonConvert.DeserializeObject<List<Log>>(plaintext);

                    if (manuallyAddLogRequest == null)
                    {
                        return BadRequest("Invalid request payload");
                    }

                    foreach (var log in manuallyAddLogRequest)
                    {
                        if (!_dbContext.Gates.Any(g => g.gateId == log.gateId))
                        {
                            return BadRequest("Gate not found");
                        }
                        if (!_dbContext.Users.Any(u => u.phoneNumber == log.phoneNumber))
                        {
                            return BadRequest("User not found");
                        }

                        _dbContext.Logs.Add(new Log
                        {
                            phoneNumber = log.phoneNumber,
                            deviceId = log.deviceId,
                            gateId = log.gateId,
                            date = log.date
                        });
                    }

                    await _dbContext.SaveChangesAsync();

                    return Ok("Log Generated");
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }
        }


        [HttpPost]
        [Route("/api/v1/gate/getlogs")]
        public async Task<IActionResult> GetLogs()
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
                    string protectedPrivateKeyPem = _updater.GetSetting("privateKey");
                    string privateKeyPem = _privateProtector.Unprotect(protectedPrivateKeyPem);

                    bool outcome;
                    string plaintext;
                    CommonClasses.Identifier userInfo;
                    (outcome, plaintext, userInfo) = CryptographyTools.TotalDecrypt(privateKeyPem, requestBody, _dbContext);
                    if (!outcome || string.IsNullOrEmpty(plaintext) || userInfo == null)
                    {
                        return BadRequest("Invalid request payload");
                    }

                    string gateId = JsonConvert.DeserializeObject<string>(plaintext);

                    if (string.IsNullOrEmpty(gateId))
                    {
                        return BadRequest("Empty gate id");
                    }

                    List<Log> logs = _dbContext.Logs.Where(l => l.gateId == gateId).ToList();

                    string userKey = _dbContext.Users.Where(u => u.phoneNumber == userInfo.Number && u.deviceId == userInfo.Id).First().publicKey;

                    bool outcome2;
                    object encryptedLogs;
                    (outcome2, encryptedLogs) = CryptographyTools.TotalEncrypt(privateKeyPem, userKey, JsonConvert.SerializeObject(logs));
                    if (!outcome2)
                    {
                        return BadRequest("Error encrypting gates");
                    }

                    return Ok(encryptedLogs);
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }
        }

        [HttpPost]
        [Route("/api/v1/gate/getuserspergate")]
        public async Task<IActionResult> GetUsersPerGate()
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
                    string protectedPrivateKeyPem = _updater.GetSetting("privateKey");
                    string privateKeyPem = _privateProtector.Unprotect(protectedPrivateKeyPem);

                    bool outcome;
                    string plaintext;
                    CommonClasses.Identifier userInfo;
                    (outcome, plaintext, userInfo) = CryptographyTools.TotalDecrypt(privateKeyPem, requestBody, _dbContext);
                    if (!outcome || string.IsNullOrEmpty(plaintext) || userInfo == null)
                    {
                        return BadRequest("Invalid request payload");
                    }

                    string gateId = JsonConvert.DeserializeObject<string>(plaintext);

                    if (string.IsNullOrEmpty(gateId))
                    {
                        return BadRequest("Empty gate id");
                    }

                    List<UserGate> users = _dbContext.UsersGates.Where(ug => ug.gateId == gateId).ToList();

                    List<string> usersNumbers = users.Select(u => u.phoneNumber).ToList();

                    string userKey = _dbContext.Users.Where(u => u.phoneNumber == userInfo.Number && u.deviceId == userInfo.Id).First().publicKey;

                    bool outcome2;
                    object encryptedUsers;
                    (outcome2, encryptedUsers) = CryptographyTools.TotalEncrypt(privateKeyPem, userKey, JsonConvert.SerializeObject(usersNumbers));
                    if (!outcome2)
                    {
                        return BadRequest("Error encrypting gates");
                    }

                    return Ok(encryptedUsers);
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }
        }
    }
}