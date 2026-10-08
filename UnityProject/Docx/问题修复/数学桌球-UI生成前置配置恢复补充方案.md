# 数学桌球：UI 生成前置配置恢复补充方案

日期：2026-10-06。状态：用户已确认，指定注册及配置生成恢复已实施，原 UI 生成任务已继续完成；未执行独立验证。

下文保留提出方案时的诊断与授权依据，最终实施结果见[规则对齐实施记录](../重构优化/数学桌球-新增代码规则对齐实施记录.md)。执行时数据表已改名为 Level.xlsx、Ball.xlsx，按当前实际文件名注册，未调整原数据。

## 问题与必要性

原整改方案已确认，只允许修改新增业务源码、三个 UI Prefab 和 UI 生成产物，并明确保护 GameProto、配置源表和模板。源码分类及格式整理已经完成，UI 自动生成尚未完成。

当前 Pipeline 已连接到本工程，Editor 处于 Edit Mode，主场景未修改，没有打开 Prefab Stage。调用项目生成器前的必要读取发现 GameLogic 与 Assembly-CSharp-Editor 程序集未加载；尝试引用 GameLogic 的 eval 返回命名空间缺失，尚未执行 Prefab 写入或 UI 生成。

本次 Console 的已有编译错误是 LevelManager 第 32、34 行引用 Tables.TbLevel 和 Tables.TbBall 时返回 CS1061。GameConfig/Tables.cs 的表区为空；配置源文件 __tables__.xlsx 只有三行表头，Defines 目录无定义文件。两张桌球数据表仍存在，字段、关卡与球参数均有数据，但未注册。

历史日志曾出现向量类型缺失；当前工程没有 ExternalTypeUtil.cs，不能把历史记录当作本次编译错误。现有导表脚本会复制这个模板回来，因此必须避免将模板复制结果写回当前工程，否则会扩大问题。

## 建议授权的最小范围

1. 在 Configs/GameConfig/Datas/__tables__.xlsx 中只追加以下两张桌球表的注册，保留表头和其他内容，数据表字段与数值不变。

| full_name | value_type | read_schema_from_file | input | index | mode | group |
| --- | --- | --- | --- | --- | --- | --- |
| billiards.TbLevel | billiards.Level | true | Level.xlsx | id | map | c |
| billiards.TbBall | billiards.Ball | true | Ball.xlsx | id | map | c |

实际写入字段和输入路径按项目 Luban 工具的既有读取规则执行；不额外新增游戏数据或调整参数。

2. 使用现有 gen_code_bin_to_project_lazyload.bat，在项目 Tools 下的隔离临时工作区镜像输入并导出，保留原脚本和模板内容，不手工拼装 dotnet 导出命令，不将临时配置加载器和 ExternalTypeUtil 复制回工程。
3. 只将这次所需的 GameConfig 自动生成 C# 和 bytes 数据导回 UnityProject：普通 C# 按源码规则写入，新数据经 Pipeline 导入，已有 Unity 资源若需替换则通过 Pipeline 操作，保留既有 GUID。生成目录有无关产物时不自动删除。
4. 不改 ConfigSystem.cs、ExternalTypeUtil、配置模板、UIScriptGenerator、TEngine、UIModule、程序集配置或工程设置。不为通过编译而使用占位表、反射或硬编码关卡。
5. Unity 自动导入完成后，继续原已确认的三窗口生成与业务 partial 迁移，不扩大 UI 需求。

## 规则与操作边界

遵循项目 luban-dev：生成代码由工具输出，禁止手改 GameProto/GameConfig；导出使用现有脚本。技能对配置写入明确要求先确认，本次已在用户同意后实施。

原方案中配置源表位于仓库级 Configs，GameProto 与 bytes 不在原修改白名单内；需要用户明确同意这项新增前置工作。该确认只涉及上述配置恢复范围，不重新请求原 UI 与格式整改的授权。

不会主动执行 recompile、测试、Play Mode、全量验证或额外导表。必要的配置导出是恢复 UI 生成前置条件的制作动作，可能触发 Unity 自动导入和编译。完成后如实报告结果，再询问用户是否需要检查。

若用户选择自行恢复配置，则在配置恢复并使项目生成器程序集可用后继续原 UI 迁移；不直接改写 Prefab YAML、不手写生成绑定作为兜底。
