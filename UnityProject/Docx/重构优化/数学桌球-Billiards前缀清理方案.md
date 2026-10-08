# Billiards 前缀清理方案

日期：2026-10-07  
任务等级：L3，跨文件命名重构  
状态：用户已确认，已完成命名迁移、Luban 生成及静态引用检查；未运行编译、测试或 Unity 验证。

## 目标与事实

当前业务类型主要位于 GameLogic 命名空间，类名继续重复 Billiards 的收益较低。配置表类名已经使用 TbLevel、TbBall 等名称，多余部分主要是 GameConfig.billiards 子命名空间、生成目录和 billiards_ 数据文件前缀。源 Excel 表本身已使用 Level.xlsx、Ball.xlsx 等名称，无需重命名这些文件。

配置源位于仓库根目录 Configs/GameConfig，UnityProject/Tools/ConfigureBilliards.py 会重新写入 billiards 模块、表注册和引用。因此本任务明确涉及仓库级配置源，需同步处理该目录。

## 脚本命名映射

| 原名称 | 目标名称 |
| --- | --- |
| BilliardsSession | Session |
| BilliardsSessionRunner | SessionRunner |
| BilliardsUISnapshot | SessionUISnapshot |
| BilliardsEvents | SessionEvents |
| BilliardsWorld | PhysicsWorld |
| BilliardsContactSolver | ContactSolver |
| BilliardsGoalJudge | GoalJudge |
| BilliardsLevelData | LevelData |
| BilliardsBoardView | BoardView |
| BilliardsGeometryGraphic | GeometryGraphic |
| BilliardsPlacementData.cs | PlacementData.cs |

SessionEvents、SessionUISnapshot、PhysicsWorld 保留职责含义，避免 Events、World 等名称过于宽泛。PlacementData.cs 当前包含多个不带 Billiards 前缀的数据类，本次只调整文件名，不额外拆分类。

工具脚本同步改名：BilliardsCampaign.py → Campaign.py、ConfigureBilliards.py → ConfigureGameConfig.py、BilliardsTableAuthoring.cs → TableAuthoring.cs、BilliardsMultiBallAuthoring.cs → MultiBallAuthoring.cs、Billiards2DChecks.cs → Board2DChecks.cs、BilliardsShutdownChecks.cs → SessionShutdownChecks.cs、BakeBilliardsUI.cjs → BakeGameUI.cjs。同步调整调用方和导入；检查工具仅维护引用，不执行。

## 配置命名调整

- GameConfig.billiards.Xxx → GameConfig.Xxx，保留 TbXxx 的 Luban 表类型约定。
- Defines/billiards.xml → Defines/game.xml，移除业务专用子模块命名；使用仓库当前 Luban 支持的根模块定义形式。
- __tables__.xlsx 中 billiards.TbXxx、billiards.Xxx 调整为根模块的 TbXxx、Xxx。
- XML 和业务 Excel 中 int#ref=billiards.TbXxx 调整为对应根模块引用。
- GameConfig/billiards/ 中的生成类型迁移到 GameConfig/。
- billiards_tblevel.bytes 等九张表的数据产物去掉 billiards_，目标为 tblevel.bytes、tbball.bytes 等；由生成器确定并与 Tables.cs 加载名称保持一致。
- 更新配置创建工具的模块定义、注册行匹配和输出路径，确保重复运行不会恢复旧前缀，也不会删除其他表的注册。

配置类 Physics 与 UnityEngine.Physics 同名；相关业务引用使用 GameConfig.Physics 等明确限定名称解决歧义，不为此引入统一的新前缀。

## 实施顺序与引用维护

1. 用户确认方案后，读取当前工作区变更，避免覆盖已有修改；确定根模块语法和现有生成入口。
2. 调整业务类、构造函数、文件名和工具脚本引用。Unity 资产脚本改名时同步移动原 .meta，保留 GUID。
3. 更新 Prefab 等序列化文本中的 m_EditorClassIdentifier；当前 BilliardsTable.prefab 存在 BoardView、SessionRunner、GeometryGraphic 的旧类型标识。
4. 更新配置 XML、Excel 注册及引用和配置创建工具。只迁移现有配置，不通过重跑配置创建工具覆盖关卡数据。
5. 通过现有 Luban 生成链路重新生成 C# 和二进制数据，保留当前懒加载模板；不直接手改自动生成类。输出迁移时维护 Unity .meta 的对应关系。
6. 移除本次重命名所取代的旧生成代码和旧数据文件，避免旧类或旧表产物继续被收集。

