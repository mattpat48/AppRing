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

        private readonly IDataProtector _protector;
        private readonly string publicKeyPath;
        private readonly string privateKeyPath;

        public KeysController(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector("Keys");
            publicKeyPath = "RingServer/Keys/serverPublicKey.pem";
            privateKeyPath = "RingServer/Keys/serverPrivateKey.pem";
        }

        [HttpPost]
        [Route("/api/v1/keys/remove")]
        public IActionResult RemoveKeys()
        {

            bool removedPublic = false;
            bool removedPrivate = false;

            if (System.IO.File.Exists(publicKeyPath)) { System.IO.File.Delete(publicKeyPath); removedPublic = true;  }
            if (System.IO.File.Exists(privateKeyPath)) { System.IO.File.Delete(privateKeyPath); removedPrivate = true;  }

            if (removedPublic && removedPrivate) return Ok("Keys removed");
            else if (removedPublic && !removedPrivate) return Ok("Public removed, private not found");
            else if (!removedPublic && removedPrivate) return Ok("Private removed, public not found");
            else return NotFound("Keys not found");

        }

        [HttpPost]
        [Route("/api/v1/keys/generate")]
        public IActionResult GenerateKeys()
        {

            if (System.IO.File.Exists(publicKeyPath) || System.IO.File.Exists(privateKeyPath))
            {
                return BadRequest("Keys already exist");
            }

            try
            {
                var rsa = RSA.Create(4096);
                var publicKey = rsa.ExportRSAPublicKeyPem();
                var privateKey = rsa.ExportRSAPrivateKeyPem();

                System.IO.File.WriteAllText(publicKeyPath, publicKey);
                System.IO.File.WriteAllText(privateKeyPath, _protector.Protect(privateKey));

                return Ok("Keys generated");
            }
            catch (Exception ex)
            {
                   return BadRequest(ex.Message);
            }
        }
    }
}