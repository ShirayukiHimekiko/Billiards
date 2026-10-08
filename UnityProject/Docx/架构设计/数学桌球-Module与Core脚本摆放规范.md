# 数学桌球：Module 与 Core 脚本摆放规范

日期：2026-10-07 · 版本：v0.5 · 任务等级：L4 · 状态：用户已确认，七个脚本已通过 Unity Pipeline 完成迁移，未进行功能验证

## 1. 已明确的目录规则

脚本业务根目录为 `Assets/GameScripts/HotFix/GameLogic/`。

**Manager 放在 `Module` 下对应的 `XxxModule` 中；模块自己的管理数据、配置快照、契约及配套实现也放在该模块。球、道具等游戏对象以及具体行为放在 `Core`，按对象或行为职责分类。**

已明确的示例：

| 内容 | 归属 |
|---|---|
| `GameManager` | `Module/GameModule/` |
| `LevelManager`、`LevelData` | `Module/LevelModule/` |
| `BallManager` | `Module/BallModule/` |
| `PropManager` | `Module/PropModule/` |
| `PropBase`、`FlipProp` | `Core/Prop/` |
| 现有 `UIModule` 整个目录 | 原样保留，作为模块内容组织的参考 |

Module 可以服务本游戏，不要求必须跨游戏复用。管理器认识白黑球、球洞或关卡配置，不会因此被划入 Core。

Core 的“核心”指直接实现游戏对象与行为的内容，并非按代码重要程度分类。LevelData 对游戏运行同样必要，但它承担模块的数据契约职责，因此归 Module。基类也按职责判断：PropBase 是道具对象基类，归 Core；UIWindow 是现有 UI 模块提供的窗口基础设施，继续归 UIModule。

本版修正前两版中“Module 只放跨游戏通用能力”“业务管理器全部迁入 Core”“LevelData 留在 Core”的解释。这些旧建议作废，不作为后续实施依据。

## 2. Module 收纳什么

每个 `XxxModule` 是一个系统的组织与管理单元，收纳使该系统完整工作的管理内容，而不是只放一个 Manager 文件。

| 职责 | 放置方式 | 示例 |
|---|---|---|
| 统一创建、查找、持有、同步、回收对象 | 对应模块 | BallManager、PropManager、PocketManager |
| 关卡读取、配置聚合、目录查询及切换组织 | LevelModule 或 GameModule | LevelManager、GameManager |
| 向对象提供初始化参数、配置快照、摆放信息 | 数据所属模块 | LevelData、BallConfigData、关卡摆放数据 |
| 管理流程自身的状态、对外快照和通知 | 流程所属模块 | 会话管理状态及其对外通知；具体建议见第 5 节 |
| 模块公开接口及内部管理辅助实现 | 对应模块 | 模块的查询契约、管理记录、加载辅助 |

模块内部按实际规模使用 `Data/`、`Interfaces/` 等子目录。文件少时可以直接放在模块根目录，不预建空目录，不强制新增接口或空壳 XxxModule 类。

### 2.1 LevelData 为什么放在 LevelModule

当前 LevelData 聚合关卡主信息、台面边界、球体摆放、球洞摆放、道具摆放、图形条件和物理配置，由 LevelManager 组织读取并提供给后续系统。

它回答的是“这一关需要加载和初始化哪些内容、使用什么参数”，属于关卡模块的数据输出；它不是台面实例，也不负责逐帧执行球或道具行为。因此 LevelData 与 LevelManager 放在同一个模块中。

数据构造时包含配置转换、有效性检查或几何预计算，不会自动改变其数据契约归属。本次按当前主要职责分类，不顺带拆改其中的方法。

### 2.2 以 UIModule 为参考

现有 `Module/UIModule/` 一起收纳 UIModule、UIBase、UIWindow、UIWidget、WindowAttribute、IUIResourceLoader、绑定及其他辅助实现，体现了“模块完整收纳自身管理能力和配套内容”的组织方式。

其他模块参考这种完整性，结合自身职责组织 Manager、数据和契约。道具对象的 PropBase 不因也是基类，就要照搬到 PropModule。

**UIModule 的目录、代码、接口和生命周期实现全部保持原样，本方案不对其整改。**

## 3. Core 收纳什么

Core 放被模块管理的游戏对象，以及使游戏实际发生的具体行为、计算和表现。

