namespace SpekkieTwitchBot.Web;

public class CustomSpotifyHttpClient
{
    private readonly HttpClient _Client;

    public CustomSpotifyHttpClient(HttpClient client)
    {
        _Client = client;
        Setup();
    }
    
    private void Setup()
    {
        
    }

    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        return await _Client.GetAsync(url);
    }

    public async Task<HttpResponseMessage> PatchAsync(string url)
    {
        var content = new StringContent("");
        return await _Client.PatchAsync(url, content);
    }

    public async Task<HttpResponseMessage> PostAsync(string url)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "", "" }
        });
        return await _Client.PostAsync(url, content);
    }
}