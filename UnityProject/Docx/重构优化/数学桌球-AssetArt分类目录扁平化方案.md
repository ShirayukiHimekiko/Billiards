# AssetArt 分类目录扁平化方案

日期：2026-10-07
任务等级：L3，涉及跨资源目录迁移和制作脚本路径调整。
状态：已完成制作脚本路径调整、资源迁移及空目录清理；未执行功能验证。

## 目标与范围

移除 AssetArt 内名为 Billiards 的冗余目录，直接以资源类型组织目录；Meshes 作为 AssetArt 下的独立大类。

| 原目录 | 目标目录 | 处理方式 |
| --- | --- | --- |
| Assets/AssetArt/Actor/Billiards/Meshes/ | Assets/AssetArt/Meshes/ | 整体移动 Meshes，保留目录及资源 GUID |
| Assets/AssetArt/Materials/Billiards/ | Assets/AssetArt/Materials/ | 将其中材质移到父目录，删除空 Billiards 目录 |
| Assets/AssetArt/Shaders/Billiards/ | Assets/AssetArt/Shaders/ | 将其中 Shader 移到父目录，删除空 Billiards 目录 |

网格迁移后删除空的 Actor/Billiards 和 Actor 目录。目标同名冲突时停止对应迁移，不覆盖现有资源。

最终顶层分类包含 Fonts、Materials、Meshes、Shaders、UI 和 UIRaw。
本次处理名为 Billiards 的目录；UIRaw/Raw/MathBilliards 的 UI 分类、AssetRaw 内运行时入口以及资源文件名保持现状。

## 同步修改

- Tools/TableAuthoring.cs：MeshContent 改为 Assets/AssetArt/Meshes/，MaterialContent 改为 Assets/AssetArt/Materials/；目录创建逻辑直接创建 Meshes 和 Materials，不再创建 AssetArt/Actor 或 Billiards 中间层。
- Tools/MultiBallAuthoring.cs：网格目录及 WorldTint、MultiBall 材质路径同步改为新的分类目录。
- Shader 内部名称是 Shader.Find 的查找标识，与磁盘目录不同，不因目录迁移而修改。
- 旧迁移方案和执行记录保留为历史记录，当前目录规范以本方案为准。

## 执行方式与风险

依照 tengine-dev 的资源和 Unity 操作规范，既有 Assets 资源移动、空资源目录删除通过 Unity CLI/Pipeline 执行，保留 .meta 和 GUID。制作脚本通过普通文件编辑修改，不执行制作脚本。

执行前读取 unity-pipeline 技能并发现目标项目实例和实际支持的命令。若技能缺失或 Pipeline 无法连接，不用文件系统搬运绕过项目规范；保留方案并说明执行限制。

主要风险是硬编码路径遗漏及目标同名资源冲突。已有分析已定位两个制作脚本中的相关路径；实施时迁移范围以本方案列出的三个源目录为限。

本次为既有资源分类整理，无新增功能或第三方依赖，复用 Unity 资源移动能力，不引入外部 GitHub 方案或额外抽象。

## 执行进度

### 当前完成结果

2026-10-07 用户重启 Pipeline 服务并要求继续后，沙箱外 CLI 成功发现目标 UnityProject 实例。操作前编辑器为 ready、编辑模式，当前加载的场景未脏，资源操作根目录为 Assets。

- 通过 Pipeline batch + move_asset 完成 24 项移动操作：整体移动包含 TableDisk.asset、TableQuad.asset 的 Meshes 目录，移动 21 个材质和 2 个 Shader 到分类根目录。所有子命令均返回 success=true，资源移动使用保留 GUID 的 AssetDatabase.MoveAsset。
- 确认三个 Billiards 中间目录为空，且 Actor 中没有其他内容后，通过 Pipeline batch + delete_asset 删除 Actor/Billiards、Materials/Billiards、Shaders/Billiards，以及腾空的 Actor 目录。4 项删除操作均返回 success=true。
- 当前磁盘顶层分类为 Fonts、Materials、Meshes、Shaders、UI、UIRaw；Meshes 中包含 TableDisk.asset 和 TableQuad.asset。制作脚本路径修改已于前序执行完成，本轮没有运行制作脚本。
- 未切换或保存场景，未进入 Play Mode，未主动执行刷新、编译、测试、构建、Shader 渲染或资源引用完整性验证。
- 两个批处理命令均已返回明确的完成结果。随后收尾查询 editor_status、list_open_scenes、console_status 时，CLI 再次返回 No Pipeline instance found；因此编辑器最终状态和本轮 Console 新增情况未确认，不宣称功能验证通过。

本次命令结果保存于同目录的 `数学桌球-AssetArt分类目录迁移命令结果.json` 和 `数学桌球-AssetArt空目录清理命令结果.json`。结果是操作执行记录，不是功能测试报告。