| 分类 | 内容 | 职责 |
|---|---|---|
| `Core/Ball/` | BallBase、WhiteBall、BlackBall | 球对象自身的初始化、表现与对象行为 |
| `Core/Ball/Components/` | BallComponentBase、BallShootComponent、BallSimulationComponent | 球的击球、模拟等组合能力 |
| `Core/Prop/` | PropBase、FlipProp、具体道具效果 | 道具对象的表现及效果执行 |
| `Core/Level/` | BoardView | 台面场景对象的表现、坐标投影和对象绑定 |
| 图形与球洞对应的 Core 分类 | GeometryView、GeometryGraphic、PocketView | 具体图形及球洞的生成表现与状态显示 |
| `Core/Physics/` | PhysicsWorld、ContactSolver | 运行时模拟、接触和碰撞计算 |
| `Core/Rules/` | GoalJudge、GoalKind、相关求解辅助 | 游戏条件的具体计算与判定 |

对象运行状态和行为参数需要进一步区分：BallConfigData 描述球类型的配置参数，建议归 BallModule；BallState、BallTrajectory 描述球当前运动状态与连续轨迹，直接服务球行为和模拟，建议继续归 Core/Ball。

Core 不是“除 Manager 外所有代码”的剩余容器；Module 也不是“所有 Data 和接口”的统一容器。归属要看内容服务于系统管理，还是服务于对象行为。

例如 IPropEffect 是道具效果执行契约，当前与具体策略一起定义在 PropBase.cs 中，服务真实模拟和预测，应随道具行为保留在 Core/Prop，不为了接口名称把它单独搬到 Module。

## 4. 归属判断步骤

1. **先找职责所有者。** 它属于哪个系统，还是哪个对象或具体行为？
2. **再看它负责什么。** 统一管理一组对象、提供配置和系统契约，归对应 Module；执行对象行为、模拟规则或表现，归 Core。
3. **区分配置与运行状态。** 模块提供的关卡快照和初始化配置放 Module；对象自身当前的位置、速度、轨迹等行为状态跟随 Core 对象。
4. **结合使用链判断。** 被 Core 使用的数据也可以由 Module 定义；被 Manager 创建或调用的对象也可以留在 Core。调用位置不等于文件归属。
5. **混合职责先说明。** 若一个文件同时定义管理数据和具体效果，不凭名称整体归类；在方案中说明哪些内容属于哪一侧，确认后再决定是否拆分。

不使用“能否在另一个游戏直接复用”作为本项目 Module/Core 的分界线；也不使用是否为 MonoBehaviour、是否有方法、是否叫 Base 或 Data 作为唯一标准。

## 5. 现有脚本归属清单

以下基于本会话已读取的源码制定，目标路径相对 GameLogic。用户已确认本版方案，表中原标注“建议”的项目一并纳入实施清单；文件移动进度见第 9 节。

| 脚本或内容 | 归属 | 状态与依据 |
|---|---|---|
| GameManager | `Module/GameModule/` | 按已明确的 Manager 规则保留 |
| LevelManager | `Module/LevelModule/` | 按已明确的 Manager 规则保留 |
| BallManager | `Module/BallModule/` | 按已明确的 Manager 规则保留 |
| PropManager | `Module/PropModule/` | 按已明确的 Manager 规则保留 |
| PocketManager | `Module/PocketModule/` | 按已明确的 Manager 规则保留 |
| GeometryManager | `Module/GeometryModule/` | 按已明确的 Manager 规则保留 |
| LevelData | `Module/LevelModule/` | 用户明确；由当前 Core/Level 迁入此处的方案 |
| PlacementData.cs 内的关卡摆放与配置快照 | 建议 `Module/LevelModule/` | 是关卡聚合数据的组成部分；本阶段可整文件迁入，避免额外拆分类型 |
| BallConfigData | 建议 `Module/BallModule/` | 球类型配置契约，可由关卡聚合数据引用 |
| PropBase、FlipProp | `Core/Prop/` | 用户明确，保持原归属 |
| IPropEffect、FlipPropEffect、PropEffects | 建议保留 `Core/Prop/` | 当前同在 PropBase.cs，承担具体效果契约、策略与执行 |
| BallBase、WhiteBall、BlackBall、球组件 | 建议保留 `Core/Ball/` 及其 Components | 实际球对象与行为 |
| BallState、BallTrajectory | 建议保留 `Core/Ball/` | 球运动状态与轨迹，服务具体行为 |
| BoardView、GeometryView、GeometryGraphic、PocketView | 建议保留现有 `Core/Level/` | 具体对象表现；本次不为细化目录附加迁移 |
| PhysicsWorld、ContactSolver | 建议保留 `Core/Physics/` | 具体运行时模拟与求解 |
| GoalJudge、GoalKind、PolynomialRoots | 建议保留现有 `Core/Rules/` | 判定及其计算辅助；本次不因算法可复用新增 MathModule |
| Session | 建议 `Module/GameModule/Session/` | 管理击球次数、暂停、会话进度及结算，属于会话管理职责，不因没有 Manager 后缀就留在 Core |
| SessionState、SessionUISnapshot | 建议随 Session 放在 `Module/GameModule/Session/` | 会话管理状态与对外快照 |
| SessionEvents | 建议 `Module/GameModule/Events/` | 会话管理对外通知定义 |
| SessionRunner | 建议保留 `Core/Session/` | Unity 场景中的会话帧驱动组件；与管理逻辑分别归属 |
| 现有 UIModule 全部内容 | 原样保留 `Module/UIModule/` | 用户明确，只作参考 |
| 游戏窗口、Widget 及 UI/Gen | 保留现有 `UI/`、`UI/Gen/` | 沿用已有 UI 组织与生成配置，本次不扩大到 UI 路径调整 |

