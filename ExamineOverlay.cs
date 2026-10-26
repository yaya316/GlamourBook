using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace GlamourBook;

/// <summary>
/// 在「查看信息」窗口标题栏右侧贴一个按钮。
///
/// 不去改游戏原生 Atk 节点（版本一更新就崩），而是每帧读窗口位置，
/// 在上面浮一个自绘按钮：纵向金属渐变 + 金色描边 + 顶部高光 + 文字投影，
/// 尽量贴近游戏原生按钮的质感。
/// </summary>
public sealed class ExamineOverlay
{
    private const string InspectAddonName = "CharacterInspect";
    private const float ButtonWidth = 104f;
    private const float ButtonHeight = 20f;

    public Action? OnCapture { get; set; }

    public void Draw()
    {
        if (!Plugin.Config.ShowExamineButton)
        {
            return;
        }

        var addon = Service.GameGui.GetAddonByName(InspectAddonName);
        if (addon.IsNull || !addon.IsVisible)
        {
            return;
        }

        // 实测（/gbpos）：X/Y/Scale 可信，Width/Height 返回未初始化的垃圾值，
        // 真实尺寸在 ScaledWidth/ScaledHeight 里。
        var origin = addon.Position;
        var right = origin.X + addon.ScaledWidth;
        var position = new Vector2(right - ButtonWidth - 30f, origin.Y + 3f);

        ImGui.SetNextWindowPos(position, ImGuiCond.Always);

        const ImGuiWindowFlags flags =
            ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.AlwaysAutoResize
            | ImGuiWindowFlags.NoFocusOnAppearing
            | ImGuiWindowFlags.NoBackground
            | ImGuiWindowFlags.NoNav;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);

        if (ImGui.Begin("##GlamourBookCapture", flags))
        {
            if (NativeButton("幻化图鉴", new Vector2(ButtonWidth, ButtonHeight)))
            {
                this.OnCapture?.Invoke();
            }

            if (ImGui.IsItemHovered())
            {
                var player = GlamourCapture.CurrentTarget();
                ImGui.SetTooltip(player != null
                    ? $"记录 {player.Name} 的幻化外观"
                    : "记录这个玩家的幻化外观");
            }
        }

        ImGui.End();

        ImGui.PopStyleVar(2);
    }

    /// <summary>自绘一个游戏质感的按钮：金属渐变 + 金边 + 高光 + 文字阴影。</summary>
    private static bool NativeButton(string label, Vector2 size)
    {
        var min = ImGui.GetCursorScreenPos();
        var max = min + size;

        ImGui.InvisibleButton(label, size);
        var clicked = ImGui.IsItemClicked();
        var hovered = ImGui.IsItemHovered();

        var draw = ImGui.GetWindowDrawList();

        var top = hovered
            ? new Vector4(0.60f, 0.66f, 0.75f, 1f)
            : new Vector4(0.47f, 0.53f, 0.62f, 1f);
        var bottom = hovered
            ? new Vector4(0.26f, 0.31f, 0.40f, 1f)
            : new Vector4(0.17f, 0.21f, 0.28f, 1f);

        draw.AddRectFilledMultiColor(
            min,
            max,
            ImGui.ColorConvertFloat4ToU32(top),
            ImGui.ColorConvertFloat4ToU32(top),
            ImGui.ColorConvertFloat4ToU32(bottom),
            ImGui.ColorConvertFloat4ToU32(bottom));

        // 金色外描边
        draw.AddRect(
            min,
            max,
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.72f, 0.64f, 0.44f, 0.95f)),
            3f,
            ImDrawFlags.None,
            1.2f);

        // 顶部内高光
        draw.AddLine(
            min + new Vector2(1.5f, 1.5f),
            new Vector2(max.X - 1.5f, min.Y + 1.5f),
            ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 0.22f)),
            1f);

        var textSize = ImGui.CalcTextSize(label);
        var textPos = new Vector2(
            min.X + ((size.X - textSize.X) * 0.5f),
            min.Y + ((size.Y - textSize.Y) * 0.5f));

        draw.AddText(textPos + new Vector2(1f, 1f), ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.8f)), label);
        draw.AddText(textPos, ImGui.ColorConvertFloat4ToU32(new Vector4(0.96f, 0.94f, 0.88f, 1f)), label);

        return clicked;
    }
}
