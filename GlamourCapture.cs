using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace GlamourBook;

/// <summary>
/// 读取「查看信息」窗口里的幻化。
///
/// 数据来源：AgentInspect。
///  - CurrentEntityId 指向被查看的玩家（不是你的目标，避免记错人）
///  - Items 是 13 个槽位的 ItemData，每条里同时有 ItemId（实际装备）和 GlamourItemId（幻化物品）
///    所以「有幻化取幻化，没幻化取装备」就是你要的这套外观
/// </summary>
public static unsafe class GlamourCapture
{
    private const int SlotCount = 12;

    /// <summary>当前被查看的玩家。窗口没开或对象不在附近时返回 null。</summary>
    public static IPlayerCharacter? CurrentTarget()
    {
        try
        {
            var agent = AgentInspect.Instance();
            if (agent == null)
            {
                return null;
            }

            var entityId = agent->CurrentEntityId;
            if (entityId == 0)
            {
                return null;
            }

            return Service.Objects.SearchById(entityId) as IPlayerCharacter;
        }
        catch
        {
            return null;
        }
    }

    public static bool TryCapture(out GlamourRecord? record, out string error)
    {
        record = null;
        error = string.Empty;

        if (Service.Objects.LocalPlayer == null)
        {
            error = "还没进游戏。";
            return false;
        }

        try
        {
            var agent = AgentInspect.Instance();
            if (agent == null)
            {
                error = "读不到查看信息窗口的数据。";
                return false;
            }

            var player = CurrentTarget();
            if (player == null)
            {
                error = "读不到被查看的玩家，请确认信息窗口已经打开并显示完整。";
                return false;
            }

            var result = new GlamourRecord
            {
                PlayerName = player.Name.ToString(),
                WorldName = player.HomeWorld.Value.Name.ToString(),
                WorldId = player.HomeWorld.RowId,
                CapturedAt = DateTime.Now,
            };

            var items = agent->Items;
            for (var i = 0; i < SlotCount && i < items.Length; i++)
            {
                var data = items[i];
                var finalId = data.GlamourItemId != 0 ? data.GlamourItemId : data.ItemId;
                if (finalId == 0)
                {
                    continue;
                }

                result.Pieces.Add(new GlamourPiece
                {
                    Slot = (EquipSlot)i,
                    ItemId = finalId,
                    BaseItemId = data.ItemId,

                    // 染色：ItemData.Color 是 B/G/R 三个字节，存成 0xRRGGBB 供界面画色块
                    Stain0 = 0,
                    Stain1 = 0,
                    ColorRgb = (uint)((data.Color.R << 16) | (data.Color.G << 8) | data.Color.B),
                });
            }

            if (result.Pieces.Count == 0)
            {
                error = "没有读到任何装备，可能窗口还没加载完。";
                return false;
            }

            record = result;
            return true;
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex, "读取幻化失败");
            error = $"读取异常：{ex.Message}";
            return false;
        }
    }
}
