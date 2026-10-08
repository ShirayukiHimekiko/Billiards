# 数学桌球：按 luban-dev 对齐业务代码与设计文档

日期：2026-10-06。任务等级：L3。状态：用户已确认；业务入口与文档已修改，生成接口绑定仍待有效结果，未验证。

## 1. 范围与约束

本方案依据项目 `.agents/skills/luban-dev/SKILL.md`、相关 TEngine 规范和当前代码。用户已确认业务代码与文档修改范围。遵守此前要求：不自行改变配表结构、数据、导表脚本、导表参数或模板。未经用户要求，不编译、不运行测试、不启动 Unity，也不执行验证命令。

配置源的实际位置是仓库根目录 `Configs/GameConfig/`，而不是 `UnityProject/Configs/`。生成代码与 bytes 的目标目录在 UnityProject 内。

## 2. 已读取确认的问题

- 当前 ConfigSystem 提供 Instance、Tables 和 Load，不存在 IsReady、Initialize 或 Release。LevelManager 和 GameManager 仍调用这些不存在的接口。
- LevelManager 自行维护两个 bytes 地址、异步加载字典并传给 Initialize。这依赖已撤回的加载器改造，与当前技能指定的 ConfigSystem.Instance.Tables 入口不一致。
- 当前生成的 Tables 没有桌球表成员，现有业务查询使用的 TbLevel、TbBall 缺少对应生成接口。仅删除无效加载器调用不能解决这个问题。
- 当前 ExternalTypeUtil 引用了 GameConfig.vector2、vector3、vector4、vector2int、vector3int；当前生成目录缺少这些类型。这属于配置生成前提问题，不能通过改业务调用解决。
- 当前源表名为 #billiards_level.xlsx 和 #billiards_ball.xlsx。文档仍混用初版表名、已注册生成的历史状态和“待创建”描述，部分文档仍要求实现已撤回的异步缓存方案。

以上结论来自读取现有文件；本轮没有通过编译或运行验证。

## 3. 已确认的业务代码修改

### LevelManager

移除 IsReady、Initialize、Release 调用，以及专为旧接口存在的 bytes 地址数组、字典加载和 LoadBytesAsync。配置查询沿用 ConfigSystem.Instance.Tables，由既有加载器负责读取和缓存。

保留关卡查询、球参数查询、业务数据转换、业务参数校验和台面准备职责。球、Session、UI 继续使用业务快照。现有 LevelManager 已承担配置访问边界，本次两个表的接入不额外新增同职责管理器。

配置查询本身按当前技能采用的懒加载接口执行；移除独立异步 IO 后，将关卡准备方法改为同步方法，并更新 GameManager 的调用。台面和窗口资源的异步流程继续沿用现有接口。取消检查保留在异步加载完成及应用关卡之前的必要位置。

表成员、主键查询和字段映射必须依据有效生成代码确定；不凭 Excel 文件名猜测 API，不通过反射、手写生成类型或硬编码默认关卡绕过缺失的生成结果。

### GameManager

移除 OnRelease 中不存在的 ConfigSystem.Release 调用；只清理业务持有的会话、球、关卡与台面。按 LevelManager 的实际方法签名调整准备流程。

保留现有退出期间的异步取消处理，以及先解除 Runner 绑定再释放会话的顺序。不得恢复关闭游戏时访问已销毁白球的行为。

### 代码规范

仅整理上述改动相关代码的格式、XML 文档注释与局部说明。不添加仅捕获后重新抛出的异常块，不重复上游已保证的条件检查，不扩展为全项目重构。

## 4. 设计文档更新

确认后更新以下文档中与配置有关的段落：

- 架构设计/数学桌球-Luban配置表与数据分层设计.md
- 架构设计/数学桌球-关卡管理器设计.md
- 架构设计/数学桌球-脚本目录归属约定.md
- 架构设计/数学桌球DEMO-v0.2/00-文档导航与设计决策.md
- 功能开发/数学桌球-代码实现方案/01-第一步-可运行的单关卡基础.md
- 功能开发/数学桌球-代码实现方案/05-A1-最小关卡数据管理.md

统一描述为现有加载器入口、实际目录、配置快照边界和有效生成代码前提，撤去将异步字典适配作为当前实施要求的内容。相关流程图同步反映实际调用。

