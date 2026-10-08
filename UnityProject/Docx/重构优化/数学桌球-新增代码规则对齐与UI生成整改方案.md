# 数学桌球：新增代码规则对齐与 UI 生成整改方案

日期：2026-10-06。任务等级：L3。状态：用户已确认；源码分类、格式整理、三个窗口 UI 自动生成及 Prefab 绑定迁移均已实施，未执行独立验证。

配置前置缺失已按用户另行确认的[补充方案](../问题修复/数学桌球-UI生成前置配置恢复补充方案.md)恢复；原保护边界仅对补充方案指定的注册及生成产物作例外，框架、加载器和模板保持原样。

实际完成范围、未完成项及验证边界见[实施记录](数学桌球-新增代码规则对齐实施记录.md)。

## 1. 目标与依据

按最新规则整改数学桌球方案新增代码：框架代码保持原样；UI 绑定与控件回调声明由项目生成器生成，业务逻辑独立维护；目录按职责分类；代码逐行书写，成员和逻辑段之间保留空行。

依据：项目 AGENTS.md、tengine-dev 的 UI 生命周期、UI 模式、命名、事件与热更规范，code-simplifier，html-to-ugui 及其 UI-DSL 规范，以及《数学桌球-脚本目录归属约定》。Unity 资源写入必须遵循 unity-operations 和 unity-pipeline。

