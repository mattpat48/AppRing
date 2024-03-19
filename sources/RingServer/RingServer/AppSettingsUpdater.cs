using System.IO;
using Newtonsoft.Json.Linq;

namespace RingServer;
public class AppSettingsUpdater
{
    private readonly string _filePath;

    public AppSettingsUpdater(string filePath)
    {
        _filePath = filePath;
    }

    public void UpdateSetting(string key, string value)
    {
        var json = JObject.Parse(File.ReadAllText(_filePath));
        json[key] = value;
        File.WriteAllText(_filePath, json.ToString());
    }

    public void RemoveSetting(string key)
    {
        var json = JObject.Parse(File.ReadAllText(_filePath));
        json.Remove(key);
        File.WriteAllText(_filePath, json.ToString());
    }

    public void AddSetting(string key, string value)
    {
        var json = JObject.Parse(File.ReadAllText(_filePath));
        if (!json.ContainsKey(key))
        {
            json[key] = value;
            File.WriteAllText(_filePath, json.ToString());
        }
    }

    public string GetSetting(string key)
    {
        var json = JObject.Parse(File.ReadAllText(_filePath));
        if (json.ContainsKey(key))
        {
            return json[key].ToString();
        }
        return string.Empty;
    }
}