PlacementData.cs 当前同时定义 BallPlacementData、PocketPlacementData、PropPlacementData、GeometryData、PhysicsConfigData。它们共同支撑 LevelData 的聚合输出，建议先整体归 LevelModule；后续有独立模块数据维护需求时，再设计拆分，不在本次文档中默认为已经拆好。

Session 与相关文件的目标位置已随本版方案一并确认：会话管理、管理状态与快照归 Module/GameModule，场景帧驱动 SessionRunner 保留在 Core/Session。

## 6. 目标结构示例

以下展示已确认的目标结构，省略其他脚本、资源和 .meta。目录树是实施目标，实际完成进度见第 9 节。

```text
GameLogic/
├── GameApp.cs
├── GameModule.cs
├── GameLogic.asmdef
├── Module/
│   ├── GameModule/
│   │   ├── GameManager.cs
│   │   ├── Session/
│   │   │   ├── Session.cs
│   │   │   ├── SessionState.cs
│   │   │   └── SessionUISnapshot.cs
│   │   └── Events/
│   │       └── SessionEvents.cs
│   ├── LevelModule/
│   │   ├── LevelManager.cs
│   │   ├── LevelData.cs
│   │   └── PlacementData.cs        关卡摆放与配置快照
│   ├── BallModule/
│   │   ├── BallManager.cs
│   │   └── BallConfigData.cs       球配置契约
│   ├── PropModule/
│   │   └── PropManager.cs
│   ├── PocketModule/
│   │   └── PocketManager.cs
│   ├── GeometryModule/
│   │   └── GeometryManager.cs
│   └── UIModule/                  原样保留，作为参考
├── Core/
│   ├── Ball/
│   │   ├── BallBase.cs
│   │   ├── WhiteBall.cs
│   │   ├── BlackBall.cs
│   │   ├── BallState.cs
│   │   ├── BallTrajectory.cs
│   │   └── Components/
│   ├── Prop/
│   │   ├── PropBase.cs
│   │   └── FlipProp.cs
│   ├── Level/                     台面、图形和球洞的具体表现
│   ├── Physics/
│   ├── Rules/
│   └── Session/
│       └── SessionRunner.cs        场景帧驱动
├── UI/
│   └── Gen/
└── SingletonSystem/
```

根目录 GameModule.cs 是已有模块访问入口，Module/GameModule 是游戏管理内容的分类目录，两者职责不同。目录名不会自动完成 TEngine 模块注册，不将所有 Manager 改名为 XxxModule，也不要求全部改用 ModuleSystem 获取。

## 7. 协作与技术边界

Module 的 Manager 可以持有、创建和调用 Core 对象，负责集合管理和生命周期；Core 对象可以接收模块定义的数据与契约，执行自身行为。

例如 PropManager 使用关卡摆放数据加载道具实例，交给 PropBase 初始化，再由 FlipProp 及其策略执行翻转效果。LevelManager 提供 LevelData，BoardView 与其他对象读取所需配置。