已在 GitHub 查找成熟方案：[TEngine 官方仓库](https://github.com/Alex-Rachel/TEngine)。官方已有 UI 代码自动生成能力；本项目也已经具备 UIScriptGenerator，不引入另一套生成器或修改框架模板。具体 API 以本地实现为准。

## 2. 当前诊断

| 问题 | 当前证据 | 整改方式 | 风险 |
| --- | --- | --- | --- |
| UI 绑定手写 | StartUI、MainUI、SettingsUI 自己实现 ScriptGenerator，按数字索引读取 UIBindComponent | 用现有 UIScriptGenerator 重新生成绑定，与业务 partial 类分离 | 中，必须同步 Prefab 绑定顺序 |
| 生成与业务没有分开 | 三个窗口都是单文件 sealed 类，未建立 UI/Gen | 业务保留 UI，生成文件放 UI/Gen；窗口改为兼容生成器的 partial 声明 | 低 |
| Prefab 缺少生成配置 | 三个 Prefab 的 className、genCodePath、impCodePath 为空 | 仅配置这三个新增 Prefab；MainUI 的类名与 GameMainUI 资源名分别设置 | 中，避免误生成 GameMainUI 类 |
| 控件语义不清 | 设置窗口使用 Audio0～Audio3；暂停按钮文字未检索到专用绑定前缀 | 按实际用途命名 Music、Sound、UISound、Voice；将需刷新的暂停文字纳入生成绑定 | 中，节点改名与引用需一并处理 |
| 成员书写紧凑 | BallConfigData、BallTrajectory、UI 字段使用逗号合并声明；SessionState 枚举值同一行 | 每个字段与枚举值独立一行；方法、属性、逻辑段分开 | 低 |
| 多个业务类型共用文件 | BallConfigData.cs 同时包含 BallState、BallTrajectory；快照文件含 SessionState；判定器文件含 GoalKind | 纯 C# 类型按同职责目录拆成同名文件，保持命名空间及类型名 | 低，不涉及 MonoBehaviour 改名 |
| 新增 Shader 压缩书写 | BilliardsBall.shader、BilliardsWorld.shader 的结构体与属性压在一行 | 仅整理这两个方案新增 Shader 的格式 | 低，保留计算表达式与渲染参数 |

以上是源文件阅读结果，不代表编译、测试或运行验证结果。当前目录无法读取 Git 历史，因此新增范围依据实施文档和明确的业务文件清单界定，不能宣称已与 Git 基线比对。

## 3. 修改白名单与框架边界

业务根目录：`Assets/GameScripts/HotFix/GameLogic/`。

- 管理器：`Module/GameModule/GameManager.cs`、`Module/LevelModule/LevelManager.cs`、`Module/BallModule/BallManager.cs`。
- 新增业务：`Core/Ball/`、`Core/Ball/Components/`、`Core/Level/`、`Core/Rules/`、`Core/Session/`、`Core/Events/` 中实施记录列出的桌球脚本，以及上述两个桌球 Shader。
- UI 业务：`UI/StartUI.cs`、`UI/MainUI.cs`、`UI/SettingsUI.cs`。
- UI 生成产物：`UI/Gen/StartUI_Gen.g.cs`、`MainUI_Gen.g.cs`、`SettingsUI_Gen.g.cs`，实际由现有生成器输出。
- UI 资源：仅 `Assets/AssetRaw/UI/StartUI.prefab`、`GameMainUI.prefab`、`SettingsUI.prefab` 的生成配置、必要节点命名和绑定组件列表。
- 文档：本方案和受本次改动影响的目录说明。

受保护内容：`Assets/TEngine/`、`Module/UIModule/`、`SingletonSystem/`、`GameModule.cs`、`GameApp.cs`、主包启动流程、`Assets/Editor/UIScriptGenerator/`、生成器公共配置、`GameProto/`、包与程序集配置、工程设置、配置源表及导表模板。新增业务即使调用框架 API，也只能调整调用方。

本轮保留既有入口接入，不重新整理框架入口文件；不恢复已撤回的配置或模板改动。工具与历史验收脚本不自动纳入批量格式化。

## 4. UI 实施方案

### 4.1 技能与生成器职责

`html-to-ugui` 的链路是 UI-DSL HTML → 烘焙 JSON → Unity UGUI 节点树。它负责布局生成，不直接生成 C# 绑定脚本。此次保留现有界面布局与美术，不为绑定整改重建整个界面；如必须增加或重制布局，才按该技能生成 HTML/JSON 并导入。

C# 部分使用项目已有 `ScriptGenerator.GenerateCSharpScript`，采用包含监听与 partial 回调的自动生成模式，绑定代码输出到配置指定的 `UI/Gen/`。不手写或拼接生成绑定文件，不修改生成器实现，不复制其他工程的生成产物。

### 4.2 生成步骤

1. 用户确认方案后，通过 Unity CLI/Pipeline 发现目标实例与可用命令，读取目标状态。
2. 仅编辑三个新增 UI Prefab；保留布局、图片、字体和现有对象引用。设置明确的窗口类名、生成目录与业务目录。
3. 规范需要生成绑定的节点前缀；设置音频节点使用四类音频语义，暂停文字采用 Text 前缀。保留资源名 GameMainUI 与业务类名 MainUI 的映射。
4. 使用生成器按真实节点遍历顺序重新收集 UIBindComponent 列表并生成 C#。不能只生成脚本却保留旧索引列表。
5. 将窗口业务类改成 partial，保留 Window 特性：StartUI/MainUI 全屏 UI 层；SettingsUI 非全屏 Top 层。
6. 删除业务文件中的手写字段绑定、ScriptGenerator 和与生成器重复的控件监听注册。实现生成器声明的 partial 回调；业务状态监听继续使用 AddUIEvent。
7. 业务文件仅维护初始化、刷新、业务事件、按钮动作与清理。保持开始、设置、退出、暂停、重开、返回、目标提示和四类音量行为。

生成模式支持不覆盖业务实现文件：设置 `isGenImp=false`，已有业务文件单独迁移。生成文件保持生成器原样，后续重新生成不会覆盖业务逻辑；不为统一格式修改只读生成产物或框架生成模板。

Prefab、节点和绑定字段修改只能通过 Pipeline/Unity API 完成，不用文本替换 YAML。Pipeline 无法可靠完成时，暂停对应资源与生成步骤并报告，不能用手写绑定兜底。

## 5. 分类与格式方案

沿用 `Module/xxModule` 管理器目录和 `Core` 内容分类，不增加无业务需求的管理器、接口、程序集或空目录。

- `BallState.cs`、`BallTrajectory.cs`：从 BallConfigData.cs 拆到 `Core/Ball/`。
- `SessionState.cs`：从快照文件拆到 `Core/Session/`。
- `GoalKind.cs`：从判定器文件拆到 `Core/Rules/`。
- 三个窗口业务文件保留 `UI/`，绑定文件由生成器输出到 `UI/Gen/`。

普通 C# 文件可直接编辑；既有 Assets 资源移动或重命名通过 Pipeline，保留 GUID。拆分纯 C# 类型不更改类型名、字段名、数值或调用接口；新 .meta 由 Unity 生成。MonoBehaviour 不改类名或脚本 GUID。

格式规则：四空格缩进；大括号独立行；每个字段独立声明；枚举值逐项换行；方法与属性之间留空行；不同逻辑段之间留空行；条件体使用清晰的多行结构；长参数、初始化器和复杂表达式按语义换行；XML 注释使用多行形式。Shader 同样展开属性、结构体和函数。

不使用批量正则拆分分号，避免误改 for 循环、字符串和表达式；不对框架目录运行格式化。保留运动公式、连续判定算法、事件优先级、退出清理顺序和取消保护，只做与规范对齐直接相关的调整。

## 6. 已知限制与实施顺序

当前 `GameProto/GameConfig/Tables.cs` 的表区为空，而 LevelManager 使用 TbLevel/TbBall；这是已有的配置生成缺口。此次不修改 GameProto、源表或模板，不把格式与 UI 整改描述为已经修复配置链。

先完成三个 UI 的生成绑定和业务迁移，再整理新增业务类型与格式，最后同步文档。遵守单一职责与组合复用；不为套用设计模式增加额外层级。

UI 生成器内部调用 AssetDatabase.Refresh，可能触发 Unity 自动导入与编译；这是生成动作的工具行为，不能保证生成期间 Unity 不自动编译。不会额外发出 recompile、运行测试、进入 Play Mode 或主动验证命令。

## 7. 交付与验证边界

确认前只完成只读诊断与本方案归档，不修改 C#、Shader、Prefab 或工程设置。

确认后交付：按分类整理的新增业务代码、由工具生成的三个窗口绑定文件、同步的三份 UI Prefab 配置与必要命名，以及实际改动清单。

用户未明确要求验证时，不主动执行编译、测试、场景运行、额外检查或导表；如实报告未验证状态，完成后询问是否需要检查。不会用历史验收记录作为本轮修改已验证的证据。
