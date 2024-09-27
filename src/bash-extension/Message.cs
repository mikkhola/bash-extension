using System.Text.Json.Serialization;

public record Message
{
    public required string Role { get; set; }
    public required string Content { get; set; }

    [JsonPropertyName("tool_calls")]
    public List<ToolCall>? ToolCalls { get; set; }
}
