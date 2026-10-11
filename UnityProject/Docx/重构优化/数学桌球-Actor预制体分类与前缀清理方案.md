# 数学桌球 Actor 预制体分类与前缀清理方案

- 日期：2026-10-11
- 任务等级：L3（跨资源目录重排、资源地址与配置同步）
- 状态：用户已确认，资源迁移与加载地址同步已完成；未主动触发编译、测试或游戏验证

## 当前情况与分类依据

`Assets/AssetRaw/Actor/Billiards/` 当前只有 17 个预制体。根据预制体的根对象、挂载组件和配置用途，可以分为球 2 个、道具 9 个、地形机关 5 个、台面 1 个。`BilliardsTable` 挂载 `BoardView`、`SessionRunner` 等组件，归入台面；`BilliardsKeyProp` 挂载 `KeyProp`，归入道具；`JumpPad` 等五个预制体挂载 `TerrainBase`，归入地形机关。其余八个道具挂载 `FlipProp`，具体效果由配置决定，不能仅凭组件名误归为翻转道具。

项目的 YooAsset Actor 收集器根路径为 `Assets/AssetRaw/Actor`，地址规则为 `AddressByFileName`。分类子目录仍位于该根目录内；去掉文件名中的 `Billiards` 会改变运行时 location，因此必须同步配置与制作工具中的字符串地址。

## 拟迁移清单

下表的路径均相对于 `Assets/AssetRaw/Actor/`；所有目标文件名仅删除开头的 `Billiards`，保留其余名称。

| 分类 | 原路径 | 目标路径 |
| --- | --- | --- |
| 球 | `Billiards/BilliardsWhiteBall.prefab` | `Ball/WhiteBall.prefab` |
| 球 | `Billiards/BilliardsBlackBall.prefab` | `Ball/BlackBall.prefab` |
| 道具 | `Billiards/BilliardsFlipProp.prefab` | `Prop/FlipProp.prefab` |
| 道具 | `Billiards/BilliardsReverseProp.prefab` | `Prop/ReverseProp.prefab` |
| 道具 | `Billiards/BilliardsFreezeProp.prefab` | `Prop/FreezeProp.prefab` |
| 道具 | `Billiards/BilliardsRestoreProp.prefab` | `Prop/RestoreProp.prefab` |
| 道具 | `Billiards/BilliardsFastChangeProp.prefab` | `Prop/FastChangeProp.prefab` |
| 道具 | `Billiards/BilliardsRevealProp.prefab` | `Prop/RevealProp.prefab` |
| 道具 | `Billiards/BilliardsPreviewProp.prefab` | `Prop/PreviewProp.prefab` |
| 道具 | `Billiards/BilliardsAddShotProp.prefab` | `Prop/AddShotProp.prefab` |
| 道具 | `Billiards/BilliardsKeyProp.prefab` | `Prop/KeyProp.prefab` |
| 地形机关 | `Billiards/BilliardsJumpPad.prefab` | `Terrain/JumpPad.prefab` |
| 地形机关 | `Billiards/BilliardsTunnel.prefab` | `Terrain/Tunnel.prefab` |
| 地形机关 | `Billiards/BilliardsPit.prefab` | `Terrain/Pit.prefab` |
| 地形机关 | `Billiards/BilliardsReverseBelt.prefab` | `Terrain/ReverseBelt.prefab` |
| 地形机关 | `Billiards/BilliardsTeleporter.prefab` | `Terrain/Teleporter.prefab` |
| 台面 | `Billiards/BilliardsTable.prefab` | `Table/Table.prefab` |

预制体根 GameObject 名称也拟与新文件名一致，去掉 `Billiards` 前缀；子对象名称、组件及序列化字段不作与本次任务无关的修改。

## 必须同步的引用

