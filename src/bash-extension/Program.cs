using Microsoft.AspNetCore.Mvc;
using Octokit;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "This works!");

string yourGitHubAppName = "bash-extension";
string githubCopilotCompletionsUrl = 
    "https://api.githubcopilot.com/chat/completions";

app.MapPost("/agent", async (
    [FromHeader(Name = "X-GitHub-Token")] string githubToken, 
    [FromBody] Request userRequest) =>
{

var octokitClient = 
    new GitHubClient(
        new Octokit.ProductHeaderValue(yourGitHubAppName))
{
    Credentials = new Credentials(githubToken)
};
var user = await octokitClient.User.Current();

userRequest.Messages.Insert(0, new Message
{
    Role = "system",
    Content = 
        "You are a Bash shell assistant that replies to " +
        "user messages with only the most relevant Bash command, nothing else. Just return the command."
});

var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", githubToken);
userRequest.Stream = true;

var copilotLLMResponse = await httpClient.PostAsJsonAsync(
    githubCopilotCompletionsUrl, userRequest);

// Read and print the content of copilotLLMResponse
var responseContent = await copilotLLMResponse.Content.ReadAsStringAsync();
Console.WriteLine("copilotLLMResponse content:");


using var stream = await copilotLLMResponse.Content.ReadAsStreamAsync();
await foreach (SseItem<string> item in SseParser.Create(stream).EnumerateAsync())
{
    Console.WriteLine(item.Data);
}



var responseStream = 
    await copilotLLMResponse.Content.ReadAsStreamAsync();
return Results.Stream(responseStream, "application/json");

});

app.MapGet("/callback", () => "You may close this tab and " + 
    "return to GitHub.com (where you should refresh the page " +
    "and start a fresh chat). If you're using VS Code or " +
    "Visual Studio, return there.");

app.Run();

static ChatMessageFunctionCall GetFunctionCall(ChatCompletionsResponse res)
{
    if (res.Choices == null || res.Choices.Count == 0)
    {
        return null;
    }

    var firstChoice = res.Choices[0];
    if (firstChoice.Message == null || firstChoice.Message.ToolCalls == null || firstChoice.Message.ToolCalls.Count == 0)
    {
        return null;
    }

    var funcCall = firstChoice.Message.ToolCalls[0].Function;
    return funcCall;
}

public class ChatCompletionsResponse
{
    [JsonPropertyName("choices")]
    public List<Choice>? Choices { get; set; }
}

public class Choice
{
    [JsonPropertyName("message")]
    public Message? Message { get; set; }
}

public class ToolCall
{
    [JsonPropertyName("function")]
    public ChatMessageFunctionCall? Function { get; set; }
}

public class ChatMessageFunctionCall
{
    // Define properties of ChatMessageFunctionCall as needed
}
