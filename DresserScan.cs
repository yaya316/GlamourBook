using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using LuminaCabinet = Lumina.Excel.Sheets.Cabinet;

namespace GlamourBook;

/// <summary>
/// 幻化台（Mirage Prism Box）与收藏柜（Armoire）的内容读取。
///
/// 两者数据结构完全不同：
///  - 幻化台：一整块物品 id 数组，读出 800 格即可
///  - 收藏柜：没有「列出全部内容」的接口，只能按 Cabinet 条目编号逐条问「有没有」
///    所以需要先用 Cabinet 表把「物品 id → 收藏柜条目编号」的映射建起来
/// </summary>
public static unsafe class DresserScan
{
    /// <summary>幻化台容量：10 页 × 80 格 = 800。</summary>
    private const int PrismBoxCapacity = 800;

    public static bool TryReadDresser(HashSet<uint> into)
    {
        try
        {
            var manager = MirageManager.Instance();
            if (manager == null)
            {
                return false;
            }

            var ids = manager->PrismBoxItemIds;
            into.Clear();

            for (var i = 0; i < PrismBoxCapacity; i++)
            {
                var id = ids[i];
                if (id != 0)
                {
                    into.Add(id);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex, "读取幻化台失败");
            return false;
        }
    }

    /// <summary>
    /// 扫描收藏柜。返回 false 表示「没读到」，原因可能是收藏柜尚未从服务器加载
    /// （游戏只在旅馆整理收藏柜、或执行某些幻化操作时才会拉取一次）。
    /// </summary>
    public static bool TryReadArmoire(HashSet<uint> into)
    {
        try
        {
            var state = UIState.Instance();
            if (state == null)
            {
                return false;
            }

            // VERIFY-A: 收藏柜在 UIState 里的字段名
            var cabinet = state->Cabinet;
            if (!cabinet.IsCabinetLoaded())
            {
                return false;
            }

            into.Clear();
            var sheet = Service.Data.GetExcelSheet<LuminaCabinet>();

            foreach (var row in sheet)
            {
                // VERIFY-B: Cabinet 表里指向物品的列名
                var itemId = row.Item.RowId;
                if (itemId == 0)
                {
                    continue;
                }

                if (cabinet.IsItemInCabinet(row.RowId))
                {
                    into.Add(itemId);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex, "读取收藏柜失败");
            return false;
        }
    }
}
