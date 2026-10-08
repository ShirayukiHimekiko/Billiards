# Unity CLI / Pipeline 间歇不可达排查与修复方案

日期：2026-10-07。任务等级：L2 工具连接排障。本方案仅归档分析，尚未应用配置或代码修改，也未执行并发复现、编译或测试。

## 结论与证据边界

当前 CLI 为 1.0.0-beta.12，项目 Pipeline 为 0.8.0-exp.1。用户观察到多个 AI 任务同时访问同一编辑器时，可达状态变为不可达，编辑器 PID 不变。源码确认命令执行以及 /api/editor_status 共用 m_ExecGate，因此长命令使状态查询排队，可能超过客户端探测时限。这是优先假设，尚未捕获断连时的请求、端口与描述文件快照，不能宣称唯一根因已确认。

此前还观察到端口监听但发现文件缺失。单纯执行锁排队不能解释文件真正消失；应作为独立故障调查。文件被哪个进程删除、是否写入失败或生命周期清理异常均未确认。不能推断某个 AI 删除了文件。

## 文档与源码依据

- Unity 官方 CLI 文档：https://docs.unity.com/en-us/unity-cli/unity-pipeline/unity-pipeline-package
- Unity 官方 CLI 发行说明：https://docs.unity.com/en-us/unity-cli/release-notes
- Unity 官方 CLI 集成说明：https://github.com/Unity-Technologies/skills/blob/main/skills/unity-cli/references/integration-advanced.md
- 项目安装包 Documentation~/connectivity.md：实例描述文件、认证、进度、后台任务、重载与弹窗。
- BasePipelineServer.cs：/api/editor_status 与执行命令共用锁；/api/status 和 /api/progress 提供后台状态；Watchdog 重开死亡监听器。
- EditorPipelineConfig.cs / EditorPipelineStartup.cs：AutoStart、Watchdog、Auto-tick 与请求日志。

## 建议修复顺序

1. 对同一个 Editor 的 Unity 命令建立跨任务串行调度。其他任务可以并行阅读与分析代码；修改 C# 时仍需协调，避免触发另一任务正在使用的 Editor 域重载。约定单一操作者只能降低风险，自动执行需所有任务共用调度入口或跨进程锁。
2. 长命令使用 CLI 的 --detach 和 unity job status/wait 获取结果，或使用命令已有的异步状态接口。后台任务仍串行执行，不能解决吞吐或增加并行度；其用途是避免保持长 HTTP 请求。任务存于 Editor 内存，不跨域重载保留。
3. 检查 Project Settings > Pipeline > Editor 的 Auto Start 和 Watchdog，并检查 Auto-tick 是否被任务关闭。包默认 AutoStart=true、WatchdogEnabled=true、WatchdogIntervalSeconds=5；无配置文件时使用默认值。Auto-tick 开启会带来后台 CPU 开销。Watchdog 只修复死亡监听器，不解决排队、死循环、发现文件权限或所有请求处理故障。
4. 将短暂重载与真实断连分开。重载后使用 unity status --until-ready --project-path <项目路径> --timeout 60 --format json 等待，但 ready 不代表执行队列空闲。不要对超时或掉线的有副作用命令直接重复提交，应先查任务结果和目标对象，避免重复修改。
5. 如果普通终端正常而某个 AI 失败，核对该任务的文件读取、进程查询和本地连接权限。官方说明 Windows 沙箱可能因发现文件 ACL 返回误导性无实例错误。使用单条命令的合规权限路径，不关闭整个沙箱，不放宽认证 token 文件给所有用户。
6. 临时开启 Log Requests Responses，关联 Logs/pipeline.log、当前 Editor.log 和 CLI 日志的时间及任务命令。日志可能包含请求参数或 eval 内容，需限制范围并脱敏。断连时记录描述文件是否存在、PID/端口和监听状态；不输出 evalToken。

## 按故障分类恢复

| 现象 | 恢复方向 |
| --- | --- |
| 文件存在、监听存在、长任务结束后恢复 | 串行调度，长任务结果轮询，按实际耗时调整命令 timeout |
| 重载期间断连，重载后恢复 | 等待 ready，重新发现同一项目，不重放结果未知的修改命令 |
| 仅某个 AI 失败、用户终端正常 | 修复该任务执行环境权限与连接路径 |
| 文件缺失、端口还监听 | 检查写入/清理日志；先保存证据，再 Stop Server / Start Server 恢复发布 |
| 监听消失且 Watchdog 未恢复 | 检查 Watchdog、Auto-tick、编译/导入与恢复失败日志 |
| 端口绑定错误，端口被其他进程占用 | 才考虑调整端口 |

## 修改与验证边界

实施工作流、配置或代码修改前需用户确认。不得直接修改 Library/PackageCache 作为持久修复；升级需先确认新版本有相关修复且兼容。当前 CLI 已是所查官方发行说明顶部列出的 beta.12，无依据承诺盲目升级可以修复。

若用户要求验证，再进行有界并发复现，对照串行与并发、记录各请求起止时间和状态。不要主动触发编译、测试、Play Mode 或更改场景。
