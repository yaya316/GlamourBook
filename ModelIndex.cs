using Lumina.Excel.Sheets;

namespace GlamourBook;

/// <summary>
/// 物品表 / 染剂表查询。文件名是历史遗留（早期版本这里做的是「模型→物品」反查）。
///
/// 图标 id 目前用反射取（Item 表里列名在不同 Lumina 版本间变过），
/// 拿到确切字段名后会改回直接访问。
/// </summary>
public static class ItemDb
{
    private static readonly Dictionary<uint, uint> IconCache = new();

    public static string Name(uint itemId)
    {
        if (itemId == 0)
        {
            return "（空）";
        }

        var item = Row(itemId);
        return item is null ? $"未知物品 #{itemId}" : item.Value.Name.ToString();
    }

    public static uint IconId(uint itemId)
    {
        if (itemId == 0)
        {
            return 0;
        }

        if (IconCache.TryGetValue(itemId, out var cached))
        {
            return cached;
        }

        var result = 0u;
        try
        {
            var item = Row(itemId);
            if (item is { } value)
            {
                // Item.Icon 是 ushort 类型的图标 id（已用 struct-dump 确认）
                result = value.Icon;
            }
        }
        catch
        {
            result = 0;
        }

        IconCache[itemId] = result;
        return result;
    }

    /// <summary>物品的装备槽分类编号（试穿窗口用它决定往哪个槽位套）。</summary>
    public static byte EquipSlotCategoryId(uint itemId)
    {
        try
        {
            var item = Row(itemId);
            if (item is { } value)
            {
                return (byte)value.EquipSlotCategory.RowId;
            }
        }
        catch
        {
            // 回退到 0，试穿窗口会按物品 id 自行推断
        }

        return 0;
    }

    private static List<(uint Rgb, string Name)>? dyeTable;

    /// <summary>用 RGB 反查染剂名字：先找完全一致的，找不到就取最接近的颜色。</summary>
    public static string DyeName(uint rgb)
    {
        if (rgb == 0)
        {
            return "未染色";
        }

        dyeTable ??= BuildDyeTable();
        if (dyeTable.Count == 0)
        {
            return "—";
        }

        foreach (var entry in dyeTable)
        {
            if (entry.Rgb == rgb)
            {
                return entry.Name;
            }
        }

        var targetR = (int)((rgb >> 16) & 0xFF);
        var targetG = (int)((rgb >> 8) & 0xFF);
        var targetB = (int)(rgb & 0xFF);

        var bestName = "—";
        var bestDistance = int.MaxValue;

        foreach (var entry in dyeTable)
        {
            var r = (int)((entry.Rgb >> 16) & 0xFF);
            var g = (int)((entry.Rgb >> 8) & 0xFF);
            var b = (int)(entry.Rgb & 0xFF);

            var distance = Math.Abs(r - targetR) + Math.Abs(g - targetG) + Math.Abs(b - targetB);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestName = entry.Name;
            }
        }

        // 差得太远就说明不是染剂色，别硬凑
        return bestDistance <= 24 ? bestName : "—";
    }

    private static List<(uint Rgb, string Name)> BuildDyeTable()
    {
        var table = new List<(uint, string)>();

        try
        {
            foreach (var stain in Service.Data.GetExcelSheet<Stain>())
            {
                var name = stain.Name.ToString();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var rgb = (uint)stain.Color;
                if (rgb == 0)
                {
                    continue;
                }

                table.Add((rgb, name));
            }
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex, "构建染剂颜色表失败");
        }

        return table;
    }

    public static string StainName(byte stainId)
    {
        if (stainId == 0)
        {
            return "未染色";
        }

        try
        {
            var stain = Service.Data.GetExcelSheet<Stain>().GetRowOrDefault(stainId);
            return stain is null ? $"染剂 #{stainId}" : stain.Value.Name.ToString();
        }
        catch
        {
            return $"染剂 #{stainId}";
        }
    }

    private static Item? Row(uint itemId)
    {
        try
        {
            return Service.Data.GetExcelSheet<Item>().GetRowOrDefault(itemId);
        }
        catch
        {
            return null;
        }
    }
}
