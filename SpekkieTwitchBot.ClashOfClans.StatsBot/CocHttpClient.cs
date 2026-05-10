using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.ClashOfClans.War;
using SpekkieClassLibrary.Constants;

namespace SpekkieTwitchBot.ClashOfClans.StatsBot;

public class CocHttpClient
{
    private readonly HttpClient _HttpClient;
    private string _ClashToken;
    private readonly string _TokenPath;
    private FileSystemWatcher? _TokenWatcher;
    private CancellationTokenSource? _WatcherDebounce;

    public CocHttpClient()
    {
        _HttpClient = new HttpClient();
        _TokenPath = $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}clash api token.txt";
        _ClashToken = ReadToken();

        #if DEBUG
                _ClashToken = ClashConstants.DebugApiToken;
                Console.WriteLine("Running in debug mode");
        #endif

        SetupAuth();
        StartTokenWatcher();
    }

    private string ReadToken()
    {
        if (!File.Exists(_TokenPath))
        {
            Console.WriteLine("[CoC] Warning: clash api token file not found — API requests will fail");
            return string.Empty;
        }

        string token = File.ReadAllText(_TokenPath).Trim();
        if (string.IsNullOrEmpty(token))
            Console.WriteLine("[CoC] Warning: clash api token is empty — API requests will fail");

        return token;
    }

    private void StartTokenWatcher()
    {
        string dir = Path.GetDirectoryName(_TokenPath)!;
        string file = Path.GetFileName(_TokenPath);

        _TokenWatcher = new FileSystemWatcher(dir, file)
        {
            NotifyFilter = NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };
        _TokenWatcher.Changed += OnTokenFileChanged;
    }

    private void OnTokenFileChanged(object sender, FileSystemEventArgs e)
    {
        _WatcherDebounce?.Cancel();
        _WatcherDebounce?.Dispose();
        _WatcherDebounce = new CancellationTokenSource();
        CancellationTokenSource cts = _WatcherDebounce;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, cts.Token);
                _ClashToken = ReadToken();
                SetupAuth();
                Console.WriteLine("[CoC] API token reloaded");
            }
            catch (OperationCanceledException) { }
        }, cts.Token);
    }

    private void SetupAuth()
    {
        _HttpClient.DefaultRequestHeaders.Accept.Clear();
        _HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _ClashToken);
    }

    public async Task<RunTimeWar?> FetchWar(string clanTag)
    {
        string url = $"{ClashConstants.ClanApiBaseUrl}{clanTag.Replace("#", "%23")}/currentwar";
        string json = await RetrieveWarData(url);
        return JsonConvert.DeserializeObject<RunTimeWar>(json);
    }

    public async Task<string> GetPlayerClan(string playerTag)
    {
        string url = ClashConstants.PlayerApiBaseUrl + playerTag.Replace("#", "%23");

        HttpResponseMessage response = await _HttpClient.GetAsync(url);
        string json = await response.Content.ReadAsStringAsync();
        JObject jsonObject = JObject.Parse(json);

        JToken? clanData = jsonObject["clan"];
        string clanTag = clanData?["tag"]?.ToString() ?? "";

        return clanTag;
    }

    private async Task<string> RetrieveWarData(string url)
    {
        try
        {
            HttpResponseMessage myWebResponse = await _HttpClient.GetAsync(url);
            Stream responseStream = await myWebResponse.Content.ReadAsStreamAsync();
            using StreamReader myStreamReader = new(responseStream, Encoding.Default);
            string json = await myStreamReader.ReadToEndAsync();

            return json;
        }
        catch (Exception e)
        {
            return "err " + e.Message;
        }
    }

    public static void LoadImage(string source, string destination)
    {
        File.Copy(source, destination, overwrite: true);
    }

    public async Task<byte[]> GetByteArrayAsync(string url)
    {
        try
        {
            HttpResponseMessage response = await _HttpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            byte[] content = await response.Content.ReadAsByteArrayAsync();

            return content;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error retrieving data: {ex.Message}");
            throw;
        }
    }
}
