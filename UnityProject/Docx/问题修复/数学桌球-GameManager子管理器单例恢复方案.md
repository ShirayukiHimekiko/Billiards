# GameManager 子管理器单例恢复方案

## 现状与问题

`GameManager` 继承项目的 `Singleton<GameManager>`，但它持有的 `LevelManager`、`BallManager`、`PropManager`、`PocketManager`、`GeometryManager` 和 `TerrainManager` 当前均为普通类，并在字段初始化时直接 `new`。因此，当前源码并不满足“这些 Manager 已经是单例”的前提；只把 `new` 改成 `.Instance` 会缺少对应属性。

项目已有 `Singleton<T>` 和 `SingletonSystem`，负责 `Instance` 创建、登记与全局退出时释放，应沿用这一实现。

## 修改方案

1. 六个 Manager 分别继承 `Singleton<自身类型>`，`GameManager` 的六个字段改为获取对应 `Instance`，保持现有字段和调用顺序。
2. `BallManager`、`PropManager`、`PocketManager`、`GeometryManager` 和 `TerrainManager` 当前的 `Release()` 用于**每关资源回收**；将其改名为 `ClearLevel()`，同步更新 `GameManager.Cleanup()` 的调用。`LevelManager.UnloadLevel(BoardView)` 保持原名。
3. 六个 Manager 在 `OnRelease()` 中执行各自的每关清理（`LevelManager` 仅清除当前关卡状态和目录缓存）。这样切关或返回菜单只清理关卡内容，`SingletonSystem.Release()` 才销毁单例本身。
4. 保留 `Session` 构造函数现有的 Manager 参数传递方式，不扩大改动到会话内部。

## 生命周期边界

- 进入关卡：沿用六个已登记的单例实例初始化本关数据。
- 退出关卡或进入下一关：`GameManager.Cleanup()` 清除每关对象和引用，单例继续可用。
- 退出游戏或热更域释放：`SingletonSystem.Release()` 调用各单例的 `Release()`，执行 `OnRelease()` 并注销实例。

## 范围与检查

修改范围为六个 Manager 类与 `GameManager.cs`，不触及 UI、资源文件或 `.meta`。按项目规则，本次不主动运行编译、测试或 Unity 验证；实现后如需检查再单独执行。
