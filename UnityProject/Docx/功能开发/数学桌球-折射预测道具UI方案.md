# 数学桌球：折射预测道具 UI 方案

## 一、任务等级与目标

本任务按 L3 功能处理，涉及 HUD、会话状态、预测表现、Luban 关卡配置和新美术资源。

目标规则：

1. 常态仍显示白球当前方向的第一段预测线。
2. 常态不显示白球在库边或其他球体碰撞后改变方向的后续预测分段。
3. 玩家通过新的 HUD 道具按钮启用“折射预测”。
4. 道具只改变预测信息的可见范围，不改变真实击球、碰撞、反弹、道具触发或结算结果。

本文把“折射预测”定义为：白球在首次改变运动方向的接触事件之后产生的预测续线。该定义同时覆盖库边反射和球体碰撞后的白球续线，避免同一套路径分段出现两种解锁规则。

## 二、现状与问题

当前实现已经具备以下基础：

- `PhysicsWorld.Predict` 在独立世界副本中生成完整预测，不影响真实世界。
- `PredictionPath` 按库边、球体或几何门导致的方向变化拆分显示分段。
- `BoardView` 统一刷新预测虚线和终点圆点。
- `Session` 通过 `SessionEvents.StateChanged` 向 `MainUI` 发布快照。
- `GameMainUI` 是现有 uGUI HUD，右侧栏已有状态、力度和操作按钮。
- 现有 `PropManager` 管理台面中的自动触发道具，不是玩家主动点击的背包/辅助道具系统。

因此，新 UI 道具不应直接伪装成一个台面 `FlipProp`，也不应另建 UI Toolkit 或第二套事件系统。

## 三、推荐交互

### 3.1 HUD 布局

在 `GameMainUI` 右侧栏的“大小趋势”和“力量”之间增加“辅助道具”区域，当前只放一个折射预测槽位：

```text
┌──────────────────────┐
│ 剩余机会  3 / 3       │
│                      │
│ 剩余黑球：2           │
│ 大小趋势：变大         │
│                      │
│ 辅助道具              │
│ [折射图标]  折射预测 ×1 │
│                      │
│ 力量  0%             │
└──────────────────────┘
```

槽位使用图标、文字和状态覆盖层共同表达，不只依赖颜色。交互区域不小于 64×64（以当前 1080p 基准缩放），装饰图片关闭 `raycastTarget`，按钮保留正常、悬停、按下和禁用状态。

### 3.2 状态机

道具 UI 仅有四种显示状态：

| 状态 | 条件 | 表现 | 点击结果 |
|---|---|---|---|
| 可用 | 剩余次数大于零，未启用 | 正常图标、数量 | 进入“已启用” |
| 已启用 | 将作用于下一次有效出杆 | 高亮边框、勾选标识、“已启用” | 再次点击取消，不消耗 |
| 已耗尽 | 剩余次数为零 | 灰度图标、锁/空数量标识 | 无操作 |
| 暂不可用 | 会话正在滚动、暂停或已结算 | 保留当前信息但按钮禁用 | 无操作 |

推荐消费时机：玩家先点击启用；只有 `Session.Shoot` 成功接受出杆后才扣除一次。因白球尺寸冲突等原因被拒绝的出杆不扣道具。这样不会因误点或无效出杆损失次数。

推荐作用周期：只解锁下一次有效出杆的完整折射预测；出杆成功后自动关闭。重新开始关卡时恢复该关配置的初始次数。

> 如果需求实际是“点击一次后整关永久解锁”，只需把消费状态从“下一杆”改为“本关已解锁”，UI 和预测显示接口不变；但必须在编码前确认，不能同时实现两套含义。

## 四、职责设计

### 4.1 配置与状态

在 `Level.xlsx` 增加客户端字段 `predictionPropCount`，由关卡配置可用次数，不在代码中硬编码。Luban 重新生成后，由 `LevelData` 保存只读快照。

运行状态由 `Session` 持有：

- `PredictionPropRemaining`：当前关卡剩余次数。
- `PredictionPropArmed`：是否已启用并等待下一次有效出杆。
- `TryTogglePredictionProp()`：仅在 `Ready` 状态切换启用状态。
- 成功出杆后消费并取消启用。
- `Restart()` 恢复配置初值。

不把该状态写进 `PhysicsWorld.PropUses`，因为后者表示台面实体道具的触发次数；混用会让预测辅助道具与真实模拟状态耦合。

### 4.2 预测表现

`PhysicsWorld.Predict` 继续生成相同的完整物理预测，确保物理逻辑只有一套。限制放在表现层：

- `GeometryGraphic.SetDashedPath` 增加“最大可见分段数”。
- 未启用时只绘制第 1 段；已启用时绘制全部分段。
- 库边接触预览也必须按所属分段过滤，不能隐藏续线却泄露后续碰撞位置。
- 当完整预测包含隐藏分段时，不显示最终停点圆点，避免把不可见路径的终点误认为第一段终点。
- 切换道具状态后立即使 `BoardView` 的预测显示缓存失效，鼠标不移动也能立刻刷新。

