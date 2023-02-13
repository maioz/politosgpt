using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Web;

namespace PolitoGPT;

public class Polito
{
    private HttpClient _http;

    public Polito()
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            UseCookies = true,
            CookieContainer = new CookieContainer(),
        };

        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://forum.politz.com.br/"),
        };

        _http.DefaultRequestHeaders.TryAddWithoutValidation("authority", "forum.politz.com.br");
        _http.DefaultRequestHeaders.TryAddWithoutValidation("accept", "application/json, text/javascript, */*; q=0.01");
        _http.DefaultRequestHeaders.TryAddWithoutValidation("accept-language", "en-US,en;q=0.9,pt;q=0.8");
        _http.DefaultRequestHeaders.TryAddWithoutValidation("cookie", "POLITZ_COOKIES_GOES_HERE");
        _http.DefaultRequestHeaders.TryAddWithoutValidation("sec-ch-ua", "\"Google Chrome\";v=\"105\", \"Not)A;Brand\";v=\"8\", \"Chromium\";v=\"105\"");
        _http.DefaultRequestHeaders.TryAddWithoutValidation("sec-ch-ua-mobile", "?0");
        _http.DefaultRequestHeaders.TryAddWithoutValidation("sec-ch-ua-platform", "\"Linux\"");
        _http.DefaultRequestHeaders.TryAddWithoutValidation("sec-fetch-dest", "empty");
        _http.DefaultRequestHeaders.TryAddWithoutValidation("sec-fetch-mode", "cors");
        _http.DefaultRequestHeaders.TryAddWithoutValidation("sec-fetch-site", "same-origin");
        _http.DefaultRequestHeaders.TryAddWithoutValidation("user-agent", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/105.0.0.0 Safari/537.36");
        _http.DefaultRequestHeaders.TryAddWithoutValidation("x-requested-with", "XMLHttpRequest");
    }

    static NotificationType TypeFromString(string type)
    {
        return Enum.Parse<NotificationType>(type, true);
    }

    internal async Task<Notification[]> GetNotifications()
    {
        var response = await _http.GetAsync("account/alerts?skip_mark_read=1&skip_summarize=1&show_only=unread");

        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();

        var cleanHtml = ClearHtml(html);

        var pattern = "<li\\sdata-alert-id=\\\"([0-9]+)\\\".+?<a\\shref=\\\"/members.+?username.+?data-user-id=\\\"([0-9]+)\\\".+?>(.+?)<\\/a>.+?(mencionou|citou).+?<a href=\\\"/posts/([0-9]+)/\\\".+?</li>";

        var matchs = cleanHtml.Matchs(pattern);

        var alerts = matchs.Select(m => new Notification()
        {
            AlertId = m.Groups[1].Value,
            UserId = m.Groups[2].Value,
            UserName = m.Groups[3].Value,
            Type = TypeFromString(m.Groups[4].Value),
            PostId = m.Groups[5].Value,
        });

        return alerts.ToArray();
    }

    internal async Task<Post> GetPostValue(string postId, NotificationType type)
    {
        var threadId = await GetThreadIdForPost(postId);
        var response = await _http.GetAsync($"threads/{threadId}/reply?quote={postId}");

        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();

        var cleanHtml = ClearHtml(html);

        string token = GetToken(cleanHtml);

        string postBbcodeDecoded = GetPostBbCode(cleanHtml);

        string postHtmlDecode = GetPostHtml(cleanHtml);

        string content = ExtractPostContentFromBbCode(postBbcodeDecoded, type);

        string citationContent = type == NotificationType.Citou
            ? (await GetMentionContent(postId))
            : String.Empty;

        string finalContent = MergeContent(content, citationContent, type);

        return new Post()
        {
            FinalContent = finalContent,
            PostHtmlDecode = postHtmlDecode,
            Token = token,
            ThreadId = threadId
        };
    }

    private static string MergeContent(string content, string citationContent, NotificationType type)
    {
        if(type == NotificationType.Mencionou)
            return content;

        var builder = new StringBuilder();
        builder.Append("Seu nome é ChatGPT e você responde perguntas de forma clara e objetiva. \n\n");
        builder.Append($"Você: {citationContent}. \n\n");
        builder.Append($"{content}");

        return builder.ToString();
    }

    private async Task<string> GetMentionContent(string postId)
    {
        var response = await _http.GetAsync($"posts/{postId}/");

        response.EnsureSuccessStatusCode();

        var regex = $"<article.+?data-content=\\\"post-{postId}\\\".+?>.+?<blockquote.+?data-source=\\\"post:\\s([0-9]+).+?\\\".+?\\/blockquote>.+?<\\/article>";

        var html = await response.Content.ReadAsStringAsync();

        var cleanHtml = ClearHtml(html);

        var mentionPostId = cleanHtml.Match(regex).Groups[1].Value;

        var post = await GetPost(mentionPostId, GetToken(cleanHtml));

        return GetValueFromQuote(post.quote);

    }

    private static string GetValueFromQuote(string quote)
    {
        var postValue = quote.Match("\\[QUOTE=.+?\\](.+?)\\[\\/QUOTE\\]")
            .Groups[1].Value;

        postValue = postValue.Replace("\n", String.Empty);

        return postValue;
    }

    private static string GetToken(string cleanHtml)
    {
        var tokenRegex = "<input type=\"hidden\" name=\"_xfToken\" value=\"(.+?)\"";
        var token = cleanHtml.Match(tokenRegex).Groups[1].Value;
        return token;
    }

    private static string GetPostHtml(string cleanHtml)
    {
        var regexHtml = "<textarea\\sname=\\\"message_html\\\".+?>(.+?)<\\/textarea>";
        var postHtmlMatch = cleanHtml.Match(regexHtml);
        var postHtml = postHtmlMatch.Groups[1].Value;

        var postHtmlDecode = HttpUtility.HtmlDecode(postHtml);
        return postHtmlDecode;
    }

    private static string GetPostBbCode(string cleanHtml)
    {
        var regexBbcode = "<textarea name=\\\"message\\\".+?>(.+?)<\\/textarea>.+?<\\/noscript>";
        var post = cleanHtml.Match(regexBbcode);
        var postBbcode = post.Groups[1].Value;
        var postBbcodeDecoded = HttpUtility.HtmlDecode(postBbcode);
        return postBbcodeDecoded;
    }

    private static string ExtractPostContentFromBbCode(string postBbcodeDecoded, NotificationType type)
    {
        var mentionTag = "[USER=19285]@ChatGPT[/USER]";

        if(type == NotificationType.Mencionou)
        {
            postBbcodeDecoded = postBbcodeDecoded.Replace(mentionTag, "ChatGPT",
                StringComparison.OrdinalIgnoreCase);
        }

        return GetValueFromQuote(postBbcodeDecoded);
    }

    private async Task<PostResponse> GetPost(string postId, string xfToken)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("_xfToken", xfToken),
            new KeyValuePair<string, string>("_xfResponseType", "json")
        });

        var response = await _http.PostAsync($"posts/{postId}/quote", content);
        response.EnsureSuccessStatusCode();

        var post = await response.Content.ReadFromJsonAsync<PostResponse>()
            ?? throw new Exception("Invalid post response with 20X");

        return post;
    }

    internal async Task Reply(string threadId, string xfToken, string answer)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("message_html", answer),
            new KeyValuePair<string, string>("_xfToken", xfToken),
            new KeyValuePair<string, string>("_xfResponseType", "json")
        });

        var response = await _http.PostAsync($"/threads/{threadId}/add-reply", content);

        response.EnsureSuccessStatusCode();
    }

    private static string ClearHtml(string html)
    {
        return html.Replace("\\t", string.Empty)
            .Replace("\\n", string.Empty)
            .Replace("\\\"", "\"");
    }

    private async Task<string> GetThreadIdForPost(string postId)
    {
        var response = await _http.GetAsync($"posts/{postId}/");

        response.EnsureSuccessStatusCode();

        var regex = "threads\\/(.+?)\\/";
        var uri = response.RequestMessage?.RequestUri?.AbsolutePath ?? string.Empty;

        var match = uri.Match(regex);

        return match.Groups[1].Value;
    }

    internal async Task MarkAlertAsRead(string alertId, string xfToken)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("_xfToken", xfToken),
            new KeyValuePair<string, string>("_xfResponseType", "json")
        });

        var response = await _http.PostAsync($"account/alert/{alertId}/read", content);

        response.EnsureSuccessStatusCode();
    }

    public async Task MarkAlertAsUnread(string alertId, string xfToken)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("_xfToken", xfToken),
            new KeyValuePair<string, string>("_xfResponseType", "json")
        });

        var response = await _http.PostAsync($"account/alert/{alertId}/unread", content);

        response.EnsureSuccessStatusCode();
    }
}