using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Security.Cryptography;
using RingServer.Utils;

namespace RingServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class KeysController : Controller
    {
        // Protettori per la chiave pubblica e privata
        private readonly IDataProtector _publicProtector;
        private readonly IDataProtector _privateProtector;

        // Percorso del file di configurazione e updater per la modifica, prelievo o rimozione dei dati
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

            // Rimuovo le chiavi pubbliche e private se ci sono
            if (_updater.GetSetting("publicKey") != string.Empty) { _updater.RemoveSetting("publicKey"); removedPublic = true;  }
            if (_updater.GetSetting("privateKey") != string.Empty) { _updater.RemoveSetting("privateKey"); removedPrivate = true;  }

            // A seconda di quali chiavi sono state rimosse, ritorno un messaggio diverso
            if (removedPublic && removedPrivate) return Ok("Keys removed");
            else if (removedPublic && !removedPrivate) return Ok("Public removed, private not found");
            else if (!removedPublic && removedPrivate) return Ok("Private removed, public not found");
            else return NotFound("Keys not found");
        }

        [HttpPost]
        [Route("/api/v1/keys/generate")]
        public IActionResult GenerateKeys()
        {
            // Controllo se le chiavi esistono già
            bool publicExist = _updater.GetSetting("publicKey") != string.Empty;
            bool privateExist = _updater.GetSetting("privateKey") != string.Empty;

            // Se esistono entrambe le chiavi, ritorno un errore
            if (publicExist && privateExist)
            {
                return BadRequest("Keys already exist");
            }

            try
            {
                // Prendo le chiavi dal formato PEM usando RSA
                using RSA rsa = RSA.Create(2048);
                var publicKey = rsa.ExportRSAPublicKeyPem();
                var privateKey = rsa.ExportRSAPrivateKeyPem();

                // Proteggo le chiavi
                if (publicExist) _updater.RemoveSetting("publicKey");
                if (privateExist) _updater.RemoveSetting("privateKey");
                var protectedPublicKey = _publicProtector.Protect(publicKey);
                var protectedPrivateKey = _privateProtector.Protect(privateKey);

                // Aggiungo le chiavi al file di configurazione
                _updater.AddSetting("publicKey", protectedPublicKey);
                _updater.AddSetting("privateKey", protectedPrivateKey);

                return Ok("Keys generated");
            }
            catch (Exception ex)
            {
                   return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("/api/v1/keys/getpublic")]
        public IActionResult GetPublicKey()
        {
            // Controllo se la chiave pubblica esiste
            if (_updater.GetSetting("publicKey") == string.Empty)
            {
                return NotFound("Public key not found");
            }

            // Ritorno la chiave pubblica
            string protectedPublicKeyPem = _updater.GetSetting("publicKey");
            string publicKeyPem = _publicProtector.Unprotect(protectedPublicKeyPem);
            return Ok(publicKeyPem);
        }

        [HttpGet]
        [Route("/api/v1/keys/getprivate")]
        public IActionResult GetPrivateKey()
        {
            // Controllo se la chiave privata esiste
            if (_updater.GetSetting("privateKey") == string.Empty)
            {
                return NotFound("Private key not found");
            }

            // Ritorno la chiave privata
            string protectedPrivateKeyPem = _updater.GetSetting("privateKey");
            string privateKeyPem = _privateProtector.Unprotect(protectedPrivateKeyPem);
            return Ok(privateKeyPem);
        }
    }
}