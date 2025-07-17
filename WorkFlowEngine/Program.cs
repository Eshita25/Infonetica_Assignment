using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using WorkflowEngine.Models;
using WorkflowEngine.Store;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null;
});
var app = builder.Build();

var store = new MemoryStore();
//test
app.MapGet("/", () => "server is running");

// Create workflow definition
app.MapPost("/workflows", (WorkflowDefinition def) =>
{
    if (store.Definitions.ContainsKey(def.Id))
        return Results.BadRequest("Workflow already exists.");
    if (def.States.Count(s => s.IsInitial) != 1)
        return Results.BadRequest("Atleast one initial state expected.");

    store.Definitions[def.Id] = def;
    return Results.Ok("Workflow created Successfully.");
});
// TODO: Add validations


// Get workflow definition
app.MapGet("/workflows/{id}", (string id) =>
{
    return store.Definitions.TryGetValue(id, out var def)
        ? Results.Ok(def)
        : Results.NotFound("Workflow not found.");
});

// Create instance
app.MapPost("/instances", (string workflowId) =>
{
    if (!store.Definitions.TryGetValue(workflowId, out var def))
        return Results.NotFound("Workflow definition not found.");

    var initial = def.States.First(s => s.IsInitial);
    var instance = new WorkflowInstance
    {
        Id = Guid.NewGuid().ToString(),
        DefinitionId = workflowId,
        CurrentState = initial.Id
    };
    store.Instances[instance.Id] = instance;
    return Results.Ok(instance);
});

// Execute action
app.MapPost("/instances/{id}/actions", (string id, string actionId) =>
{
    if (!store.Instances.TryGetValue(id, out var instance))
        return Results.NotFound("Instance not found.");
    var def = store.Definitions[instance.DefinitionId];

    var action = def.Actions.FirstOrDefault(a => a.Id == actionId);
    if (action == null || !action.Enabled)
        return Results.BadRequest("Invalid or disabled action.");
    if (!action.FromStates.Contains(instance.CurrentState))
        return Results.BadRequest("Action not valid from current state.");

    var targetState = def.States.FirstOrDefault(s => s.Id == action.ToState && s.Enabled);
    if (targetState == null)
        return Results.BadRequest("Target state is invalid or disabled.");
    if (def.States.First(s => s.Id == instance.CurrentState).IsFinal)
        return Results.BadRequest("Cannot act from a final state.");

    instance.CurrentState = targetState.Id;
    instance.History.Add((action.Id, DateTime.UtcNow));
    return Results.Ok(instance);
});

// Get instance state
app.MapGet("/instances/{id}", (string id) =>
{
    return store.Instances.TryGetValue(id, out var inst)
        ? Results.Ok(inst)
        : Results.NotFound("Instance not found.");
});

app.Run();
