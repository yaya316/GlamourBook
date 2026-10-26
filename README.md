# 幻化图鉴 / GlamourBook

Dalamud API 15 插件。记录他人幻化外观（不是实际装备），浏览、对比自己背包/幻化台/收藏柜，标出缺失部位。

## 文件结构

```
GlamourBook/
├── GlamourBook.csproj     项目文件（故意不写 TargetFramework）
├── GlamourBook.json       插件清单（名称必须 = AssemblyName）
├── Plugin.cs              入口 / 命令 / 生命周期
├── Service.cs             静态服务容器
├── Configuration.cs       配置
├── Models.cs              记录 / 部位 / 槽位枚举
├── RecordStore.cs         记录持久化（records.json）
├── ModelIndex.cs          模型 id → 物品 id 反查表
├── GlamourCapture.cs      ★ 读取 Examine 目标的外观（风险点 1）
├── Ownership.cs           背包/武具库扫描 + 拥有判定
├── DresserScan.cs         ★ 幻化台/收藏柜读取（风险点 2，默认编译关闭）
├── ExamineOverlay.cs      「查看信息」窗口下方的记录按钮
└── MainWindow.cs          主窗口
```

## 构建

```powershell
cd GlamourBook
dotnet build -c Debug
```

产物在 `bin/x64/Debug/`。把 `GlamourBook.dll` 和 `GlamourBook.json` 放进
`%APPDATA%\XIVLauncher\pluginConfigs\..\..\devPlugins\GlamourBook\`
（更省事：用 `/xldev` 打开开发面板，把该目录设为 DevPlugins 路径），然后 `/xlplugins` 里重载。

## 设计要点

- **为什么读模型而不是装备表**：角色渲染数据里只有模型 id，但模型已经是幻化生效后的结果，
  而阅读装备表得到的是实际穿着。要「记录幻化」，模型是正确的一层。
- **为什么一件外观可能有多个物品**：同模型物品经常不止一件（同款不同色、不同副本来源）。
  所以每个部位存的是候选列表，界面让你确认；判断「有没有」时只要候选里任一命中即算拥有。
- **收藏柜/幻化台是懒加载的**：游戏只在你打开过对应界面后才把内容填进内存，
  所以必须手动「扫描」后缓存，未扫描过时判定会跳过收藏柜来源。

## 需要你确认的风险点（代码里都标了 VERIFY-x）

| 标记 | 文件 | 内容 |
|---|---|---|
| VERIFY-1 | .csproj | `Dalamud.NET.Sdk/15.0.0` 版本号是否为实际可用版本 |
| VERIFY-B/C | DresserScan.cs | `MirageManager` 的 PrismBox / Armoire 成员名 |
| VERIFY-D | ModelIndex.cs | `Item.ModelMain/ModelSub` 是否仍为 `ushort` |
| VERIFY-E | ModelIndex.cs | `EquipSlotCategory` 字段名 |
| VERIFY-F | GlamourCapture.cs | `AgentInspect.EntityId` |
| VERIFY-G/H | GlamourCapture.cs | `DrawData.Weapon` / `Equipment[].ModelId` |
| VERIFY-I | GlamourCapture.cs | DrawData 槽位下标顺序 |

编译报错优先看这几处。DresserScan 默认不参与编译（`#if GB_DRESSER`），
打开方法见该文件顶部注释。

## 已知缺口

1. 幻化台/收藏柜读取待补（等 struct 字段名确认）
2. 未做图标显示（避免依赖纹理 API，先用文字）
3. 未做多人比对 / 差异视图
4. 记录重复策略：目前同一玩家可存多份（按时间排序），未做自动去重
