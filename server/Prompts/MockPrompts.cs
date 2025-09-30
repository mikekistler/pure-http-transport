using ModelContextProtocol.Protocol;
using PureHttpTransport;

namespace PureHttpMcpServer.Prompts;

public class MockPrompts : IMockPrompts
{
    private static List<Prompt> _prompts = new List<Prompt>
        {
            new() { Name = "prompt1", Title = "Greeting", Description = "A pleasant greeting message." },
            new() { Name = "prompt2", Title = "Farewell", Description = "A warm farewell message." },
            new() {
                Name = "code_review",
                Title = "Request Code Review",
                Description = "Asks the LLM to analyze code quality and suggest improvements.",
                Arguments = new List<PromptArgument>
                {
                    new PromptArgument
                    {
                        Name = "code",
                        Description = "The code to review",
                        Required = true
                    }
                }
            }
        };

    private static Dictionary<string, string> _templates = new Dictionary<string, string>
    {
        { "prompt1", "Hello! How can I assist you today?" },
        { "prompt2", "Goodbye! Have a great day!" },
        { "code_review", "Please review the following code:\n{code}\nProvide feedback on its quality and suggest improvements." }
    };

    public IEnumerable<Prompt> ListPrompts()
    {
        return _prompts;
    }

    public GetPromptResult? GetPrompt(GetPromptRequestParams requestParams)
    {
        var prompt = _prompts.FirstOrDefault(p => p.Name == requestParams.Name);
        if (prompt == null) return null;

        var text = _templates.ContainsKey(requestParams.Name) ? _templates[requestParams.Name] : prompt.Description!;

        // If the prompt has arguments, include them in the text
        if (prompt.Arguments?.Count > 0)
        {
            // replace {arg} in the text with <arg>
            foreach (var arg in prompt.Arguments)
            {
                // Check if the argument is present in the request
                if (requestParams.Arguments?.TryGetValue(arg.Name, out var value) == true)
                {
                    text = text.Replace("{" + arg.Name + "}", value.ToString());
                }
            }
        }

        return new GetPromptResult
        {
            Description = prompt.Description,
            Messages = new List<PromptMessage>
            {
                new PromptMessage
                {
                    Role = Role.Assistant,
                    Content = new TextContentBlock
                    {
                        Text = text
                    }
                }
            }
        };
    }
}
