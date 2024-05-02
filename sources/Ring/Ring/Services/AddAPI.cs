using Newtonsoft.Json;
using Ring.Utils;

namespace Ring.Services;
public class AddAPI
{
    private readonly HttpClient _httpClient;
    private readonly string? _server;

    public AddAPI(HttpClient client)
    {
        _httpClient = client;
        _server = Environment.GetEnvironmentVariable("API_SERVER");
    }

}
