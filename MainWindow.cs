using System.Numerics;
using System.Text;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Windowing;

namespace GlamourBook;

public sealed class MainWindow : Window
{
    private static readonly Vector4 Green = new(0.46f, 0.86f, 0.52f, 1f);
    private static readonly Vector4 Orange = new(0.96f, 0.64f, 0.36f, 1f);
    private static readonly Vector4 Blue = new(0.55f, 0.75f, 0.98f, 1f);

    private static readonly ImGuiTableFlags TableFlags =
        ImGuiTableFlags.RowBg
        | ImGuiTableFlags.BordersInnerV
        | ImGuiTableFlags.NoSavedSettings
        | ImGuiTableFlags.SizingStretchProp;

    private readonly RecordStore store;
    private readonly Ownership ownership;
    private readonly Dictionary<uint, ISharedImmediateTexture> iconCache = new();

    private string search = string.Empty;
    private Guid? selectedId;
    private bool didAutoScan;
    private bool onlyMissing;

    public MainWindow(RecordStore store, Ownership ownership)
        : base("幻化图鉴###GlamourBookMain")
    {
        this.store = store;
        this.ownership = ownership;
        this.Size = new Vector2(920f, 660f);
        this.SizeCondition = ImGuiCond.FirstUseEver;
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(700f, 480f),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public override void Draw()
    {
        if (Plugin.Config.RescanOnOpen && !this.didAutoScan)
        {
            this.didAutoScan = true;
            this.ownership.ScanInventory();
        }

        this.DrawHeader();
        ImGui.Spacing();
        this.DrawList();
        ImGui.Spacing();
        this.DrawDetail();
    }

    private void DrawHeader()
    {
        ImGui.TextUnformatted("幻化图鉴");
        ImGui.SameLine();
        ImGui.TextDisabled("记录他人的幻化外观，而不是他穿了什么");

        ImGui.Spacing();

        if (ImGui.Button("重扫背包"))
        {
            var ok = this.ownership.ScanInventory();
            Service.Chat.Print(ok
                ? $"[幻化图鉴] 背包/武具库已刷新：{this.ownership.Inventory.Count} 件。"
                : "[幻化图鉴] 背包刷新失败，见日志。");
        }

        ImGui.SameLine();
        if (ImGui.Button("扫描幻化台"))
        {
            this.ownership.ScanDresser();
            Service.Chat.Print(this.ownership.LastDresserScan == DateTime.MinValue
                ? "[幻化图鉴] 幻化台读不到。先在游戏里打开一次幻化台再点这里。"
                : $"[幻化图鉴] 幻化台已缓存：{this.ownership.Dresser.Count} 件。");
        }

        ImGui.SameLine();
        if (ImGui.Button("扫描收藏柜"))
        {
            this.ownership.ScanArmoire();
            Service.Chat.Print(this.ownership.LastArmoireScan == DateTime.MinValue
                ? "[幻化图鉴] 收藏柜还没加载。去旅馆找管家整理一次收藏柜，再回来点这里。"
                : $"[幻化图鉴] 收藏柜已缓存：{this.ownership.Armoire.Count} 件。");
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(230f);
        ImGui.InputTextWithHint("##gbsearch", "搜索玩家 / 世界 / 备注", ref this.search, 128);

        ImGui.Spacing();

        var hasSource = this.ownership.HasAnySource;
        ImGui.TextColored(hasSource ? Green : Orange, "●");
        ImGui.SameLine();
        ImGui.TextDisabled($"背包 {this.ownership.Inventory.Count} 件");
        ImGui.SameLine();
        ImGui.TextColored(this.ownership.Dresser.Count > 0 ? Green : Orange, "●");
        ImGui.SameLine();
        ImGui.TextDisabled($"幻化台 {this.ownership.Dresser.Count} 件");
        ImGui.SameLine();
        ImGui.TextColored(this.ownership.Armoire.Count > 0 ? Green : Orange, "●");
        ImGui.SameLine();
        ImGui.TextDisabled($"收藏柜 {this.ownership.Armoire.Count} 件");

        if (!hasSource)
        {
            ImGui.TextColored(Orange, "还没扫描任何来源，下面的「是否拥有」无法判断。");
        }
    }

    private void DrawList()
    {
        var results = this.store.Search(this.search)
            .OrderByDescending(r => r.CapturedAt)
            .ToList();

        ImGui.TextDisabled($"共 {results.Count} 条记录");

        if (results.Count == 0)
        {
            ImGui.Spacing();
            ImGui.TextDisabled(this.store.Records.Count == 0
                ? "还没有记录。右键玩家 → 查看信息，点窗口下方的「记录 … 的幻化」。"
                : "没有匹配的记录。");
            return;
        }

        if (ImGui.BeginTable("##recordlist", 3, TableFlags))
        {
            ImGui.TableSetupColumn("玩家", ImGuiTableColumnFlags.WidthStretch, 0.6f);
            ImGui.TableSetupColumn("记录时间", ImGuiTableColumnFlags.WidthFixed, 130f);
            ImGui.TableSetupColumn("状态", ImGuiTableColumnFlags.WidthFixed, 90f);
            ImGui.TableHeadersRow();

            foreach (var record in results)
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                var selected = this.selectedId == record.Id;
                if (ImGui.Selectable($"{record.DisplayName}##{record.Id}", selected, ImGuiSelectableFlags.SpanAllColumns))
                {
                    this.selectedId = record.Id;
                }

                ImGui.TableSetColumnIndex(1);
                ImGui.TextDisabled(record.CapturedAt.ToString("MM-dd HH:mm"));

                ImGui.TableSetColumnIndex(2);
                this.DrawStatusSummary(record);
            }

            ImGui.EndTable();
        }
    }

    private void DrawStatusSummary(GlamourRecord record)
    {
        if (!this.ownership.HasAnySource)
        {
            ImGui.TextDisabled("未扫描");
            return;
        }

        var missing = this.CountMissing(record);
        if (missing == 0)
        {
            ImGui.TextColored(Green, "齐全");
        }
        else
        {
            ImGui.TextColored(Orange, $"缺 {missing}");
        }
    }

    private void DrawDetail()
    {
        var record = this.selectedId is { } id
            ? this.store.Records.FirstOrDefault(r => r.Id == id)
            : null;

        if (record == null)
        {
            ImGui.TextDisabled("选中上面一条记录查看详情。");
            return;
        }

        ImGui.TextUnformatted(record.DisplayName);
        ImGui.SameLine();
        ImGui.TextDisabled(record.CapturedAt.ToString("yyyy-MM-dd HH:mm:ss"));

        var missing = this.CountMissing(record);
        var total = record.Pieces.Count;
        if (this.ownership.HasAnySource)
        {
            ImGui.TextColored(Green, $"已有 {total - missing} / {total}");
            ImGui.SameLine();
            if (missing > 0)
            {
                ImGui.TextColored(Orange, $"缺 {missing} 个部位");
            }
            else
            {
                ImGui.TextColored(Green, "可以整套幻化");
            }
        }
        else
        {
            ImGui.TextColored(Orange, "未扫描来源，无法判断拥有情况。");
        }

        ImGui.Spacing();

        ImGui.Checkbox("只看缺件", ref this.onlyMissing);

        ImGui.SameLine();
        if (ImGui.Button("复制缺失清单"))
        {
            this.CopyMissing(record);
        }

        ImGui.SameLine();
        if (ImGui.Button("试穿整套"))
        {
            TryOnService.QueueRecord(record);
        }

        ImGui.SameLine();
        if (ImGui.Button("删除这条记录"))
        {
            this.store.Remove(record.Id);
            this.selectedId = null;
            return;
        }

        ImGui.Spacing();

        if (ImGui.BeginTable("##pieces", 5, TableFlags))
        {
            ImGui.TableSetupColumn("部位", ImGuiTableColumnFlags.WidthFixed, 70f);
            ImGui.TableSetupColumn("物品", ImGuiTableColumnFlags.WidthStretch, 0.9f);
            ImGui.TableSetupColumn("染色", ImGuiTableColumnFlags.WidthFixed, 170f);
            ImGui.TableSetupColumn("来源", ImGuiTableColumnFlags.WidthFixed, 130f);
            ImGui.TableSetupColumn("状态", ImGuiTableColumnFlags.WidthFixed, 90f);
            ImGui.TableHeadersRow();

            foreach (var piece in record.Pieces.OrderBy(p => (int)p.Slot))
            {
                if (this.onlyMissing
                    && this.ownership.HasAnySource
                    && this.ownership.OwnsAny(new[] { piece.ItemId }))
                {
                    continue;
                }

                this.DrawPieceRow(piece);
            }

            ImGui.EndTable();
        }
    }

    private void DrawPieceRow(GlamourPiece piece)
    {
        ImGui.TableNextRow(ImGuiTableRowFlags.None, 48f);

        ImGui.TableSetColumnIndex(0);
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(EquipSlotNames.Of(piece.Slot));

        ImGui.TableSetColumnIndex(1);
        this.DrawIcon(piece.ItemId, 40f);
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(ItemDb.Name(piece.ItemId));
        if (piece.IsGlamoured)
        {
            ImGui.SameLine();
            ImGui.TextColored(Blue, "幻化");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip($"实际装备：{ItemDb.Name(piece.BaseItemId)}");
            }
        }

        ImGui.TableSetColumnIndex(2);
        if (piece.ColorRgb != 0)
        {
            var r = ((piece.ColorRgb >> 16) & 0xFF) / 255f;
            var g = ((piece.ColorRgb >> 8) & 0xFF) / 255f;
            var b = (piece.ColorRgb & 0xFF) / 255f;

            ImGui.ColorButton(
                $"##dye{piece.Slot}",
                new Vector4(r, g, b, 1f),
                ImGuiColorEditFlags.NoTooltip,
                new Vector2(18f, 18f));

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip($"#{piece.ColorRgb:X6}");
            }

            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(ItemDb.DyeName(piece.ColorRgb));
        }
        else
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextDisabled("未染色");
        }

        ImGui.TableSetColumnIndex(3);
        {
            var source = SourceDb.Get(piece.ItemId);
            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(source.Suspect ? Orange : Blue, source.Label);
        }

        ImGui.TableSetColumnIndex(4);
        var where = this.ownership.WhereFond(new[] { piece.ItemId });
        if (!this.ownership.HasAnySource)
        {
            ImGui.TextDisabled("—");
        }
        else if (where != null)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(Green, "✓ 已有");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip($"位于：{where}");
            }
        }
        else
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(Orange, "✗ 缺少");
        }
    }

    private void DrawIcon(uint itemId, float size)
    {
        var iconId = ItemDb.IconId(itemId);
        if (iconId == 0)
        {
            ImGui.Dummy(new Vector2(size, size));
            ImGui.SameLine();
            return;
        }

        if (!this.iconCache.TryGetValue(iconId, out var texture) || texture is null)
        {
            texture = Service.Texture.GetFromGameIcon(new GameIconLookup(iconId));
            this.iconCache[iconId] = texture;
        }

        var wrap = texture.GetWrapOrDefault();
        if (wrap is null)
        {
            ImGui.Dummy(new Vector2(size, size));
            ImGui.SameLine();
            return;
        }

        ImGui.Image(wrap.Handle, new Vector2(size, size));
        ImGui.SameLine();
    }

    private void CopyMissing(GlamourRecord record)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"【幻化图鉴】{record.DisplayName}  {record.CapturedAt:yyyy-MM-dd HH:mm}");

        var count = 0;
        foreach (var piece in record.Pieces.OrderBy(p => (int)p.Slot))
        {
            if (this.ownership.HasAnySource && this.ownership.OwnsAny(new[] { piece.ItemId }))
            {
                continue;
            }

            var dye = piece.ColorRgb != 0 ? $"（{ItemDb.DyeName(piece.ColorRgb)}）" : string.Empty;
            builder.AppendLine($"{EquipSlotNames.Of(piece.Slot)}：{ItemDb.Name(piece.ItemId)}{dye}");
            count++;
        }

        if (count == 0)
        {
            Service.Chat.Print("[幻化图鉴] 这套没有缺件。");
            return;
        }

        ImGui.SetClipboardText(builder.ToString());
        Service.Chat.Print($"[幻化图鉴] {count} 件缺失已复制到剪贴板。");
    }

    private int CountMissing(GlamourRecord record)
    {
        if (!this.ownership.HasAnySource)
        {
            return 0;
        }

        var missing = 0;
        foreach (var piece in record.Pieces)
        {
            if (!this.ownership.OwnsAny(new[] { piece.ItemId }))
            {
                missing++;
            }
        }

        return missing;
    }
}
