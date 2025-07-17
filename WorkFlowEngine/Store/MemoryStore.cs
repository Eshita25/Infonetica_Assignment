using WorkflowEngine.Models;

namespace WorkflowEngine.Store;

public class MemoryStore
{
    //database
    public Dictionary<string, WorkflowDefinition> Definitions { get; set; } = new();
    public Dictionary<string, WorkflowInstance> Instances { get; set; } = new();
}
