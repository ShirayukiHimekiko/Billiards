# AssetArt 内部用途分类方案

日期：2026-10-07
任务等级：L3，跨资源目录和制作工具路径调整。
状态：分类迁移和五个工具路径调整已完成；用户确认主动删除两个 UI 模板，保留删除，不恢复。已有选关嵌套 Prefab 缺失引用本次未修复。

## 目标

保留 AssetArt 按资源类型划分的六个顶层目录，在各大类内部按用途或资源族分类。分类采用英文目录名，避免重新引入 Billiards、MathBilliards 等项目名中间层。

沿用本会话已获取的 tengine-dev 资源与 Unity 操作规范。本次为目录整理，不新增游戏功能或引入第三方依赖，复用 Unity CLI/Pipeline 的资源操作能力。

## 材质

| 目标目录（相对 AssetArt） | 资源 | 数量 |
| --- | --- | --- |
| Materials/Common/ | WorldTint.mat | 1 |
| Materials/Table/ | Felt.mat、Cushion.mat、WoodFrame.mat、FrameInlay.mat、TableShadow.mat、TopSight0–3.mat、BottomSight0–3.mat、MultiBall_Mouth.mat、MultiBall_BarA.mat、MultiBall_BarB.mat | 16 |
| Materials/Balls/ | RollingBall.mat、BallShadow.mat | 2 |
| Materials/Aiming/ | TargetPoint.mat、MultiBall_PredictionDot.mat | 2 |

MultiBallAuthoring 中 Mouth 为球洞表现，BarA、BarB 为洞口盖子的两条交叉条，均归入 Table。PredictionDot 用于预测轨迹指示，归入 Aiming。WorldTint 被动态几何、线条等多种表现复用，归入 Common。

## 网格、Shader、字体和 UI 模板

| 目标目录（相对 AssetArt） | 资源 | 数量 |
| --- | --- | --- |
| Meshes/Primitives/ | TableDisk.asset、TableQuad.asset | 2 |
| Shaders/Common/ | BilliardsWorld.shader | 1 |
| Shaders/Balls/ | BilliardsBall.shader | 1 |
| Fonts/NotoSansCJK/ | NotoSansCJKsc-Regular.otf、NotoSansCJK-LICENSE.txt | 2 |
| UI/Windows/ | MainUI.prefab | 1 |
| UI/Widgets/ | LevelItemWidget.prefab | 1 |

两个网格虽带 Table 前缀，但制作脚本也用于球体、球洞和其他表现，按基础形状归入 Primitives。字体和对应授权文件放在同一资源族目录。

## 原始图片

保留 UIRaw/Raw 的资源类型层，在其下直接按用途分类，移除 MathBilliards 中间目录。现有 Menu、Settings 整体移动到新的父目录，保持已有目录和图片 GUID。

| 目标目录（相对 AssetArt） | 资源 | 数量 |
| --- | --- | --- |
| UIRaw/Raw/Common/ | button_normal.png、button_pressed.png、ui_panel.png | 3 |
| UIRaw/Raw/Balls/ | ball_normal.png、ball_inverse.png | 2 |
| UIRaw/Raw/Table/ | board_background.png | 1 |
| UIRaw/Raw/Guides/ | aim_arrow.png、triangle_target.png、hint_inscribed.png、hint_circumscribed.png | 4 |
| UIRaw/Raw/Props/ | toggle_active.png、toggle_used.png、icon_grow.png、icon_shrink.png | 4 |
| UIRaw/Raw/HUD/ | power_fill.png、power_frame.png、shot_available.png、shot_used.png、icon_success.png、icon_failure.png | 6 |
| UIRaw/Raw/Menu/ | 现有 Menu 中全部 start_menu 图片 | 3 |
| UIRaw/Raw/Settings/ | 现有 Settings 中全部 settings 图片 | 10 |

共分类 62 个既有文件：21 个材质、2 个网格、2 个 Shader、2 个字体相关文件、2 个 UI 模板、33 张图片。不创建没有对应资源的空分类。

## 路径同步范围

- Tools/TableAuthoring.cs：分类后的网格、公共材质、桌台材质、球体材质和瞄准材质路径；球面图片与道具图片路径；对应分类目录创建逻辑。
- Tools/MultiBallAuthoring.cs：公共材质、网格路径；球洞及洞口盖子材质和预测指示材质的分类保存路径。
- Tools/InitialDemoAuthoring.cs：字体路径和各图片的分类路径。按具体图片用途更新制作入口，不用递归查找或运行时路径兜底替代准确路径。
- Tools/UI/BuildLevelSelectUI.eval.cs：LevelItemWidget 保存到 UI/Widgets，并同步目录创建逻辑。
- Tools/CampaignChecks.cs：同步 LevelItemWidget 检查路径。本次只改路径，不执行检查工具。

