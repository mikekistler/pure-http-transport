using ModelContextProtocol.Protocol;

namespace PureHttpMcpServer.Prompts;

public static class MockPrompts
{
    private static List<Prompt> _prompts = new List<Prompt>
        {
            new() { Name = "prompt1", Title = "Greeting", Description = "A pleasant greeting message." },
            new() { Name = "prompt2", Title = "Farewell", Description = "A warm farewell message." }
        };

    public static IEnumerable<Prompt> ListPrompts()
    {
        return _prompts;
    }

    public static Prompt? GetPrompt(string name)
    {
        return _prompts.FirstOrDefault(p => p.Name == name);
    }
}
