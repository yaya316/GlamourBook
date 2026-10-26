# 幻化图鉴 / GlamourBook

一个 Dalamud（最终幻想14 插件框架）插件，用于记录、浏览他人幻化，并对照自己的收藏情况。

> **状态：已归档，未完成。**
> 核心功能可用，试穿与来源细分未完成（详见下方「已知限制」）。
> 保留此仓库主要作为留档与技术参考——里面关于游戏内部结构的结论都是实机验证过的。

---

## 安装

在游戏内操作（需要 **Dalamud API 15**）：

1. `/xlplugins` → **设置**
2. 找到 **实验性 / Experimental** → **第三方插件仓库**
3. 添加下面的地址并保存：

```
https://raw.githubusercontent.com/yaya316/GlamourBook/main/repo.json
```

国内网络访问 `raw.githubusercontent.com` 经常不通，可改用镜像地址：

```
https://cdn.jsdelivr.net/gh/yaya316/GlamourBook@main/repo.json
```

4. 回到插件列表，搜索「幻化图鉴」即可安装

---

## 已完成

| 功能 | 说明 |
|---|---|
| 记录他人幻化 | 在「查看信息」窗口标题栏右侧的按钮，一键记录对方每个部位的外观物品（优先幻化物品，而非实际装备） |
| 记录身份准确 | 使用 `AgentInspect.CurrentEntityId` 定位被查看者，不依赖玩家当前目标 |
| 浏览与搜索 | 按玩家名 / 世界 / 备注搜索，表格展示，物品图标 |
| 缺件比对 | 对照背包、武具库、幻化台（`MirageManager`）、收藏柜（`Cabinet`），逐部位标出「已有 / 缺少」 |
| 只看缺件 / 复制缺失清单 | 勾选过滤；一键把缺失物品与染色复制到剪贴板 |
| 染色显示 | 色块 + 染剂名（由 RGB 反查染剂表） |
| 来源标注 | 可制作 / 疑似商城·活动 / 疑似副本·兑换 / 未知 |
| 原生风格按钮 | 自绘 ImGui 按钮（渐变 + 金边 + 高光 + 文字投影），贴在游戏窗口上 |

## 已知限制

1. **试穿不生效。** `AgentTryon.TryOn` 能让游戏接受每一件（聊天栏会逐条提示），试穿窗口也会弹出、图标也在，但模型上不显示装备。
   已尝试：同帧批量调用、逐帧调用、直接写 `TryOnItems` 数组（14 槽）、设置 `DisplayGear`。均未解决。
   **推测**：程序化调用缺少某个内部状态（`OpenerAddonId` 或视图初始化），需要更深的逆向。
2. **商城名 / 副本名缺失。** 这两类数据不在客户端表里：
   - 商城是网页数据（`qu.sdo.com`），需要抓接口 JSON
   - 副本掉落是服务端数据，客户端无掉落表
   目前的「疑似商城/活动」「疑似副本/兑换」是启发式推断（可入收藏柜 / 装等 ≥ 100），显示为橙色，准确度有限。
3. **染色是颜色匹配，不是染剂 ID。** `ItemData.Color` 给的是 RGB（`B/G/R` 三个字节），染剂名是按颜色反查的（完全相同 → 最近色，容差 ≤ 24）。同色异名时会取到其中一个。
4. **收藏柜需要先加载。** 游戏只在打开过收藏柜界面后才拉取数据，插件只能缓存扫描结果。

## 技术备忘（实机验证过的结构）

这些是从运行中的游戏里用反射导出来的，不是猜的，对写别的插件也有用：

```csharp
// 被查看的玩家
AgentInspect.Instance()->CurrentEntityId

// 每个槽位的物品（13 个）
AgentInspect.Instance()->Items           // Span<ItemData>
AgentInspect.ItemData:
    uint ItemId          // 实际装备
    uint GlamourItemId   // 幻化物品（无幻化时为 0）
    uint IconId
    ColorRgb Color       // 字段为 B/G/R 三个 byte
    bool CanCompare, CanTryOn

// 幻化台（800 格）
MirageManager.Instance()->PrismBoxItemIds     // Client.Game 命名空间，不是 UI

// 收藏柜
UIState.Instance()->Cabinet
    .IsCabinetLoaded()
    .IsItemInCabinet(uint cabinetRowId)
// 物品 → 收藏柜行号：Cabinet 表的 Item 列

// 试穿
AgentTryon.TryOn(uint openerAddonId, uint itemId, byte stain0Id, byte stain1Id, uint glamourItemId, bool applyCompanyCrest)  // 静态方法
AgentTryon.Instance()->TryOnItems            // Span<TryOnItem>，14 槽，与部位一一对应
AgentTryon.Instance()->DisplayGear           // 布尔开关

// 物品表
Item.Icon        // ushort，就是图标 id（不是 RowRef）
Item.ModelMain   // ulong
Item.EquipSlotCategory   // 链接，取 .RowId

// 槽分类行号（实测）
// 1=主手 2=副手 3=头 4=身 5=手 7=腿 8=脚 9=耳 10=颈 11=腕 12=戒指 13=双手武器

// Dalamud 侧
AtkUnitBasePtr.Width / .Height   // 返回未初始化的垃圾值（≈2.9e-39）
AtkUnitBasePtr.ScaledWidth / .ScaledHeight   // 真实尺寸，用这两个
AtkUnitBasePtr.Position / .X / .Y            // 屏幕像素，可信
```

## 构建

需要 .NET SDK 与已安装的 Dalamud（API 15）。

```powershell
cd GlamourBook
dotnet build -c Debug
```

产物在 `bin/Debug/`，把 `GlamourBook.dll` 和 `GlamourBook.json` 放进一个目录，
在游戏里 `/xldev` 把该目录加为开发插件路径，然后 `/xlplugins` 启用。

调试命令：

- `/gb` — 打开主窗口
- `/gbdump` — 导出关键结构字段到插件配置目录
- `/gbpos` — 导出「查看信息」窗口的几何数据（校准按钮位置）
- `/gbcat` — 导出槽位分类对照表

## 许可

MIT
