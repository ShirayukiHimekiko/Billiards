# 数学桌球 YooAsset 资源目录重排方案

- 日期：2026-10-07
- 任务等级：L3（跨目录资源迁移与制作工具路径调整）
- 状态：用户已确认，资源迁移和制作工具路径调整已完成；未运行编译、测试或游戏验证

## 目标与分类边界

按用户明确规则执行：需要作为独立入口通过 YooAsset 或 GameModule.Resource 加载的资源放在 Assets/AssetRaw；不需要独立加载的资源放在 Assets/AssetArt。

Prefab、材质等通过 Unity 对象引用使用的依赖，也可以放在 AssetArt。AssetArt 不表示禁止打包；依赖仍由 YooAsset 的主资源依赖分析纳入构建。不能用“没有序列化 GUID 引用”推断资源未使用，动态加载入口必须同时追踪代码、Window 属性和配置字段。

## 已完成的只读分析

1. AssetRaw 有 77 个非 .meta 资源文件；AssetArt 当前无资源文件。
2. 拟保留 17 个独立加载入口：4 个业务实体 Prefab、4 个 UI 窗口 Prefab、9 个 Luban .bytes。
3. 拟迁移 AssetRaw 中 60 个文件：33 张 PNG、21 个材质、2 个网格、1 个字体、1 个授权文本、2 个非独立加载 UI Prefab。
4. 另将业务代码目录内的 2 个 Shader 移到 AssetArt，合计迁移 62 个资源文件；文件夹和 .meta 不计入该数量。
5. MainUI 类的 Window 地址为 GameMainUI；GameMainUI.prefab 必须保留在 AssetRaw。MainUI.prefab 未发现当前业务加载入口，移到 AssetArt 保留。
6. LevelSelectUI 通过 m_item_LevelItemWidget 模板和 AdjustIconNum 创建卡片，LevelItemWidget.prefab 不需要独立加载；其 GUID 被 LevelSelectUI 引用。
7. NotoSans 字体被 5 个 UI Prefab 引用；网格被桌面和球 Prefab 引用。当前业务未发现独立加载贴图、材质、字体或网格的调用。
8. 原有 Launcher、TEngine 内置资源、启动场景、ProjectSettings 等拥有框架和 Unity 固定用途，不作为这次业务资源整理的迁移目标。

## 成熟方案评估与依据

已查询 YooAsset 官方 GitHub，复用现有主资源/依赖资源模型，不新增资源管理框架：

- 官方资源信息同时区分可寻址 Address 和依赖 DependAssets：https://github.com/tuyoogame/YooAsset/blob/yoo3/Assets/YooAsset/Editor/BundleCollector/CollectAssetInfo.cs 。该网页为上游 yoo3 分支，仅用于模型参考，具体行为以本项目安装源码为准。
- 本地 Packages/com.tuyoogame.yooasset/Editor/AssetBundleCollector/ECollectorType.cs 区分 MainAssetCollector、StaticAssetCollector 和 DependAssetCollector。
- 本地 AssetBundleCollector.cs 的 CreateCollectAssetInfo/GetAllDependencies 收集主资源依赖。
- 本地 AssetBundleBuilder/BuildPipeline/BaseTasks/TaskGetBuildMap.cs 为未显式收集的依赖创建 BuildAssetInfo，并处理共享资源和依赖 Shader。无需把 AssetArt 全量加入 MainAssetCollector。

## 迁移操作清单

通过 Unity Pipeline 专用资源移动命令执行，必要时按已发现的命令能力使用最小 AssetDatabase 操作。保留资源和文件夹 GUID、导入设置与序列化引用，不在文件系统中手动移动 .meta。

