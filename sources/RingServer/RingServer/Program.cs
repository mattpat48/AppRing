
using System.Data.SqlTypes;
using System.IO;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using System.Text;

namespace RingServer
{
    public class Program
    {

        public static void Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddDataProtection();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            if (!CheckForKeys())
            {
                var rsa = RSA.Create(2048);
                var publicKey = rsa.ExportSubjectPublicKeyInfo();
                var privateKey = rsa.ExportRSAPrivateKey();
                SaveRSAKeys(app.Services, publicKey, privateKey);
            }

            app.Run();
        }

        private static bool CheckForKeys()
        {
            string publicKeyPath = "RingServer/Keys/serverPublicKey.pem";
            string privateKeyPath = "RingServer/Keys/serverPrivateKey.pem";

            return File.Exists(publicKeyPath) && File.Exists(privateKeyPath);
        }

        private static void SaveRSAKeys(IServiceProvider services, byte[] publicKey, byte[] privateKey)
        {
            // Percorsi dove salvare le chiavi
            string publicKeyPath = "RingServer/Keys/serverPublicKey.pem";
            string privateKeyPath = "RingServer/Keys/serverPrivateKey.pem";

            // Estrai i percorsi delle directory dalle stringhe dei percorsi dei file
            string publicKeyDir = Path.GetDirectoryName(publicKeyPath);
            string privateKeyDir = Path.GetDirectoryName(privateKeyPath);

            // Crea le directory se non esistono
            if (!string.IsNullOrEmpty(publicKeyDir)) { Directory.CreateDirectory(publicKeyDir); }
            if (!string.IsNullOrEmpty(privateKeyDir)) { Directory.CreateDirectory(privateKeyDir); }

            // Salvataggio della chiave pubblica
            File.WriteAllBytes(publicKeyPath, publicKey);

            // Criptare e salvare la chiave privata
            var protector = services.GetDataProtector("Keys");
            var protectedKey = protector.Protect(privateKey);

            File.WriteAllBytes(publicKeyPath, publicKey);
            File.WriteAllBytes(privateKeyPath, protectedKey);
        }

        private static byte[] LoadPrivateKey(IServiceProvider services, string filePath)
        {
            byte[] protectedKey = File.ReadAllBytes(filePath);
            var protector = services.GetDataProtector("Keys");
            return protector.Unprotect(protectedKey);
        }

        private static byte[] LoadPublicKey(IServiceProvider services, string filePath)
        {
            byte[] publicKey = File.ReadAllBytes(filePath);
            return publicKey;
        }

    }
}
