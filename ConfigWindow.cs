using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace GlamourBook;

public sealed class ConfigWindow : Window
{
    public ConfigWindow()
        : base("幻化图鉴 · 设置###GlamourBookConfig")
    {
        this.Size = new Vector2(440f, 260f);
        this.SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        var config = Plugin.Config;

        ImGui.TextDisabled("界面");
        var showButton = config.ShowExamineButton;
        if (ImGui.Checkbox("在「查看信息」窗口显示记录按钮", ref showButton))
        {
            config.ShowExamineButton = showButton;
            config.Save();
        }

        var rescan = config.RescanOnOpen;
        if (ImGui.Checkbox("打开主窗口时自动重扫背包", ref rescan))
        {
            config.RescanOnOpen = rescan;
            config.Save();
        }

        ImGui.Spacing();
        ImGui.TextDisabled("记录");
        var all = config.StoreAllCandidates;
        if (ImGui.Checkbox("记录时保存备用候选信息", ref all))
        {
            config.StoreAllCandidates = all;
            config.Save();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextDisabled("数据说明");
        ImGui.TextWrapped("幻化台与收藏柜的内容不会一直留驻内存：游戏只在你打开过对应界面之后才会加载。");
        ImGui.TextWrapped("所以需要先在游戏里看过一次，再回主窗口点「扫描幻化台」或「扫描收藏柜」。");

        ImGui.Spacing();
        if (ImGui.Button("关闭"))
        {
            this.IsOpen = false;
        }
    }
}