这不采用前两版的“Module 绝对不能引用 Core 类型”规则。当前两目录同属 GameLogic 程序集，职责分类不意味着它们已经是独立程序集的单向依赖层。若将来拆 asmdef，应先设计共享数据与契约，避免直接照目录拆分造成程序集循环引用。

跨系统通知继续遵循项目现有事件机制；对象与所属 Manager 的持有关系继续沿用实际设计。对象不应为了查询全局状态而随意访问所有管理器，也不应重复承担 Manager 的集合管理职责。

TEngine 运行时与编辑器代码、主包启动流程、GameProto/Luban 生成代码、资源及配置源表保留各自技术位置。GameApp、GameModule、SingletonSystem 和现有 UI 生成配置也不因本次分类调整而迁移。

## 8. 参考与设计取舍

沿用本会话查询到的 tengine-dev 架构、模块及热更边界规范，现有实际实现优先。本次目录职责是本项目的约定，不将其表述为 TEngine 的统一强制目录规则。

成熟实现参考沿用本会话已查询的 [TEngine UIModule 源码](https://github.com/Alex-Rachel/TEngine/blob/main/UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIModule.cs)。它用于说明模块应完整收纳自己的管理能力及配套内容，不用于要求各模块跨游戏通用，也不用于修改现有 UIModule。

设计重点是单一职责、数据归属明确和组合复用：管理器负责组织，对象负责行为，配置契约由所属模块维护。保持当前类名、命名空间和公开 API，不为目录规范额外引入接口、单例、注册机制或继承层级。

## 9. 文档关系与实施边界

[旧脚本目录归属约定](数学桌球-脚本目录归属约定.md)中的“Manager 放 Module、对象放 Core”仍与本次要求一致；其中 LevelData 等管理数据放 Core 的历史条目，以本版纠正后的归属为准。旧文档已补充当前归属说明，历史实施状态保留。

2026-10-07 已通过 Unity Pipeline 完成七个脚本迁移，源码未修改或拆分。目标实例为 Unity 6000.5.10f1，项目为 F:/code/Billiards/UnityProject，Pipeline 包版本为 0.8.0-exp.1。

| 文件 | 原位置（相对 GameLogic） | 完成后的位置（相对 GameLogic） |
|---|---|---|
| LevelData.cs | `Core/Level/` | `Module/LevelModule/` |
| PlacementData.cs | `Core/Level/` | `Module/LevelModule/` |
| BallConfigData.cs | `Core/Ball/` | `Module/BallModule/` |
| Session.cs | `Core/Session/` | `Module/GameModule/Session/` |
| SessionState.cs | `Core/Session/` | `Module/GameModule/Session/` |
| SessionUISnapshot.cs | `Core/Session/` | `Module/GameModule/Session/` |
| SessionEvents.cs | `Core/Events/` | `Module/GameModule/Events/` |

操作记录：此前服务端口在监听，但 CLI 所需的实例描述文件缺失，未绕过 Pipeline 移动资源。用户在目标编辑器先 Stop Server、再 Start Server 后，描述文件恢复，CLI 连接成功。

通过 create_folder 创建 Module/GameModule/Session 与 Events，再用 batch 执行 move_asset。批量操作达到时间预算时，前六个文件已成功移动，SessionEvents 被明确标记为跳过。Unity 自动处理资源后连接短暂中断；连接恢复后，仅补移 SessionEvents，没有重复移动已完成的文件。

仅进行了迁移结果确认：七个旧文件路径均已移除，目标文件的 GUID 和 SHA-256 与移动前一致，文件内容没有改变。两个新分类目录及其 .meta 由 Unity 创建。Core/Session 保留 SessionRunner；原 Core/Events 空目录未额外删除。

六个 Manager、PropBase、FlipProp、球对象及组件、UIModule、现有 UI/Gen、程序集和生成器配置保持原样。没有保存场景或 Prefab，没有主动调用 recompile、测试、Play Mode 或功能验证。

资源移动期间 Unity 自身进行了自动资源处理；收尾 editor_status 返回 ready、compiling=false、domainReloadInProgress=false、playMode=stopped。这是操作状态确认，不代表玩法功能已经验证通过。

方案与已确认清单已实施完成，后续编译、测试和专项检查须在用户明确要求后进行。
