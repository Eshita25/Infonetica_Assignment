namespace WorkflowEngine.Models;

public class Action
{
    public string Id { get; set; }

    // public string Description{ get; set; }
    public bool Enabled { get; set; }
    public List<string> FromStates { get; set; } = new();
    public string ToState { get; set; }
}
