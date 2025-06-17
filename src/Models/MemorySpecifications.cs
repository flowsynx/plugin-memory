using FlowSynx.PluginCore;

namespace FlowSynx.Plugins.Memory.Models;

internal class MemorySpecifications : PluginSpecifications
{
    [RequiredMember]
    public string Bucket { get; set; } = string.Empty;
}