| 原路径 | 目标路径 | 理由 |
| --- | --- | --- |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards | Assets/AssetArt/UIRaw/Raw/MathBilliards | 当前业务没有独立 Sprite 加载调用；保留 Prefab 引用及未使用源图 |
| Assets/AssetRaw/Materials/Billiards | Assets/AssetArt/Materials/Billiards | 由 Renderer 等组件引用；未发现业务独立加载材质 |
| Assets/AssetRaw/Actor/Billiards/Meshes | Assets/AssetArt/Actor/Billiards/Meshes | 由桌面及球 Prefab 引用 |
| Assets/AssetRaw/Fonts/NotoSansCJKsc-Regular.otf | Assets/AssetArt/Fonts/NotoSansCJKsc-Regular.otf | 由 UI Text 组件直接引用 |
| Assets/AssetRaw/Fonts/NotoSansCJK-LICENSE.txt | Assets/AssetArt/Fonts/NotoSansCJK-LICENSE.txt | 字体授权文件，不作为运行时加载入口 |
| Assets/AssetRaw/UI/LevelItemWidget.prefab | Assets/AssetArt/UI/LevelItemWidget.prefab | LevelSelectUI 的嵌套模板，通过已有对象复制生成卡片 |
| Assets/AssetRaw/UI/MainUI.prefab | Assets/AssetArt/UI/MainUI.prefab | 未发现当前业务加载入口；MainUI 类的 Window 地址实际为 GameMainUI，保留此旧资源而不删除 |
| Assets/GameScripts/HotFix/GameLogic/Core/Level/BilliardsWorld.shader | Assets/AssetArt/Shaders/Billiards/BilliardsWorld.shader | 材质依赖；保持 Shader 名称 Billiards/WorldTint |
| Assets/GameScripts/HotFix/GameLogic/Core/Ball/BilliardsBall.shader | Assets/AssetArt/Shaders/Billiards/BilliardsBall.shader | 材质依赖；保持既有 Shader 名称 |

## 收集器处理

保留 Assets/AssetRaw/Actor、UI、Fonts、Materials、UIRaw/Raw 等既有收集器根目录及其 .meta，仅移动上述具体子目录或文件。Fonts 中两个文件分别移动，不整体移动 Fonts 目录；Materials 仅移动 Billiards 子目录；UIRaw/Raw 仅移动 MathBilliards 子目录。

原因：收集器保存 CollectPath 和 CollectorGUID。移动收集器根目录后，工具可能通过目录 GUID 跟随新位置，继续将 AssetArt 下的内容作为 MainAssetCollector，违背此次规则。

AssetRaw 中空的收集器目录可以保留，目录本身不是待加载资源。当前不用新增 AssetArt 主资源收集器，不调整打包规则或共享包策略。独立加载入口的文件名和 location 保持不变。

## 制作工具配套调整

移动成功后，在同一任务范围内修正以下制作工具的源图、字体、材质、网格或嵌套卡片路径；不改变业务逻辑，也不运行这些制作脚本：

- Tools/InitialDemoAuthoring.cs：Art 源图路径、NotoSans 字体路径改为 AssetArt。
- Tools/TableAuthoring.cs（执行时已更名）：源图、网格创建目录、材质创建目录改为 AssetArt；实体 Prefab 与 GameMainUI 仍输出到 AssetRaw。相应文件夹创建逻辑同步调整。
- Tools/MultiBallAuthoring.cs（执行时已更名）：读取 WorldTint、TableDisk、TableQuad 及创建 MultiBall 材质的目录改为 AssetArt；独立实体 Prefab 仍保留在 AssetRaw。
- Tools/UI/BuildLevelSelectUI.eval.cs：LevelItemWidget 输出到 AssetArt/UI；先通过 AssetDatabase 创建该目录。LevelSelectUI、GameMainUI 继续使用 AssetRaw/UI。

现有验收脚本引用的桌面 Prefab 和 GameMainUI 地址未改变，不需要修改。历史设计交付说明只作为历史记录，不批量改写；不运行美术生成脚本、Luban、Prefab 制作或验收工具。

## 执行顺序与失败处理

1. 用户确认本方案。
2. 发现目标项目的 Pipeline 实例，读取实际资源命令及 schema，确认编辑器可操作；不得保存或丢弃用户的未保存内容。
3. 通过 Pipeline 创建缺失目标目录，确认目标无同名资源，再按清单移动。已有源目录的移动保留其 .meta；新增目录由 Unity 创建。
4. 每项记录真实命令结果。失败或超时先查源/目标当前状态，不盲目重试，不覆盖同名资源，不删除并重建资源。
5. 成功移动后更新对应工具路径，记录完成与未完成项目。
6. 无额外验证授权时，不主动编译、运行测试、进入 Play Mode、构建 AssetBundle 或 Player。移动命令导致的必要 AssetDatabase 导入属于资源操作本身，不宣称游戏验证通过。
7. 结束后如实报告执行范围、未验证项，并询问是否需要检查。

