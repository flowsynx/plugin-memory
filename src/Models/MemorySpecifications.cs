using FlowSynx.PluginCore;

namespace FlowSynx.Plugins.Memory.Models;

public class MemorySpecifications : PluginSpecifications
{
    [RequiredMember]
    public string Bucket { get; set; } = string.Empty;
}