Luban 生成是此次配置修改的实施步骤，编译、测试和 Unity 运行验证需要用户另行要求。不得仅把旧 .bytes 改名而假定序列化完全兼容；类型全名变化可能影响生成标识，C# 与数据产物必须成套更新。

## 范围边界与风险

- 保留玩法行为、配置数据、字段结构、程序集边界和模块职责；不引入新框架或设计模式。
- 本次范围为脚本和配置表。Actor/Billiards、Materials/Billiards、MathBilliards 美术目录、Prefab 名称和 Shader 路径保持现有名称；它们涉及资源地址，需另行扩大范围后处理。
- 事件类名调整，当前 RuntimeId 字符串 Billiards.StateChanged 保留，避免额外改变事件标识协议。
- 更新直接描述当前生成路径及工具调用的维护文档；历史方案保留原始记录。
- 配置模块移除涉及类型标识和资源地址，风险高于普通文件改名。若已有发布包或历史存档引用旧配置类型，需单独考虑迁移和版本配套发布，当前尚未确认该情况。

## 成熟方案参考

查阅 Luban 官方 GitHub 的定义格式和命名约定。沿用现有 Luban 生成流程：表注册、类型引用和数据输出名称同步迁移，无需引入重命名插件或新依赖。

- https://github.com/focus-creative-games/luban/wiki/define
- https://github.com/focus-creative-games/luban/wiki/best_practices

## 检查边界

本次方案阶段仅进行了规则阅读、文件和引用梳理、GitHub 资料查询；未运行编译、测试、导表、Unity 编辑器或场景验证。实施完成后如实说明未验证，并询问用户是否需要检查。

## 实施记录

- 已按上述映射重命名业务脚本、类及构造函数，并同步模块、UI 和工具中的调用引用。
- 已同步重命名七个工具脚本；ConfigureGameConfig.py 导入 Campaign.py，生成根模块定义 Defines/game.xml；以明确的九张表名称更新注册，不再按 billiards 命名空间筛选。
- 迁移 __tables__.xlsx 的 18 处表名和行类型引用，以及 Level、LevelBall、LevelGeometry、LevelPocket、LevelProp 五张业务 Excel 中的 8 处类型引用，共 26 处。
- Excel 仅合入本次目标字符串，保留原有表格数据、格式及其他 OOXML 部件；没有执行配置创建工具覆盖关卡内容。
- 已维护脚本和生成资产的原 .meta，保留 GUID；同步 BilliardsTable.prefab 的三个组件类型标识。
- 已将配置类型移至 GameConfig 根目录，九个数据文件去掉 billiards_ 前缀。
- 已运行 Configs/GameConfig/gen_code_bin_to_project_lazyload.bat，使用现有 cs-bin/bin 和懒加载模板完成配置代码及数据生成，进程退出码为 0。
- 保留 GameConfig.Physics 的显式类型限定，避免与 UnityEngine.Physics 歧义。
- 资源目录、Prefab 名称、Shader 文件及路径、Billiards.StateChanged 事件标识保持原值。
- 首次实施时未运行额外引用扫描、C# 编译、测试、Unity 导入或场景验证；导表成功不代表这些检查已经通过。

## 续接任务的静态检查记录

用户要求继续未完成的任务后，对本次命名迁移进行静态核对，结果如下：

- 业务代码、工具及配置定义中未发现方案映射中的旧类型名、旧工具入口、GameConfig.billiards 命名空间或 billiards_tb 数据加载名称的残留引用。历史方案、缓存和已有构建产物未纳入当前源码检查。
- 11 个改名业务脚本均有对应 .meta，当前源码范围内未发现这 10 个目标类型的重复定义；PlacementData.cs 为聚合数据类文件。
- Tables.cs 中的九个加载名称均对应当前存在的 .bytes 与 .meta 文件。
- 当前配置源工作簿的 XML 内容中未发现 billiards. 类型或表引用。
- BilliardsTable.prefab 中 BoardView、SessionRunner 及三个 GeometryGraphic 组件的类型标识，与其 m_Script GUID 所指向的当前脚本相匹配。
- 本次未发现需要继续修复的命名迁移遗漏，未追加业务代码或配置数据修改。
- 剩余资源路径、Shader 名称、事件字符串及内部诊断标识中的 Billiards 不属于本次已确认的脚本类型和配置表重命名映射。
- 此次结果仅覆盖静态名称、文件配对及序列化引用关系；未运行编译、自动化测试、Unity 导入、场景或玩法验证。
