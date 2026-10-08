# Unity Pipeline 实例发现失败排查与恢复

日期：2026-10-07

## 已确认的现象

- 当前 Unity 主进程打开 `F:\code\Billiards\UnityProject`，进程 ID 为 36180；该 ID 仅代表本次排查时的状态。
- 系统端口列表显示 7800 正由该主进程监听。
- `Library/Pipeline/.unity-pipeline-port` 缺失，普通沙箱和沙箱外只读查询结果一致。
- `unity command --project-path "F:\code\Billiards\UnityProject"` 返回 `No Pipeline instance found for project`。
- Unity CLI 版本为 `1.0.0-beta.12`，项目 Pipeline 包版本为 `0.7.0-exp.1`。

## 判断依据

安装包中的 `Runtime/Models/InstanceDescriptor.cs` 将实例描述写入上述文件，包含项目路径、端口、进程 ID 和认证信息。`Editor/EditorPipelineServer.cs` 在服务启动时生成描述文件。

因此，本次失败发生在实例发现阶段。Unity 已监听端口，但 CLI 缺少发现和认证所需的文件。不能仅凭本次证据判断文件是被删除、被清理，还是因其他生命周期问题而消失。

`EditorPipelineManager.cs` 明确规定 `Port = 0` 表示从 7800–7849 自动分配端口。`Allow Browser Clients` 用于浏览器 Origin 请求，不是 CLI 连接开关。面板中的 Watching 状态属于代码重载监听，不代表 Pipeline HTTP 服务状态。

## 建议恢复步骤

1. 在现有 Editor Pipeline Manager 面板点击 Restart，使服务重新执行实例描述生成流程。
2. 如需检查恢复结果，先检查实例文件是否生成，再在 PowerShell 中执行：

   ```powershell
   unity command --project-path "F:\code\Billiards\UnityProject" --query editor_status
   unity command --project-path "F:\code\Billiards\UnityProject" editor_status
   ```

3. 如果重启后文件仍然没有生成，或生成后再次消失，再排查 Console 中的 `WriteToProjectRoot failed`、`Failed to update instance descriptor` 等信息，以及是否有清理脚本或多个自动化任务操作同一 Editor。
4. 不手工伪造实例文件，不复制或硬编码认证信息，不直接修改包缓存或序列化设置。

## 本次操作边界

只读取项目规则、包源码、日志、进程和端口信息，并执行 CLI 实例发现。未重启服务，未修改业务代码、Pipeline 包或工程设置，未运行编译、测试、Play Mode 或场景验证。恢复方案尚未执行，不能宣称连接已恢复。
