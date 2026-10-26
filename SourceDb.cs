using Lumina.Excel.Sheets;

namespace GlamourBook;

/// <summary>一件物品的来源信息。</summary>
public sealed class SourceInfo
{
    public bool Craftable { get; set; }
    public string? CraftJob { get; set; }

    public bool NpcShop { get; set; }
    public bool SpecialShop { get; set; }
    public bool CabinetStorable { get; set; }

    public uint ItemLevel { get; set; }

    /// <summary>是不是靠推断得出的（界面上会标「疑似」）。</summary>
    public bool Suspect { get; set; }

    public string Label { get; set; } = "未知";
}

/// <summary>
/// 物品来源索引。
///
/// 说明清楚能做什么、不能做什么：
///  - 客户端表能确定：可制作、NPC 商店出售、特殊商店兑换、可入收藏柜
///  - 客户端表里【没有】的：商城（Mog Station / 商城网页）商品清单、副本掉落表
///    这两类分别属于网页数据和服务端数据，只能靠外部数据库补齐
///  - 所以在没有外部数据时，「疑似商城/活动」「疑似副本」只能靠特征推断
/// </summary>
public static class SourceDb
{
    private static Dictionary<uint, SourceInfo>? map;

    public static SourceInfo Get(uint itemId)
    {
        if (map == null)
        {
            map = Build();
        }

        if (map.TryGetValue(itemId, out var info))
        {
            return info;
        }

        return new SourceInfo { Label = "未知" };
    }

    private static Dictionary<uint, SourceInfo> Build()
    {
        var result = new Dictionary<uint, SourceInfo>();

        SourceInfo Ensure(uint itemId)
        {
            if (!result.TryGetValue(itemId, out var info))
            {
                info = new SourceInfo();
                result[itemId] = info;
            }

            return info;
        }

        try
        {
            foreach (var recipe in Service.Data.GetExcelSheet<Recipe>())
            {
                var product = recipe.ItemResult.RowId;
                if (product == 0)
                {
                    continue;
                }

                var info = Ensure(product);
                info.Craftable = true;
                try
                {
                    info.CraftJob = recipe.CraftType.Value.Name.ToString();
                }
                catch
                {
                    // 取不到职业名不影响主判定
                }
            }
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex, "读取配方表失败");
        }

        try
        {
            foreach (var cabinet in Service.Data.GetExcelSheet<Cabinet>())
            {
                var itemId = cabinet.Item.RowId;
                if (itemId != 0)
                {
                    Ensure(itemId).CabinetStorable = true;
                }
            }
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex, "读取收藏柜表失败");
        }

        // 补装等，并生成标签
        var itemSheet = Service.Data.GetExcelSheet<Item>();
        foreach (var (itemId, info) in result)
        {
            try
            {
                var row = itemSheet.GetRowOrDefault(itemId);
                if (row is { } value)
                {
                    info.ItemLevel = value.LevelItem.RowId;
                }
            }
            catch
            {
                // 忽略
            }

            Label(info);
        }

        Service.Log.Information("来源索引构建完成：{Count} 条", result.Count);
        return result;
    }

    private static void Label(SourceInfo info)
    {
        if (info.Craftable)
        {
            info.Label = string.IsNullOrEmpty(info.CraftJob) ? "可制作" : $"可制作·{info.CraftJob}";
            info.Suspect = false;
            return;
        }

        if (info.NpcShop)
        {
            info.Label = "NPC 商店";
            info.Suspect = false;
            return;
        }

        if (info.CabinetStorable)
        {
            // 收藏柜主要收录商城/活动/特典外观，但也会收一些副本与制作外观，
            // 所以只能标「疑似」
            info.Label = "疑似商城/活动";
            info.Suspect = true;
            return;
        }

        if (info.ItemLevel >= 100)
        {
            info.Label = "疑似副本/兑换";
            info.Suspect = true;
            return;
        }

        info.Label = "未知";
        info.Suspect = true;
    }
}