## 执行结果

2026-10-07 连接已恢复，目标为 F:\code\Billiards\UnityProject 的 Unity 6000.5.10f1 Editor。通过实时命令 schema 确认 create_folder、move_asset 和 batch 能力后执行迁移。

- Pipeline batch 返回 15 项操作全部成功：6 项目标目录创建、9 项文件或目录移动，覆盖清单中的 62 个资源文件。
- 使用 move_asset/AssetDatabase 维护 GUID、.meta、导入设置及既有引用；保留所有收集器根目录，没有修改收集器配置。
- 保留 AssetRaw 中原有 17 个独立加载入口；既有文件名和运行时 location 不变。
- 通过普通文件编辑调整 InitialDemoAuthoring.cs、TableAuthoring.cs、MultiBallAuthoring.cs、Tools/UI/BuildLevelSelectUI.eval.cs，共 4 个制作脚本。未执行这些制作脚本。
- 操作前后 main 场景均为已保存状态，编辑器处于编辑模式；未保存或修改场景，未进入 Play Mode。
- 收尾 editor_status 返回 ready，未处于编译或 Domain Reload。
- 未主动运行编译、测试、构建、截图、运行时资源加载或引用完整性验证；不宣称游戏验证通过。

执行前逐文件路径和 GUID 清单：`数学桌球-资源迁移执行清单.json`。原始 Pipeline 操作结果：`数学桌球-资源迁移命令结果.json`。这两份记录只用于追踪本次迁移，不是测试结果。

## 历史连接阻碍（已恢复）

用户已确认本方案并要求重新连接 CLI。本次重连通过 CLI 和只读进程、日志查询确认：目标项目 Unity Editor 正在运行（PID 16004），Pipeline 包为 0.7.0-exp.1；日志记录 Pipeline Server 已启动，目标进程监听 7800 端口，但 Library/Pipeline/.unity-pipeline-port 实例描述文件缺失。`unity pipeline list` 返回服务器不可达，`unity command --project-path` 仍返回未找到实例。在沙箱外重试得到相同结果，不能归因于沙箱限制。

上述排查阶段尚未移动资源，也未修改制作脚本。未重启或关闭用户编辑器，未重装或升级 Pipeline，未运行编译、测试或 Play Mode。用户继续任务后连接已恢复，并完成上文所列迁移。

2026-10-07 执行 `unity command --project-path F:\code\Billiards\UnityProject`，CLI 返回：

> No Pipeline instance found for project: F:\code\Billiards\UnityProject. Make sure Unity Editor is running with the Pipeline package installed.

项目 manifest 已声明 com.unity.pipeline 0.7.0-exp.1，但当前无法连接到该项目实例。需要使项目 Unity Editor 的 Pipeline 可连接后，才能执行既有资源移动。此结果不证明 Unity Editor 一定关闭，也不证明包未安装。

依据 .agents/skills/tengine-dev/references/unity-operations.md：Pipeline 不可达时禁止移动既有 Assets 资源；不得改用 PowerShell 文件搬运或直接改写 Unity 序列化文件。

## 完整资源文件明细

下面按当前磁盘文件列出迁移与保留目标，仅为执行前分析清单，不是运行或构建验证结果。

### 拟迁移的 62 个文件

