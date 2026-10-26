using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace GlamourBook;

/// <summary>
/// 调用游戏自带的「试穿」窗口，把整套幻化套到自己身上。
///
/// 走过的弯路：
///  1) 同一帧内连调 11 次 TryOn → 被引擎合并，只剩一件
///  2) 直接写 TryOnItems 数组 → 图标进了列表，但角色身上不生效（缺内部状态字段）
/// 现在：逐帧送一件，交给游戏自己的逻辑处理。
/// </summary>
public static unsafe class TryOnService
{
    private static readonly Queue<GlamourPiece> Pending = new();

    public static void QueueRecord(GlamourRecord record)
    {
        Pending.Clear();
        foreach (var piece in record.Pieces.OrderBy(p => (int)p.Slot))
        {
            Pending.Enqueue(piece);
        }

        if (Pending.Count == 0)
        {
            return;
        }

        Service.Chat.Print($"[幻化图鉴] 正在逐帧送入试穿（{Pending.Count} 件，约 1 秒）…");
    }

    /// <summary>由 Framework.Update 调用：每帧只送一件。</summary>
    public static void Tick()
    {
        if (Pending.Count == 0)
        {
            return;
        }

        var piece = Pending.Dequeue();
        try
        {
            // TryOn(openerAddonId, itemId, stain0Id, stain1Id, glamourItemId, applyCompanyCrest)
            AgentTryon.TryOn(0, piece.ItemId, piece.Stain0, piece.Stain1, 0, false);

            if (Pending.Count == 0)
            {
                var agent = AgentTryon.Instance();
                if (agent != null)
                {
                    // 程序化打开时这个开关默认是关的，模型会显示裸装
                    agent->DisplayGear = true;
                    agent->TryOnItemsChanged = true;
                    agent->ShowAddon();
                }

                Service.Chat.Print("[幻化图鉴] 整套已送入试穿窗口，按 Esc 关闭。");
            }
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex, "试穿单件失败");
            Pending.Clear();
            Service.Chat.PrintError($"[幻化图鉴] 试穿失败：{ex.Message}");
        }
    }
}