历史验收记录保留原发生时间与结果，并补充当前状态说明，避免将旧版验证结果当作本次修改已通过。之前撤回的修复方案继续保留撤回标记，不重新作为当前设计依据。

## 5. 配置生成前提与规则差异

当前空 Tables 与缺失的向量生成类型，使本轮仅修改业务代码无法形成完整可用的接入。需要先明确由用户提供有效生成结果，或另行授权处理配置生成问题。在该边界明确前，不修改 Excel、Defines、luban.conf、导表脚本、模板或生成目录来迁就业务代码。

如后续授权生成配置，只能使用项目既有导出脚本，不能手拼导出命令，不能手改生成 C#。是否需要调整配置源必须另列具体差异，不能沿用此前已撤回的方案。

项目通用规范要求异步 IO，但本次指定的 luban-dev 技能示例及当前 ConfigSystem 使用同步懒加载。方案按用户要求对齐现有技能与实际接口，保留该基础设施事实，并在设计文档中明确差异；不自行重写加载器模板。

## 6. 成熟方案参考

已查阅官方 GitHub 项目：[TEngine](https://github.com/Alex-Rachel/TEngine) 与 [Luban examples](https://github.com/focus-creative-games/luban_examples)。本项目已有 TEngine 配置集成与 Luban 生成链，适合沿用既有机制。无需引入另一套配置框架、二进制解码器或运行时反射适配。

## 7. 当前交付状态

用户确认后已完成以下修改：

- LevelManager 移除无效加载器接口、硬编码 bytes 地址和字典加载方法，改为同步 LoadLevel，通过 ConfigSystem.Instance.Tables 查询并构建业务快照；保留构造器校验与应用前的取消检查。
- GameManager 同步调用 LoadLevel，移除不存在的 ConfigSystem.Release，保留退出时先解绑 Runner、再释放会话与球、最后清理台面的顺序。
- 更新配置分层、关卡管理器、目录归属、导航、第一步和 5-A1 文档；为历史验收与已撤回方案补充当前状态说明。

现有 TbLevel、TbBall 查询仍沿用旧业务契约，当前空 Tables 不支持它们。尚未进行最终生成接口绑定，也没有解决 ExternalTypeUtil 的缺失类型，不能称为完整可编译或可运行的修复。未执行编译、测试、导表或 Unity 操作。

## 8. 配置生成问题的具体差异

以下仅为读取分析，不包含配置写入授权或已经执行的修改。

| 对象 | 当前事实 | 完整接入需要明确什么 |
|---|---|---|
| 桌球表 | helper 按文件名列出 Tbbilliards_level、Tbbilliards_ball；生成 Tables 没有任何表属性 | 提供有效生成结果，确定实际 Tables 成员与记录类型，再绑定业务；helper 名称不能代替生成代码 |
| 跨表引用 | #billiards_level.xlsx 的 ballConfigId 为 int#ref=billiards.TbBall | 导出定义必须含对应表，或在另行确认后调整引用；本轮保留原值 |
| Unity 类型映射 | ExternalTypeUtil 依赖五种 GameConfig 向量类型，当前 Defines 和生成目录未提供定义 | 提供项目有效类型定义与同次生成结果；若需新增定义，先列出具体 Schema 差异，不手写生成 C# |
| 导表入口 | 当前客户端脚本未显式配置 tableImporter；工具依赖清单记录 Luban/4.10.2 | 核对该工具的实际导入行为与有效导表结果；本轮不更改开关、正则、工具版本或模板 |

官方 [Luban 自动导入文档](https://www.datable.cn/docs/manual/importtable) 说明 # 文件命名与表导入的关系，但不能仅凭文档或 helper 扫描断言本地工具已成功导入。当前没有执行导表，不把“默认关闭导入”或“下划线必然不支持”作为本轮已确认根因；此前已撤回方案的相关结论不能直接复用。

后续有两种明确边界：用户提供有效生成结果时只完成业务绑定；如果有效结果不存在，先按 skill 列出需要变更的配置定义和生成差异，由用户明确该范围后再执行。两种路径都不要求重新引入 IsReady/Initialize/Release 或自定义 bytes 字典协议。
