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

                string protectedPrivateKeyPem = _updater.GetSetting("privateKey");
                string privateKeyPem = _privateProtector.Unprotect(protectedPrivateKeyPem);

                string plaintext = CryptographyTools.DecryptString(privateKeyPem, requestBody);
                var userInfo = JsonConvert.DeserializeObject<CommonClasses.AllGatesRequest>(plaintext);

                if (userInfo == null)
                {
                    return BadRequest("Invalid request payload");
                }

               
                string[] gatesIds = _dbContext.UsersGates.Where(ug => ug.phoneNumber == userInfo.Number && ug.deviceId == userInfo.Id).Select(ug => ug.gateId).ToArray();

                if (gatesIds == null)
                {
                    return BadRequest("No gates found for the given user");
                }

                List<Gate> gates = _dbContext.Gates.Where(g => gatesIds.Contains(g.gateId)).ToList();
                string key = _dbContext.Users.Where(u => u.phoneNumber == userInfo.Number && u.deviceId == userInfo.Id).First().publicKey;

                var encryptedGates = CryptographyTools.EncryptString(key, JsonConvert.SerializeObject(gates));

                return Ok(encryptedGates);
            }
        }

        [HttpPost]
        [Route("/api/v1/gate/getsinglegate")]
        public async Task<IActionResult> GetSingleGate()
        {
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var requestBody = await reader.ReadToEndAsync();
                if (requestBody == null)
                {
                    return BadRequest("Outer invalid request payload");
                }

                string protectedPrivateKeyPem = _updater.GetSetting("privateKey");
                string privateKeyPem = _privateProtector.Unprotect(protectedPrivateKeyPem);

                string plaintext = CryptographyTools.DecryptString(privateKeyPem, requestBody);
                var userInfo = JsonConvert.DeserializeObject<CommonClasses.SingleGateRequest>(plaintext);

                if (userInfo == null)
                {
                    return BadRequest("Invalid request payload");
                }

                string gateId = _dbContext.UsersGates.Where(ug => ug.phoneNumber == userInfo.Number && ug.deviceId == userInfo.Id && ug.gateId == userInfo.GateId).First().gateId;
                Gate gate = _dbContext.Gates.Where(g => g.gateId == gateId).First();

                if (gate == null)
                {
                    return BadRequest("No gates found for the given user");
                }

                string key = _dbContext.Users.Where(u => u.phoneNumber == userInfo.Number && u.deviceId == userInfo.Id).First().publicKey;
                var encryptedGate = CryptographyTools.EncryptString(key, JsonConvert.SerializeObject(gate));

                return Ok(encryptedGate);
            }
        }
    }
}