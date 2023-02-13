using System.Text.Json.Serialization;
public class CompletionRequest
{
    [JsonPropertyName("model")]
    public string? Model
    {
        get;
        set;
    }
    [JsonPropertyName("prompt")]
    public string? Prompt
    {
        get;
        set;
    }
    [JsonPropertyName("max_tokens")]
    public int? MaxTokens
    {
        get;
        set;
    }
}

public class CompletionResponse
{
    [JsonPropertyName("choices")]
    public List<ChatGPTChoice>? Choices
    {
        get;
        set;
    }

    public string GetFirstChoiceText() => Choices.First().Text;
}

public class ChatGPTChoice
{
    [JsonPropertyName("text")]
    public string? Text
    {
        get;
        set;
    }
}