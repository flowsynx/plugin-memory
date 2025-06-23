using FlowSynx.Connectors.Storage.Memory.Services;
using FlowSynx.PluginCore;
using FlowSynx.Plugins.Memory.Models;
using FlowSynx.PluginCore.Helpers;
using FlowSynx.PluginCore.Extensions;

namespace FlowSynx.Plugins.Memory;

public class MemoryPlugin : IPlugin
{
    private IMemoryManager _manager = null!;
    private MemorySpecifications _s3Specifications = null!;
    private bool _isInitialized;

    public PluginMetadata Metadata
    {
        get
        {
            return new PluginMetadata
            {
                Id = Guid.Parse("ac220180-021e-4150-b0e1-c4d4bdbfb9f0"),
                Name = "Memory",
                CompanyName = "FlowSynx",
                Description = Resources.PluginDescription,
                Version = new PluginVersion(1, 0, 0),
                Category = PluginCategory.Storage,
                Authors = new List<string> { "FlowSynx" },
                Copyright = "© FlowSynx. All rights reserved.",
                Icon = "flowsynx.png",
                ReadMe = "README.md",
                RepositoryUrl = "https://github.com/flowsynx/plugin-memory",
                ProjectUrl = "https://flowsynx.io",
                Tags = new List<string>() { "flowsynx", "memory", "in-memory", "storage" },
            };
        }
    }

    public PluginSpecifications? Specifications { get; set; }
    public Type SpecificationsType => typeof(MemorySpecifications);

    public Task Initialize(IPluginLogger logger)
    {
        if (ReflectionHelper.IsCalledViaReflection())
            throw new InvalidOperationException(Resources.ReflectionBasedAccessIsNotAllowed);

        ArgumentNullException.ThrowIfNull(logger);
        _s3Specifications = Specifications.ToObject<MemorySpecifications>();
        _manager = new MemoryManager(logger, _s3Specifications.Bucket);
        _isInitialized = true;
        return Task.CompletedTask;
    }

    public Task<object?> ExecuteAsync(PluginParameters parameters, CancellationToken cancellationToken)
    {
        if (ReflectionHelper.IsCalledViaReflection())
            throw new InvalidOperationException(Resources.ReflectionBasedAccessIsNotAllowed);

        if (!_isInitialized)
            throw new InvalidOperationException($"Plugin '{Metadata.Name}' v{Metadata.Version} is not initialized.");

        var operationParameter = parameters.ToObject<OperationParameter>();
        var operation = operationParameter.Operation;

        if (OperationMap.TryGetValue(operation, out var handler))
        {
            return handler(parameters, cancellationToken);
        }

        throw new NotSupportedException($"Memory plugin: Operation '{operation}' is not supported.");
    }

    private Dictionary<string, Func<PluginParameters, CancellationToken, Task<object?>>> OperationMap => new(StringComparer.OrdinalIgnoreCase)
    {
        ["create"] = async (parameters, cancellationToken) => { await _manager.Create(parameters, cancellationToken); return null; },
        ["delete"] = async (parameters, cancellationToken) => { await _manager.Delete(parameters, cancellationToken); return null; },
        ["exist"] = async (parameters, cancellationToken) => await _manager.Exist(parameters, cancellationToken),
        ["list"] = async (parameters, cancellationToken) => await _manager.List(parameters, cancellationToken),
        ["purge"] = async (parameters, cancellationToken) => { await _manager.Purge(parameters, cancellationToken); return null; },
        ["read"] = async (parameters, cancellationToken) => await _manager.Read(parameters, cancellationToken),
        ["write"] = async (parameters, cancellationToken) => { await _manager.Write(parameters, cancellationToken); return null; },
    };

    public IReadOnlyCollection<string> SupportedOperations => OperationMap.Keys;
}