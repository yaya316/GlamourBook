using System.Reflection;
using System.Text;
using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;

namespace GlamourBook;

/// <summary>
/// 运行时把关键结构的真实成员名导出成文本，用于对齐那些「文档里没写」的字段。
/// 结果写到插件配置目录下的 struct-dump.txt。
/// </summary>
public static class DebugDump
{
    public static string FilePath =>
        Path.Combine(Service.Interface.GetPluginConfigDirectory(), "struct-dump.txt");

    public static void Write()
    {
        try
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== AgentInspect.ItemData ===");
            DumpType(sb, typeof(AgentInspect.ItemData));
            sb.AppendLine();

            sb.AppendLine("=== AgentInspect ===");
            DumpType(sb, typeof(AgentInspect));
            sb.AppendLine();

            sb.AppendLine("=== AgentTryon.TryOn ===");
            DumpMethod(sb, typeof(AgentTryon), "TryOn");
            sb.AppendLine();

            sb.AppendLine("=== AgentTryon 字段/属性 ===");
            DumpType(sb, typeof(AgentTryon));
            sb.AppendLine();

            sb.AppendLine("=== AgentTryon 相关方法 ===");
            foreach (var method in typeof(AgentTryon).GetMethods(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                if (!method.Name.Contains("TryOn") && !method.Name.Contains("Show") && !method.Name.Contains("Update"))
                {
                    continue;
                }

                var parameters = string.Join(
                    ", ",
                    method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                sb.AppendLine($"{method.ReturnType.Name} {method.Name}({parameters})");
            }

            sb.AppendLine();
            sb.AppendLine("=== AgentTryon.TryOnItem ===");
            DumpType(sb, typeof(AgentTryon.TryOnItem));
            sb.AppendLine();

            var colorType = typeof(AgentInspect.ItemData).GetField("Color")?.FieldType;
            if (colorType != null)
            {
                sb.AppendLine("=== ItemData.Color ===");
                DumpType(sb, colorType);
                sb.AppendLine();
            }

            sb.AppendLine("=== EquipmentModelId ===");
            DumpType(sb, typeof(EquipmentModelId));
            sb.AppendLine();

            sb.AppendLine("=== DrawDataContainer ===");
            DumpType(sb, typeof(DrawDataContainer));
            sb.AppendLine();

            sb.AppendLine("=== Item (Lumina) 图标/染色/模型相关属性 ===");
            foreach (var p in typeof(Item).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (p.Name.Contains("Icon") || p.Name.Contains("Stain") || p.Name.Contains("Model"))
                {
                    sb.AppendLine($"{p.Name} : {p.PropertyType}");
                }
            }

            File.WriteAllText(FilePath, sb.ToString());
            Service.Log.Information("结构转储已写入 {Path}", FilePath);
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex, "写结构转储失败");
        }
    }

    /// <summary>把「查看信息」窗口的真实几何数据写出来，用于校准按钮位置。</summary>
    public static void WriteWindowMetrics()
    {
        try
        {
            var sb = new StringBuilder();
            var addon = Service.GameGui.GetAddonByName("CharacterInspect");

            if (addon.IsNull)
            {
                sb.AppendLine("addon: null（窗口没开）");
            }
            else
            {
                sb.AppendLine($"IsVisible : {addon.IsVisible}");
                sb.AppendLine($"X         : {addon.X}");
                sb.AppendLine($"Y         : {addon.Y}");
                sb.AppendLine($"Width     : {addon.Width}");
                sb.AppendLine($"Height    : {addon.Height}");
                sb.AppendLine($"Scale     : {addon.Scale}");
                sb.AppendLine($"ScaledW   : {addon.ScaledWidth}");
                sb.AppendLine($"ScaledH   : {addon.ScaledHeight}");
                sb.AppendLine($"Position  : {addon.Position}");
                sb.AppendLine($"Size      : {addon.Size}");
            }

            var io = ImGui.GetIO();
            sb.AppendLine($"ImGuiDisp : {io.DisplaySize}");

            var path = Path.Combine(Service.Interface.GetPluginConfigDirectory(), "window-metrics.txt");
            File.WriteAllText(path, sb.ToString());
            Service.Chat.Print($"[幻化图鉴] 窗口数据已写入：{path}");
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex, "写窗口数据失败");
        }
    }

    /// <summary>导出「部位 → 物品 → 算出的槽分类值」，以及分类表本身的槽位标志。</summary>
    public static void WriteCategoryReport()
    {
        try
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== EquipSlotCategory 类型的真实字段 ===");
            DumpType(sb, typeof(EquipSlotCategory));
            sb.AppendLine();

            sb.AppendLine("=== EquipSlotCategory 表（rowId → 槽位标志）===");
            foreach (var row in Service.Data.GetExcelSheet<EquipSlotCategory>())
            {
                sb.AppendLine(
                    $"row {row.RowId,3} : Head={row.Head} Body={row.Body} Gloves={row.Gloves} Legs={row.Legs} Feet={row.Feet} " +
                    $"Ears={row.Ears} Neck={row.Neck} Wrists={row.Wrists} MainHand={row.MainHand} OffHand={row.OffHand}");
            }

            sb.AppendLine();
            sb.AppendLine("=== 记录里的部位 → 物品 → 算出的分类值 ===");

            var store = new RecordStore();
            foreach (var record in store.Records.Take(3))
            {
                sb.AppendLine($"--- {record.DisplayName} ---");
                foreach (var piece in record.Pieces.OrderBy(p => (int)p.Slot))
                {
                    sb.AppendLine(
                        $"slot {(int)piece.Slot,2} {EquipSlotNames.Of(piece.Slot),-6} : " +
                        $"itemId={piece.ItemId,6} categoryRowId={ItemDb.EquipSlotCategoryId(piece.ItemId),3} " +
                        $"name={ItemDb.Name(piece.ItemId)}");
                }
            }

            var path = Path.Combine(Service.Interface.GetPluginConfigDirectory(), "category-report.txt");
            File.WriteAllText(path, sb.ToString());
            Service.Chat.Print($"[幻化图鉴] 分类报告已写入：{path}");
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex, "写分类报告失败");
        }
    }

    private static void DumpType(StringBuilder sb, Type type)
    {
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            sb.AppendLine($"F {field.Name} : {field.FieldType}");
        }

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            sb.AppendLine($"P {prop.Name} : {prop.PropertyType}");
        }
    }

    private static void DumpMethod(StringBuilder sb, Type type, string name)
    {
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
        {
            if (method.Name != name)
            {
                continue;
            }

            var parameters = string.Join(
                ", ",
                method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
            sb.AppendLine($"{method.ReturnType.Name} {method.Name}({parameters})");
        }
    }
}
