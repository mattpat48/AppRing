using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Ring.Services;

public class VerificationAPI
{

    private readonly HttpClient _httpClient;

    public VerificationAPI(HttpClient client)
    {
        _httpClient = client;
    }

    public async Task<string> SendSMS(string phoneNumber)
    {
        if (phoneNumber == null)
        {
            return "Phone number not found";
        }
        else
        {
            var url = "https://localhost:7046/api/v1/auth/sendcode";

            string jsonRequestBody = JsonConvert.SerializeObject(phoneNumber);
            var stringContent = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

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

    public async Task<string> CheckCode(string inserted)
    {
        var url = "https://localhost:7046/api/v1/auth/verifycode";

        string jsonRequestBody = JsonConvert.SerializeObject(inserted);
        var stringContent = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

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