制作辅助函数按调用方明确指定的分类路径保存资源，避免维护一个混合所有资源名称和类型的全局分类器。不更改 Shader.Find 使用的 Shader 内部名称，不改资源文件名。

AssetRaw 的运行时入口和 YooAsset location 不变；通过 GUID 的现有序列化引用由 Unity 资源迁移机制维持。当前 AtlasConfiguration 的输入指向 AssetRaw，输出指向 AssetArt/Atlas，本次不涉及该配置。

## 执行顺序与边界

1. 通过 CLI 发现正确的目标实例，读取操作前编辑器及场景状态。
2. 记录源路径、目标路径和 GUID，确认目标分类目录没有同名资源冲突。
3. 通过 Pipeline 创建分类目录并执行 move_asset；遇到失败停止对应后续操作，根据实际完成结果继续，不盲目重试。
4. 同步以上五个制作或检查工具的路径，不运行工具。
5. 仅通过 Pipeline 删除确认已空的旧目录，保存命令结果和本方案执行状态。

所有 Assets 资源操作均使用 Unity CLI/Pipeline，保留 .meta/GUID。不用文件系统搬运绕过 Pipeline，不覆盖同名目标，不擅自保存或切换用户场景。

按用户既有要求，不主动运行编译、测试、Play Mode、制作工具、检查工具或资源引用完整性验证；完成后报告执行结果和未验证范围，再询问是否需要检查。

## 执行记录

- 通过目标 UnityProject 的 CLI/Pipeline 完成两个迁移批次，分别执行 29 项基础资源移动和 22 项图片/目录移动，涉及 62 个文件。每项 move_asset 命令均返回 success=true；Menu 和 Settings 整体移动，资源 GUID 随资源保留。
- 已同步修改 TableAuthoring.cs、MultiBallAuthoring.cs、InitialDemoAuthoring.cs、UI/BuildLevelSelectUI.eval.cs、CampaignChecks.cs。材质制作辅助函数按调用方指定的用途分类保存，图片入口使用对应分类路径。未运行这些工具。
- 通过 Pipeline 删除已确认为空的 UIRaw/Raw/MathBilliards 目录。
- 发现 Materials 球体分类目录实际大小写为 BallS，直接 rename_asset 因大小写同名冲突被拒绝。通过 Pipeline 将目录先移动到未占用的 BallsCaseMigration，再移动到 Balls；两项操作均返回成功，临时目录已随移动消除，目录 GUID 保持不变。
- 首次资源迁移收尾编辑器为 ready、编辑模式，场景未脏；后续查询显示编辑器进入 playing。本任务未触发、退出或暂停 Play Mode，也未保存或切换场景。
- 收尾日志记录了上述直接大小写重命名失败，以及 LevelSelectUI.prefab 的 Missing Nested Prefab 报错。后者指向 GUID 84db84b7f9a65f2429fe7b365705659d 的 LevelItemWidget。
- 迁移命令结果已确认两个 UI 模板成功移动到 UI/Windows 和 UI/Widgets。随后磁盘和 AssetDatabase 查询发现两个模板及分类目录均已消失。Editor.log 对应报错包含 UnityEditor.ProjectWindowUtil:DeleteAssets 调用；只读查询回收站发现 UI/Windows、UI/Widgets 目录及目录 .meta，表明它们在迁移之后经编辑器删除操作移入回收站。
- 用户明确答复“我主动删除了，保留删除”。因此不恢复 MainUI.prefab、LevelItemWidget.prefab 或其分类目录，也不修改 LevelSelectUI 中的嵌套 Prefab 引用。当前选关资源仍引用已删除模板，相关 Missing Nested Prefab 报错保留；是否清理或替换该依赖属于后续工作。本任务执行的 delete_asset 仅针对空的 MathBilliards 目录。
- 未主动运行编译、测试、构建、制作或检查工具、场景验证和全量资源引用检查。针对已出现的迁移相关错误只做必要定位，不宣称功能验证通过。

执行前清单：`数学桌球-AssetArt内部分类迁移清单.json`。两个迁移批次、空目录清理、目录大小写修正和收尾日志均在本方案同目录保存命令结果 JSON，用于记录执行事实，不是功能测试报告。
