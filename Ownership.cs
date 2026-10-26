using FFXIVClientStructs.FFXIV.Client.Game;

namespace GlamourBook;

/// <summary>
/// 「我有没有这些外观物品」的判定。
/// 三个来源分开存，因为它们的可读条件不同：
///  - 背包/武具库：随时可读
///  - 幻化台 / 收藏柜：只在对应界面被打开过之后才在客户端内存里有内容（懒加载），所以要缓存
/// </summary>
public sealed class Ownership
{
    public HashSet<uint> Inventory { get; } = new();
    public HashSet<uint> Dresser { get; } = new();
    public HashSet<uint> Armoire { get; } = new();

    public DateTime LastInventoryScan { get; private set; } = DateTime.MinValue;
    public DateTime LastDresserScan { get; private set; } = DateTime.MinValue;
    public DateTime LastArmoireScan { get; private set; } = DateTime.MinValue;

    /// <summary>是否已经扫描过任意一个来源。没有来源时不应该显示「齐全」。</summary>
    public bool HasAnySource => this.Inventory.Count > 0 || this.Dresser.Count > 0 || this.Armoire.Count > 0;

    /// <summary>扫描背包 + 武具库 + 坐骑包等所有常规容器。</summary>
    public unsafe bool ScanInventory()
    {
        try
        {
            var manager = InventoryManager.Instance();
            if (manager == null)
            {
                return false;
            }

            this.Inventory.Clear();
            foreach (var type in Enum.GetValues<InventoryType>())
            {
                var container = manager->GetInventoryContainer(type);
                if (container == null)
                {
                    continue;
                }

                for (var i = 0; i < container->Size; i++)
                {
                    var slot = container->GetInventorySlot(i);
                    if (slot == null || slot->ItemId == 0)
                    {
                        continue;
                    }

                    this.Inventory.Add(slot->ItemId);
                }
            }

            this.LastInventoryScan = DateTime.Now;
            return true;
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex, "扫描背包失败");
            return false;
        }
    }

    public void ScanDresser()
    {
        if (DresserScan.TryReadDresser(this.Dresser))
        {
            this.LastDresserScan = DateTime.Now;
        }
    }

    public void ScanArmoire()
    {
        if (DresserScan.TryReadArmoire(this.Armoire))
        {
            this.LastArmoireScan = DateTime.Now;
        }
    }

    /// <summary>候选物品里任意一件被拥有，就算「具备这个部位的幻化条件」。</summary>
    public bool OwnsAny(IEnumerable<uint> itemIds)
    {
        foreach (var id in itemIds)
        {
            if (this.Inventory.Contains(id) || this.Dresser.Contains(id) || this.Armoire.Contains(id))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>返回命中位置，用于在界面上区分「在背包」「在幻化台」「在收藏柜」。</summary>
    public string? WhereFond(IEnumerable<uint> itemIds)
    {
        foreach (var id in itemIds)
        {
            if (this.Inventory.Contains(id))
            {
                return "背包/武具库";
            }

            if (this.Dresser.Contains(id))
            {
                return "幻化台";
            }

            if (this.Armoire.Contains(id))
            {
                return "收藏柜";
            }
        }

        return null;
    }
}