| 原路径 | 新路径 |
| --- | --- |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/aim_arrow.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/aim_arrow.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/ball_inverse.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/ball_inverse.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/ball_normal.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/ball_normal.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/board_background.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/board_background.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/button_normal.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/button_normal.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/button_pressed.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/button_pressed.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/hint_circumscribed.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/hint_circumscribed.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/hint_inscribed.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/hint_inscribed.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/icon_failure.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/icon_failure.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/icon_grow.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/icon_grow.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/icon_shrink.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/icon_shrink.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/icon_success.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/icon_success.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/Menu/start_menu_background.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/Menu/start_menu_background.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/Menu/start_menu_button_normal.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/Menu/start_menu_button_normal.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/Menu/start_menu_button_pressed.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/Menu/start_menu_button_pressed.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/power_fill.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/power_fill.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/power_frame.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/power_frame.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/Settings/settings_close_normal.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/Settings/settings_close_normal.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/Settings/settings_close_pressed.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/Settings/settings_close_pressed.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/Settings/settings_icon_music.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/Settings/settings_icon_music.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/Settings/settings_icon_sound.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/Settings/settings_icon_sound.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/Settings/settings_icon_ui_sound.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/Settings/settings_icon_ui_sound.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/Settings/settings_icon_voice_v2.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/Settings/settings_icon_voice_v2.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/Settings/settings_panel.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/Settings/settings_panel.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/Settings/settings_slider_fill.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/Settings/settings_slider_fill.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/Settings/settings_slider_handle.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/Settings/settings_slider_handle.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/Settings/settings_slider_track.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/Settings/settings_slider_track.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/shot_available.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/shot_available.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/shot_used.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/shot_used.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/toggle_active.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/toggle_active.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/toggle_used.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/toggle_used.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/triangle_target.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/triangle_target.png |
| Assets/AssetRaw/UIRaw/Raw/MathBilliards/ui_panel.png | Assets/AssetArt/UIRaw/Raw/MathBilliards/ui_panel.png |
| Assets/AssetRaw/Materials/Billiards/BallShadow.mat | Assets/AssetArt/Materials/Billiards/BallShadow.mat |
| Assets/AssetRaw/Materials/Billiards/BottomSight0.mat | Assets/AssetArt/Materials/Billiards/BottomSight0.mat |
| Assets/AssetRaw/Materials/Billiards/BottomSight1.mat | Assets/AssetArt/Materials/Billiards/BottomSight1.mat |
| Assets/AssetRaw/Materials/Billiards/BottomSight2.mat | Assets/AssetArt/Materials/Billiards/BottomSight2.mat |
| Assets/AssetRaw/Materials/Billiards/BottomSight3.mat | Assets/AssetArt/Materials/Billiards/BottomSight3.mat |
| Assets/AssetRaw/Materials/Billiards/Cushion.mat | Assets/AssetArt/Materials/Billiards/Cushion.mat |
| Assets/AssetRaw/Materials/Billiards/Felt.mat | Assets/AssetArt/Materials/Billiards/Felt.mat |
| Assets/AssetRaw/Materials/Billiards/FrameInlay.mat | Assets/AssetArt/Materials/Billiards/FrameInlay.mat |
| Assets/AssetRaw/Materials/Billiards/MultiBall_BarA.mat | Assets/AssetArt/Materials/Billiards/MultiBall_BarA.mat |
| Assets/AssetRaw/Materials/Billiards/MultiBall_BarB.mat | Assets/AssetArt/Materials/Billiards/MultiBall_BarB.mat |
| Assets/AssetRaw/Materials/Billiards/MultiBall_Mouth.mat | Assets/AssetArt/Materials/Billiards/MultiBall_Mouth.mat |
| Assets/AssetRaw/Materials/Billiards/MultiBall_PredictionDot.mat | Assets/AssetArt/Materials/Billiards/MultiBall_PredictionDot.mat |
| Assets/AssetRaw/Materials/Billiards/RollingBall.mat | Assets/AssetArt/Materials/Billiards/RollingBall.mat |
| Assets/AssetRaw/Materials/Billiards/TableShadow.mat | Assets/AssetArt/Materials/Billiards/TableShadow.mat |
| Assets/AssetRaw/Materials/Billiards/TargetPoint.mat | Assets/AssetArt/Materials/Billiards/TargetPoint.mat |
| Assets/AssetRaw/Materials/Billiards/TopSight0.mat | Assets/AssetArt/Materials/Billiards/TopSight0.mat |
| Assets/AssetRaw/Materials/Billiards/TopSight1.mat | Assets/AssetArt/Materials/Billiards/TopSight1.mat |
| Assets/AssetRaw/Materials/Billiards/TopSight2.mat | Assets/AssetArt/Materials/Billiards/TopSight2.mat |
| Assets/AssetRaw/Materials/Billiards/TopSight3.mat | Assets/AssetArt/Materials/Billiards/TopSight3.mat |
| Assets/AssetRaw/Materials/Billiards/WoodFrame.mat | Assets/AssetArt/Materials/Billiards/WoodFrame.mat |
| Assets/AssetRaw/Materials/Billiards/WorldTint.mat | Assets/AssetArt/Materials/Billiards/WorldTint.mat |
| Assets/AssetRaw/Actor/Billiards/Meshes/TableDisk.asset | Assets/AssetArt/Actor/Billiards/Meshes/TableDisk.asset |
| Assets/AssetRaw/Actor/Billiards/Meshes/TableQuad.asset | Assets/AssetArt/Actor/Billiards/Meshes/TableQuad.asset |
| Assets/AssetRaw/Fonts/NotoSansCJKsc-Regular.otf | Assets/AssetArt/Fonts/NotoSansCJKsc-Regular.otf |
| Assets/AssetRaw/Fonts/NotoSansCJK-LICENSE.txt | Assets/AssetArt/Fonts/NotoSansCJK-LICENSE.txt |
| Assets/AssetRaw/UI/LevelItemWidget.prefab | Assets/AssetArt/UI/LevelItemWidget.prefab |
| Assets/AssetRaw/UI/MainUI.prefab | Assets/AssetArt/UI/MainUI.prefab |
| Assets/GameScripts/HotFix/GameLogic/Core/Level/BilliardsWorld.shader | Assets/AssetArt/Shaders/Billiards/BilliardsWorld.shader |
| Assets/GameScripts/HotFix/GameLogic/Core/Ball/BilliardsBall.shader | Assets/AssetArt/Shaders/Billiards/BilliardsBall.shader |

