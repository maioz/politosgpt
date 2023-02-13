using System.Net;
using System.Net.Http.Json;

namespace PolitoGPT;

public class ChatGPT
{
    private HttpClient _http;
    const string ApiKey = "API_KEY";

    public ChatGPT()
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            UseCookies = true,
            CookieContainer = new CookieContainer(),
        };

        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.openai.com/v1/"),
        };

        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
    }

    public async Task<CompletionResponse> GetCompletion(string message)
    {
        var request = new CompletionRequest()
        {
            MaxTokens = 500,
            Model = "text-davinci-003",
            Prompt = message,
        };

        var response = await _http.PostAsJsonAsync("completions", request);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CompletionResponse>();

        return result;
    }
}