该方案只控制可见信息，不改变世界副本的实际模拟结果，更符合单一职责和开闭原则。

### 4.3 UI 与事件

不新增独立 `UIWindow`。折射道具属于对局 HUD，作为 `MainUI` 的一个 `UIWidget` 使用：

- `PredictionPropWidget`：只负责按钮、图标、数量和状态表现。
- `MainUI`：将按钮操作转交给当前 `Session`，并用会话快照刷新 Widget。
- `SessionUISnapshot`：增加剩余次数和启用状态。
- 继续复用 `SessionEvents.StateChanged`；UI 在 `RegisterEvent` 中通过 `AddUIEvent` 自动管理监听生命周期。

不修改 `Module/UIModule/`、现有 UI 基础设施或事件框架。

## 五、美术方案

新增一组与现有紫色菱形道具风格一致的折射预测图标，至少包含：

- 可用/普通状态；
- 已启用高亮状态；
- 已耗尽状态可以复用普通图标并由 UI 灰度、锁图形和数量共同表达，不强制新增第三张纹理。

图形语义建议使用“虚线路径撞库后转向”的符号，不复用现有双向箭头 `toggle_active.png`，避免与尺寸趋势翻转道具混淆。

资源进入现有 `AssetArt/UIRaw/Raw/Props/` 美术源目录，再按项目现有导入流程进入运行时 UI 资源。Sprite 由现有 Prefab 引用或 `SetSprite` 管理，不使用 `Resources.Load`。

## 六、预计修改范围

### 仓库级配置源

- `Configs/GameConfig/Datas/Level.xlsx`
- Luban 生成脚本输出的 `Level.cs`、配置 bytes

### UnityProject 代码

- `Module/LevelModule/LevelData.cs`
- `Module/GameModule/Session/Session.cs`
- `Module/GameModule/Session/SessionUISnapshot.cs`
- `UI/MainUI.cs`
- 新增 `UI/PredictionPropWidget.cs`
- `Core/Level/BoardView.cs`
- `Core/Level/GeometryGraphic.cs`
- `Core/Physics/PredictionPath.cs`（只在库边接触需要记录所属分段时调整）

### Unity 资源

- `Assets/AssetRaw/UI/GameMainUI.prefab`
- 新增 `Assets/AssetArt/UI/Widgets/PredictionPropWidget.prefab`
- 新增折射预测图标源文件及对应导入资源
- 由项目 UI 生成器更新 `UI/Gen/PredictionPropWidget_Gen.g.cs`，必要时同步更新 `MainUI_Gen.g.cs`

Unity Prefab、生成绑定和 Sprite 导入必须通过项目规定的 Unity CLI/Pipeline 完成，避免直接编辑 YAML 或破坏资源引用。

## 七、不会包含的内容

- 不修改真实物理碰撞、球速、反弹或通关规则。
- 不把现有台面 `FlipProp` 改成 UI 主动道具。
- 不新增全局背包、商城、付费或跨关持久化系统。
- 不修改 `Module/UIModule/`。
- 不顺带重构现有 HUD 或预测系统的其他行为。
- 未经明确要求，不主动运行编译、Play Mode、测试或视觉验收。

## 八、成熟方案评估

调研的可复用结论如下：

- Unity 官方 `open-project-1` 使用事件通道隔离运行系统与 UI；本项目已有 `GameEvent`/`AddUIEvent`，采用同一原则即可，不引入它的 ScriptableObject 事件实现。
- Unity 官方 uGUI 文档建议动态/可复用 UI 元素采用独立 Prefab，并在运行时绑定不同图标、文本和操作；本方案使用独立 `PredictionPropWidget`。
- Unity 官方 UI 指引强调沿用项目已存在的运行时 UI 技术；本项目是 uGUI/TEngine，因此不迁移到 UI Toolkit。

参考：

- https://github.com/UnityTechnologies/open-project-1/wiki/Event-system
- https://github.com/Unity-Technologies/uGUI/blob/main/com.unity.ugui/Documentation~/HOWTO-UICreateFromScripting.md
- https://github.com/Unity-Technologies/skills/blob/main/skills/ui/SKILL.md

## 九、确认项

推荐按以下语义实施：

1. 默认只显示第一段预测线。
2. 折射包含库边和球体碰撞后白球改变方向的续线。
3. 道具点击后作用于下一次有效出杆，出杆成功才消耗。
4. 每关次数由 `Level.xlsx` 配置，重新开始关卡恢复初始次数。
5. UI 集成在现有右侧 HUD，不建立独立弹窗。

确认以上五项后再进入代码、配置、美术与 Prefab 制作。
