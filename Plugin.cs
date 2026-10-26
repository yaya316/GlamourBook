using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace GlamourBook;

public sealed class Plugin : IDalamudPlugin
{
    public static Configuration Config { get; private set; } = null!;

    public string Name => "GlamourBook";

    private readonly WindowSystem windowSystem = new("GlamourBook");
    private readonly RecordStore store;
    private readonly Ownership ownership = new();
    private readonly MainWindow mainWindow;
    private readonly ConfigWindow configWindow;
    private readonly ExamineOverlay overlay = new();

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();

        Config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Config.Initialize(pluginInterface);

        this.store = new RecordStore();
        this.mainWindow = new MainWindow(this.store, this.ownership);
        this.configWindow = new ConfigWindow();
        this.windowSystem.AddWindow(this.mainWindow);
        this.windowSystem.AddWindow(this.configWindow);

        this.overlay.OnCapture = this.Capture;

        Service.Interface.UiBuilder.Draw += this.Draw;
        Service.Interface.UiBuilder.OpenMainUi += this.OpenMainUi;
        Service.Interface.UiBuilder.OpenConfigUi += this.OpenConfigUi;
        Service.Framework.Update += this.OnUpdate;

        Service.Commands.AddHandler("/glamourbook", new CommandInfo(this.OnCommand)
        {
            HelpMessage = "打开幻化图鉴",
        });
        Service.Commands.AddHandler("/gb", new CommandInfo(this.OnCommand)
        {
            HelpMessage = "打开幻化图鉴",
        });

        Service.Log.Information("GlamourBook 已加载");

        Service.Commands.AddHandler("/gbdump", new CommandInfo((_, _) =>
        {
            DebugDump.Write();
            Service.Chat.Print($"[幻化图鉴] 结构转储已写入：{DebugDump.FilePath}");
        })
        {
            HelpMessage = "导出结构字段名到文件（排错用）",
        });

        Service.Commands.AddHandler("/gbpos", new CommandInfo((_, _) => DebugDump.WriteWindowMetrics())
        {
            HelpMessage = "导出查看信息窗口的几何数据（校准按钮位置用）",
        });

        Service.Commands.AddHandler("/gbcat", new CommandInfo((_, _) => DebugDump.WriteCategoryReport())
        {
            HelpMessage = "导出槽位分类对照表（排错用）",
        });

        DebugDump.Write();
    }

    private void Draw()
    {
        this.overlay.Draw();
        this.windowSystem.Draw();
    }

    private void OpenMainUi()
    {
        this.mainWindow.IsOpen = true;
    }

    private void OpenConfigUi()
    {
        this.configWindow.IsOpen = true;
    }

    private void OnUpdate(IFramework framework)
    {
        TryOnService.Tick();
    }

    private void Capture()
    {
        if (!GlamourCapture.TryCapture(out var record, out var error) || record is null)
        {
            Service.Chat.PrintError($"[幻化图鉴] 记录失败：{error}");
            return;
        }

        this.store.Add(record);
        Service.Chat.Print($"[幻化图鉴] 已记录 {record.DisplayName}，共 {record.Pieces.Count} 个部位。用 /gb 查看。");
        this.mainWindow.IsOpen = true;
    }

    private void OnCommand(string command, string args)
    {
        this.mainWindow.IsOpen = true;
    }

    public void Dispose()
    {
        Service.Interface.UiBuilder.Draw -= this.Draw;
        Service.Interface.UiBuilder.OpenMainUi -= this.OpenMainUi;
        Service.Interface.UiBuilder.OpenConfigUi -= this.OpenConfigUi;
        Service.Framework.Update -= this.OnUpdate;

        Service.Commands.RemoveHandler("/glamourbook");
        Service.Commands.RemoveHandler("/gb");
        Service.Commands.RemoveHandler("/gbdump");
        Service.Commands.RemoveHandler("/gbpos");
        Service.Commands.RemoveHandler("/gbcat");

        this.windowSystem.RemoveAllWindows();
        Config.Save();
    }
}
