namespace GlamourBook;

/// <summary>12 个可幻化部位，顺序与游戏装备槽位一致（0=主手 … 11=戒指2）。</summary>
public enum EquipSlot
{
    MainHand = 0,
    OffHand = 1,
    Head = 2,
    Body = 3,
    Hands = 4,
    Legs = 5,
    Feet = 6,
    Ears = 7,
    Neck = 8,
    Wrists = 9,
    Ring1 = 10,
    Ring2 = 11,
}

public static class EquipSlotNames
{
    public static string Of(EquipSlot slot) => slot switch
    {
        EquipSlot.MainHand => "主手",
        EquipSlot.OffHand => "副手",
        EquipSlot.Head => "头部",
        EquipSlot.Body => "身体",
        EquipSlot.Hands => "手部",
        EquipSlot.Legs => "腿部",
        EquipSlot.Feet => "脚部",
        EquipSlot.Ears => "耳饰",
        EquipSlot.Neck => "项链",
        EquipSlot.Wrists => "手镯",
        EquipSlot.Ring1 => "戒指 1",
        EquipSlot.Ring2 => "戒指 2",
        _ => slot.ToString(),
    };
}

/// <summary>单个部位的外观。</summary>
public sealed class GlamourPiece
{
    public EquipSlot Slot { get; set; }

    /// <summary>实际看到的物品 id（有幻化时是幻化物品，没有时就是装备本身）。</summary>
    public uint ItemId { get; set; }

    /// <summary>他身上真正穿的装备 id，仅作参考。</summary>
    public uint BaseItemId { get; set; }

    /// <summary>染色（染剂编号，0 = 未染色）。</summary>
    public byte Stain0 { get; set; }
    public byte Stain1 { get; set; }

    /// <summary>染色颜色（0xRRGGBB，来自 ItemData.Color）。0 表示无染色。</summary>
    public uint ColorRgb { get; set; }

    public bool IsGlamoured => this.ItemId != 0 && this.ItemId != this.BaseItemId;
}

/// <summary>一次「记录」= 一个玩家的一份幻化快照。</summary>
public sealed class GlamourRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PlayerName { get; set; } = string.Empty;
    public uint WorldId { get; set; }
    public string WorldName { get; set; } = string.Empty;
    public DateTime CapturedAt { get; set; } = DateTime.Now;
    public string Note { get; set; } = string.Empty;
    public List<GlamourPiece> Pieces { get; set; } = new();

    public string DisplayName => string.IsNullOrEmpty(this.WorldName)
        ? this.PlayerName
        : $"{this.PlayerName} @ {this.WorldName}";
}