### 保留在 AssetRaw 的 17 个文件

| 路径 | 使用入口 |
| --- | --- |
| Assets/AssetRaw/Actor/Billiards/BilliardsBlackBall.prefab | 配置 prefabLocation → LoadGameObjectAsync |
| Assets/AssetRaw/Actor/Billiards/BilliardsFlipProp.prefab | 配置 prefabLocation → LoadGameObjectAsync |
| Assets/AssetRaw/Actor/Billiards/BilliardsTable.prefab | 配置 prefabLocation → LoadGameObjectAsync |
| Assets/AssetRaw/Actor/Billiards/BilliardsWhiteBall.prefab | 配置 prefabLocation → LoadGameObjectAsync |
| Assets/AssetRaw/Configs/bytes/billiards_tbball.bytes | ConfigSystem → Tables → LoadAsset<TextAsset> |
| Assets/AssetRaw/Configs/bytes/billiards_tbboard.bytes | ConfigSystem → Tables → LoadAsset<TextAsset> |
| Assets/AssetRaw/Configs/bytes/billiards_tblevel.bytes | ConfigSystem → Tables → LoadAsset<TextAsset> |
| Assets/AssetRaw/Configs/bytes/billiards_tblevelball.bytes | ConfigSystem → Tables → LoadAsset<TextAsset> |
| Assets/AssetRaw/Configs/bytes/billiards_tblevelgeometry.bytes | ConfigSystem → Tables → LoadAsset<TextAsset> |
| Assets/AssetRaw/Configs/bytes/billiards_tblevelpocket.bytes | ConfigSystem → Tables → LoadAsset<TextAsset> |
| Assets/AssetRaw/Configs/bytes/billiards_tblevelprop.bytes | ConfigSystem → Tables → LoadAsset<TextAsset> |
| Assets/AssetRaw/Configs/bytes/billiards_tbphysics.bytes | ConfigSystem → Tables → LoadAsset<TextAsset> |
| Assets/AssetRaw/Configs/bytes/billiards_tbprop.bytes | ConfigSystem → Tables → LoadAsset<TextAsset> |
| Assets/AssetRaw/UI/GameMainUI.prefab | Window 属性 → UIWindow → LoadGameObjectAsync |
| Assets/AssetRaw/UI/LevelSelectUI.prefab | Window 属性 → UIWindow → LoadGameObjectAsync |
| Assets/AssetRaw/UI/SettingsUI.prefab | Window 属性 → UIWindow → LoadGameObjectAsync |
| Assets/AssetRaw/UI/StartUI.prefab | Window 属性 → UIWindow → LoadGameObjectAsync |