### 历史连接阻碍与准备记录（资源操作已于上文完成）

- 已修改 Tools/TableAuthoring.cs 和 Tools/MultiBallAuthoring.cs 的网格、材质路径，并移除 TableAuthoring 中多余的 Actor/Billiards、Materials/Billiards 建目录逻辑。未运行这些制作脚本。
- Unity CLI 在沙箱内和沙箱外均返回 No Pipeline instance found。沙箱外只读查询确认目标项目 Unity Editor 正在运行，Pipeline 包版本为 0.7.0-exp.1，但实例描述文件 Library/Pipeline/.unity-pipeline-port 缺失，CLI 报告服务器不可达。
- Assets 下资源未移动，空目录未删除；需恢复 Pipeline 服务发现后继续执行。两个制作脚本在迁移完成前依赖尚未就位的新目录。
- 用户要求继续任务后，再次通过沙箱内外的 CLI 实例发现和 CLI 状态查询尝试连接，仍返回 No Pipeline instance found。已准备数学桌球-AssetArt分类目录迁移清单.json，记录 25 个资源的源路径、目标路径与现有 GUID；该清单仅为迁移准备，尚未执行资源操作。
- 未运行编译、测试、Play Mode、场景加载或资源引用验证。

### 用户重启后继续执行的连接结果

2026-10-07 用户通知已重启 Pipeline 服务并授权继续。再次只读查询发现目标 Editor 已变为新进程，Pipeline 包现为 0.8.0-exp.1（由用户侧更新，本任务未安装或升级）。新 Editor 进程在本机 7800 端口有监听，但 Library/Pipeline/.unity-pipeline-port 仍缺失。

沙箱内外的 unity command --project-path 均返回 No Pipeline instance found；unity status --port 7800 --json 返回 STATUS_NO_INSTANCES。端口有监听不能证明 CLI 已取得可用项目实例。因此仍未移动 Assets 下资源、未删除资源目录，也未重新运行制作脚本或验证操作。已请求用户提供当前 Editor 经菜单停止/启动 Pipeline 后的 Console 启动结果日志，以进一步确定缺失实例描述文件的原因。

### 服务重启后的续办记录

用户已重启编辑器并继续任务。当前项目实际安装的 Pipeline 已变为 0.8.0-exp.1，CLI 版本为 1.0.0-beta.12。本次未修改包配置。重新连接在沙箱内和沙箱外仍返回 No Pipeline instance found；Library/Pipeline/.unity-pipeline-port 仍缺失，资源尚未迁移。

新版本源码仍会在服务启动时创建描述文件；当前日志含 Start HTTP server 记录，未发现描述文件写入失败日志。沙箱内 CLI 误清理实例描述文件只是待确认的解释，尚未证实。后续发现实例和执行迁移均使用沙箱外 CLI，避免进程可见性限制。

已新增执行前清单 `数学桌球-AssetArt分类目录扁平化迁移清单.json`，记录 2 个网格、21 个材质、2 个 Shader 的原路径、目标路径和原 GUID，以及 Meshes 目录原 GUID。执行前目标路径均未存在同名文件。该清单不表示引用完整性或运行验证通过。

用户再次启动服务后，本轮首次重连仅在沙箱外执行，仍返回 No Pipeline instance found，描述文件仍缺失；项目日志没有新的 Pipeline Server 启动记录。现有证据不足以认定是沙箱清理导致的问题，等待用户提供 Start Server 后 Console 的完整提示以定位连接阻碍，不继续要求重复重启。
- 用户重启 Pipeline 后再次连接：目标 Editor 正在运行，CLI 查询到 Pipeline 包已更新为 0.8.0-exp.1，主进程存在 7800 端口监听；但实例描述文件仍缺失，CLI 在沙箱外等待就绪后仍返回 No Pipeline instance found。当前安装包的 connectivity.md 明确仍使用 Library/Pipeline/.unity-pipeline-port 进行实例发现，因此尚不能归因于版本导致的描述文件路径变化。需要 Unity Console 的实际启动提示进一步定位；本轮未追加资源或脚本修改。
- 用户要求继续后，已重新尝试项目实例发现；用户随后确认已重启 Pipeline 服务。再次发现仍返回 No Pipeline instance found，目标进程有 Pipeline 端口监听，但实例描述文件仍不存在；CLI 按监听端口查询也未返回可用实例。服务重启未恢复 CLI 连接，具体原因尚未确定。未自动关闭、重启或保存 Unity Editor；资源迁移继续等待连接恢复。

## 验证边界

遵循用户要求，不主动运行编译、测试、Play Mode、场景加载或额外引用验证。执行命令的结果确认属于操作收尾，不代表功能验证通过。完成后报告实际修改与未验证范围，并询问是否需要检查。
