using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Security.Cryptography;

namespace RingServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CredentialsController : ControllerBase
    {

        private readonly IDataProtector _protector;
        private readonly string publicKeyPath;
        private readonly string privateKeyPath;

        public CredentialsController(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector("Keys");
            publicKeyPath = "RingServer/Keys/serverPublicKey.pem";
            privateKeyPath = "RingServer/Keys/serverPrivateKey.pem";
        }

        [HttpGet]
        [Route("/api/v1/auth/publickey")]
        public string GetKeys()
        {

            if (System.IO.File.Exists(publicKeyPath))
            {
                var publicKey = System.IO.File.ReadAllBytes(publicKeyPath);

                if (publicKey.Length == 0) { return "Key length = 0"; }

                var publicString = Convert.ToBase64String(publicKey);

                return ($"{publicString}");
            }
            else
            {
                return "Keys not found";
            }
        }

        [HttpPost]
        [Route("/api/v1/auth/removeKeys")]
        public string RemoveKeys()
        {

            bool removedPublic = false;
            bool removedPrivate = false;

            if (System.IO.File.Exists(publicKeyPath)) { System.IO.File.Delete(publicKeyPath); removedPublic = true;  }
            if (System.IO.File.Exists(privateKeyPath)) { System.IO.File.Delete(privateKeyPath); removedPrivate = true;  }

            if (removedPublic && removedPrivate) return "Keys removed";
            else if (removedPublic && !removedPrivate) return "Public removed, private not found";
            else if (!removedPublic && removedPrivate) return "Private removed, public not found";
            else return "Keys not found";

        }

        [HttpPost]
        [Route("/api/v1/auth/generateKeys")]
        public string GenerateKeys()
        {

            if (System.IO.File.Exists(publicKeyPath) || System.IO.File.Exists(privateKeyPath))
            {
                return "Keys already exist";
            }

            try
            {
                var rsa = RSA.Create(2048);
                var publicKey = rsa.ExportSubjectPublicKeyInfo();
                var privateKey = rsa.ExportRSAPrivateKey();

                System.IO.File.WriteAllBytes(publicKeyPath, publicKey);
                System.IO.File.WriteAllBytes(privateKeyPath, _protector.Protect(privateKey));

                return "Keys generated";
            }
            catch (Exception ex)
            {
                   return ex.Message;
            }
        }
    }
}