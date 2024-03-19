
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
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddDistributedMemoryCache();

            builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(@"D:\Apps\AppRing"))
            .SetApplicationName("RingServer");

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
            AppSettingsUpdater updater = new AppSettingsUpdater("appsettings.json");
            return updater.GetSetting("publicKey") != string.Empty && updater.GetSetting("privateKey") != string.Empty;
        }

        private static void SaveRSAKeys(IServiceProvider services, byte[] publicKey, byte[] privateKey)
        {
            var publicProtector = services.GetRequiredService<IDataProtectionProvider>().CreateProtector("PublicKeyProtector");
            var privateProtector = services.GetRequiredService<IDataProtectionProvider>().CreateProtector("PrivateKeyProtector");

            // Criptare e salvare la chiave pubblica e quella privata
            var protectedPublicKey = publicProtector.Protect(publicKey);
            var protectedPrivateKey = privateProtector.Protect(privateKey);

            AppSettingsUpdater updater = new AppSettingsUpdater("appsettings.json");
            if (updater.GetSetting("publicKey") == string.Empty)
            {
                updater.AddSetting("publicKey", Convert.ToBase64String(protectedPublicKey));
            }
            if (updater.GetSetting("privateKey") == string.Empty)
            {
                updater.AddSetting("privateKey", Convert.ToBase64String(protectedPrivateKey));
            }

        }

    }
}
