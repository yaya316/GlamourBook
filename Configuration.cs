using Dalamud.Configuration;
using Dalamud.Plugin;

namespace GlamourBook;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    /// <summary>在「查看信息」窗口下方显示记录按钮。</summary>
    public bool ShowExamineButton { get; set; } = true;

    /// <summary>记录时自动把「同为外观」的候选物品全部存下（关掉只存第一件）。</summary>
    public bool StoreAllCandidates { get; set; } = true;

    /// <summary>打开主窗口时自动重扫背包。</summary>
    public bool RescanOnOpen { get; set; } = true;

    /// <summary>主窗口默认排序：0 = 时间倒序，1 = 玩家名，2 = 世界。</summary>
    public int SortMode { get; set; }

    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;

    public void Initialize(IDalamudPluginInterface pi) => this.pluginInterface = pi;

    public void Save() => this.pluginInterface?.SavePluginConfig(this);
}
