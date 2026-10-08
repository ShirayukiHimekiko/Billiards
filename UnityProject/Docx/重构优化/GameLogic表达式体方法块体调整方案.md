# GameLogic 表达式体方法块体调整方案

## 目标与确认

按项目方法体书写规范，将检查发现的 12 处表达式体方法改为常规 `{ ... }` 块体。用户已在本次会话确认该修改范围。

## 修改范围

以下路径均相对于 `Assets/GameScripts/HotFix/GameLogic/`：

| 文件 | 方法 | 数量 |
|------|------|------|
| `Core/Level/BoardView.cs` | `SetBall`、`ToWorld`、`IsOnTable`、`SetHints` | 4 |
| `Core/Physics/PhysicsWorld.cs` | `Copy`、`Stationary` | 2 |
| `Core/Ball/Components/BallSimulationComponent.cs` | `SetGrowing`、`ResetFeature` | 2 |
| `Module/LevelModule/LevelData.cs` | `ContainsCircle`、`Cross` | 2 |
| `Module/LevelModule/PlacementData.cs` | `Vertex` | 1 |
| `Module/GameModule/Session/Session.cs` | `Publish` | 1 |

## 实施方式与边界

- `void` 方法将原表达式作为块体中的语句；有返回值的方法使用 `return` 返回原表达式。
- 保留方法签名、原表达式、求值顺序、对象初始化内容和 XML 文档注释。
- 排除 `Module/UIModule/`，不调整属性、属性访问器、Lambda 表达式及其他无关代码。
- 此次为低风险的书写形式调整，无需引入设计模式、依赖或新的抽象。

## 检查与验证边界

本轮确认仅授权上述修改。按用户要求，完成后询问是否需要检查；未收到新的明确要求前，不运行额外检查、编译、测试或 Unity。
