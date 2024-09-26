using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using RingServer.Utils;

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

            // utilizzando SQLServer
            builder.Services.AddDbContext<RingDBContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("SQLServerDatabaseConnection")));
            // utilizzando Azure SQL
            //builder.Services.AddDbContext<RingDBContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("AzureSQLDatabaseConnection")));

            var app = builder.Build();
            //app.Urls.Add("https://10.20.100.50:7046");
            app.Urls.Add("https://192.168.1.14" + ":7046");

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
                var publicKey = rsa.ExportRSAPublicKeyPem();
                var privateKey = rsa.ExportRSAPrivateKeyPem();
                SaveRSAKeys(app.Services, publicKey, privateKey);
            }

            app.Run();
        }

        private static bool CheckForKeys()
        {
            AppSettingsUpdater updater = new AppSettingsUpdater("appsettings.json");
            return updater.GetSetting("publicKey") != string.Empty && updater.GetSetting("privateKey") != string.Empty;
        }

        private static void SaveRSAKeys(IServiceProvider services, string publicKey, string privateKey)
        {
            var publicProtector = services.GetRequiredService<IDataProtectionProvider>().CreateProtector("PublicKeyProtector");
            var privateProtector = services.GetRequiredService<IDataProtectionProvider>().CreateProtector("PrivateKeyProtector");

            // Criptare e salvare la chiave pubblica e quella privata
            var protectedPublicKey = publicProtector.Protect(publicKey);
            var protectedPrivateKey = privateProtector.Protect(privateKey);

            AppSettingsUpdater updater = new AppSettingsUpdater("appsettings.json");
            if (updater.GetSetting("publicKey") == string.Empty)
            {
                updater.AddSetting("publicKey", protectedPublicKey);
            }
            if (updater.GetSetting("privateKey") == string.Empty)
            {
                updater.AddSetting("privateKey", protectedPrivateKey);
            }
        }
    }
}
