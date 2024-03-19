
using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

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
            builder.Services.AddDistributedMemoryCache();

            builder.Services.AddSession(options =>
            {
                // Configure session options if needed (e.g., IdleTimeout)
                options.IdleTimeout = TimeSpan.FromMinutes(30); // Example: Set session timeout
                options.Cookie.HttpOnly = true; // Prevents accessing the cookie from client-side scripts
                options.Cookie.IsEssential = true; // Mark the session cookie as essential
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseSession();
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            if (!CheckForKeys())
            {
                using RSA rsa = RSA.Create(2048);
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

    }
}
