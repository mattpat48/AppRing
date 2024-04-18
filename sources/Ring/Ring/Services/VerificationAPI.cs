using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Ring.Utils;

namespace Ring.Services;

public class VerificationAPI
{

    private readonly HttpClient _httpClient;
    private readonly string _server;

    public VerificationAPI(HttpClient client)
    {
        _httpClient = client;
        _server = Environment.GetEnvironmentVariable("API_SERVER");
    }

    public async Task<string> SendSMS(string phoneNumber)
    {
        if (phoneNumber == null)
        {
            return "Phone number not found";
        }
        else
        {
            var url = _server + "/api/v1/auth/sendcode";

            string key = await SecureStorage.GetAsync("ServerKey");

            var stringContent = CryptographyTools.EncryptString(await SecureStorage.GetAsync("ServerKey"), JsonConvert.SerializeObject(phoneNumber));

            HttpResponseMessage response = await _httpClient.PostAsync(url, stringContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return responseContent;
            }
            else
            {
                return "success";
            }
        }
    }

    public async Task<string> VerifyCode(string inserted)
    {
        var url = _server + "/api/v1/auth/verifycode";

        var toSend = new
        {
            Code = inserted,
            Number = await SecureStorage.GetAsync("PhoneNumber")
        };

        string key = await SecureStorage.GetAsync("ServerKey");
        
        if (key == null)
        {
            return "Server key not found";
        }
        var stringContent = CryptographyTools.EncryptString(key, JsonConvert.SerializeObject(toSend));

        HttpResponseMessage response = await _httpClient.PostAsync(url, stringContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return responseContent;
        }
        else
        {
            return "success";
        }
    }
}