1. 仓库根目录 `Configs/GameConfig/Datas/` 的 `Board.xlsx`、`Ball.xlsx`、`Prop.xlsx`、`Terrain.xlsx` 中 `prefabLocation` 改为新文件名；使用现有 Luban 导出流程更新 Unity 项目内对应的 `tbboard.bytes`、`tbball.bytes`、`tbprop.bytes`、`tbterrain.bytes`。这些是实际运行时地址，单改预制体会导致加载失败。导出生成物以受影响数据为限，不手改二进制或生成代码。
2. `UnityProject/Tools/ConfigureGameConfig.py` 与 `Campaign.py` 的同名地址同步修改，避免以后重建源表时恢复旧地址。
3. `UnityProject/Tools/TableAuthoring.cs`、`MultiBallAuthoring.cs`、`Board2DChecks.cs` 的预制体路径、名称、创建目录与返回路径同步修改，不执行这些制作或检查工具。
4. 历史实施记录和旧方案保留原叙述，不批量改写；本方案记录新的资源归属。

## 执行边界

- 实施前重新核对 17 个源、目标及其 GUID，并通过当前 Unity CLI/Pipeline 实例发现和状态查询确认目标项目可操作。
- 通过 Unity Pipeline 的资源移动与重命名命令处理 `Assets/` 内预制体和目录；如需调整根 GameObject 名称，使用适合的 Prefab 编辑器 API。保留既有 `.meta` 和 GUID，不用文件系统搬运或文本替换 Unity 序列化资源。
- `BilliardsTable.prefab` 在制定方案时已有未提交修改；迁移时保留这部分内容，不覆盖、重建或回退该文件。尊重编辑器中的未保存状态。
- 17 个资源全部迁出后，才通过 Pipeline 删除空的 `Actor/Billiards` 文件夹；保留 `Actor` 收集器根目录及其配置。
- 没有额外检查授权时，不运行编译、测试、Play Mode、关卡回放或打包。Luban 导出和 Unity 必要导入属于维持本次资源地址一致性的实施步骤，最终如实报告实际完成范围与未验证项。

## 执行记录

2026-10-11，目标实例为 `F:\code\Billiards\UnityProject` 的 Unity 6000.5.10f1 Editor。执行前 `editor_status` 为 `ready`，未编译、未发生 Domain Reload、未进入 Play Mode；打开的 `main` 场景无未保存修改，未打开 Prefab Stage。迁移时没有保存场景或运行制作工具。

1. Unity Pipeline 创建 `Actor/Ball`、`Actor/Prop`、`Actor/Terrain`、`Actor/Table` 四个目录。使用 `batch` 的 `transactional=false` 与 `move_asset` 分组迁移 17 个预制体，同时从文件名去掉 `Billiards` 前缀；每项命令返回成功且 GUID 与迁移前记录一致。Unity 重命名后，预制体根对象名也不再有此前缀。旧 `Actor/Billiards` 目录迁空后由 `delete_asset` 删除。
2. `Configs/GameConfig/Datas/` 内四张源表只调整 `prefabLocation`：Board 1 行、Ball 3 行、Prop 34 行、Terrain 5 行。同步修改 `Tools/ConfigureGameConfig.py`、`Campaign.py` 的作者地址，以及 `TableAuthoring.cs`、`MultiBallAuthoring.cs`、`Board2DChecks.cs` 的目标路径和命名。
3. 在项目 `Temp` 的隔离输出目录运行现有 Luban `cs-bin`/`bin` 导出。与原数据产物比较，仅 `tbboard.bytes`、`tbball.bytes`、`tbprop.bytes`、`tbterrain.bytes` 发生变化；四份数据通过 Pipeline `import_asset` 覆盖导入，命令均返回成功，原 GUID 保持不变。未把隔离生成的 C# 代码导回项目。
4. 开始迁移前 `BilliardsTable.prefab`、`BilliardsWhiteBall.prefab`、`BilliardsBlackBall.prefab` 已有未提交改动；本轮通过 `move_asset` 搬迁，不覆盖或重建这些预制体。

本轮没有主动运行编译、测试、Play Mode、关卡回放、YooAsset 打包或运行时加载检查。导入资源后 Unity 曾自动进入 `compiling`，随后 `editor_status` 恢复为 `ready`、`compiling=false`。这只是编辑器自动刷新状态，不等同于编译结果或游戏验证。
