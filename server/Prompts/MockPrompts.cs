using ModelContextProtocol.Protocol;
using PureHttpTransport;

namespace PureHttpMcpServer.Prompts;

public class MockPrompts : IMockPrompts
{
    private static List<Prompt> _prompts = new List<Prompt>
        {
            new() { Name = "prompt1", Title = "Greeting", Description = "A pleasant greeting message." },
            new() { Name = "prompt2", Title = "Farewell", Description = "A warm farewell message." }
        };

    public IEnumerable<Prompt> ListPrompts()
    {
        return _prompts;
    }

    public Prompt? GetPrompt(string name)
    {
        return _prompts.FirstOrDefault(p => p.Name == name);
    }
}
