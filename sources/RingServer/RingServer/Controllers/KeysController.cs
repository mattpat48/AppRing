using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Security.Cryptography;

namespace RingServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class KeysController : ControllerBase
    {

        private readonly IDataProtector _publicProtector;
        private readonly IDataProtector _privateProtector;
        private readonly string _config;
        private AppSettingsUpdater _updater;

        public KeysController(IDataProtectionProvider provider)
        {
            _publicProtector = provider.CreateProtector("PublicKeyProtector");
            _privateProtector = provider.CreateProtector("PrivateKeyProtector");
            _config = "appsettings.json";
            _updater = new AppSettingsUpdater(_config);
        }

        [HttpPost]
        [Route("/api/v1/keys/remove")]
        public IActionResult RemoveKeys()
        {

            bool removedPublic = false;
            bool removedPrivate = false;

            if (_updater.GetSetting("publicKey") != string.Empty) { _updater.RemoveSetting("publicKey"); removedPublic = true;  }
            if (_updater.GetSetting("privateKey") != string.Empty) { _updater.RemoveSetting("privateKey"); removedPrivate = true;  }

            if (removedPublic && removedPrivate) return Ok("Keys removed");
            else if (removedPublic && !removedPrivate) return Ok("Public removed, private not found");
            else if (!removedPublic && removedPrivate) return Ok("Private removed, public not found");
            else return NotFound("Keys not found");

        }

        [HttpPost]
        [Route("/api/v1/keys/generate")]
        public IActionResult GenerateKeys()
        {

            bool publicExist = _updater.GetSetting("publicKey") != string.Empty;
            bool privateExist = _updater.GetSetting("privateKey") != string.Empty;

            if (publicExist && privateExist)
            {
                return BadRequest("Keys already exist");
            }

            try
            {
                var rsa = RSA.Create(4096);
                var publicKey = rsa.ExportRSAPublicKeyPem();
                var privateKey = rsa.ExportRSAPrivateKeyPem();

                // Criptare e salvare la chiave pubblica e quella privata

                if (publicExist) _updater.RemoveSetting("publicKey");
                if (privateExist) _updater.RemoveSetting("privateKey");
                var protectedPublicKey = _publicProtector.Protect(publicKey);
                var protectedPrivateKey = _privateProtector.Protect(privateKey);

                _updater.AddSetting("publicKey", protectedPublicKey);
                _updater.AddSetting("privateKey", protectedPrivateKey);

                return Ok("Keys generated");
            }
            catch (Exception ex)
            {
                   return BadRequest(ex.Message);
            }
        }
    }
}