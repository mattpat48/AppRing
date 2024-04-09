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
        var url = _server + "/api/v1/auth/verifycode";

        var toSend = new
        {
            Code = inserted,
            Number = await SecureStorage.GetAsync("PhoneNumber")
        };

        string jsonRequestBody = JsonConvert.SerializeObject(toSend);
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
