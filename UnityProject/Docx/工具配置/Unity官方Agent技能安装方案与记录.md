# Unity 官方 Agent 技能安装方案与记录

## 任务与范围

- 用户请求：将 https://github.com/Unity-Technologies/unity-agent-plugin 的 Codex 技能下载到本项目。
- 任务等级：L2，项目级 AI 工作流安装；已读取 tengine-dev 技能及项目结构规范。
- 安装目录：`UnityProject/.agents/skills/`。
- 安装方式：使用 skill-installer 的 GitHub 安装脚本，通过 `--dest` 指定项目目录。
- 不安装全局插件，不修改游戏代码、场景、资源、Unity 包或既有技能。

## 上游与适配

- 来源仓库：`Unity-Technologies/unity-agent-plugin`。
- 来源分支：`main`。
- 固定提交：`cf6b2da24e424b0a60d560a57f39f676cb6f79f3`。
- 下载日期：2026-10-07。
- 本次来源目录中包含 32 个带有 `SKILL.md` 的技能；全部下载，并保留其随附参考文件和脚本。
- 上游声明适用 Unity 6+；当前项目记录的编辑器版本为 `6000.5.10f1`。
- 现有项目技能目录与这 32 个技能无重名，采用新增安装，禁止覆盖既有目录。
- 上游技能使用时仍服从项目 AGENTS.md 和用户要求，尤其是修改前确认、TEngine 规范、仅通过 CLI 操作 Unity、禁止未经请求运行编译和验证。
- 部分技能执行时需要 Unity CLI、Pipeline 或对应 Unity 功能包；下载技能不代表已经安装或验证这些依赖。

## 执行步骤

1. 从上述固定提交下载 32 个技能目录到项目技能目录。
2. 保留上游许可和安装来源清单，方便后续追踪版本。
3. 汇报安装工具返回的结果，不运行上游检查脚本、Unity、编译或测试。

## 安装状态

安装工具返回退出码 0，并逐项报告 32 个技能均已安装到项目技能目录。

已另存上游原始许可至 `.agents/skills/unity-agent-plugin.LICENSE.md`，来源清单至 `.agents/skills/unity-agent-plugin.source.json`。

按照用户要求，没有运行额外检查、编译、测试、Unity 场景或上游验证脚本；尚未验证技能调用及外部工具依赖。

## 加载与使用

Codex 官方文档说明，其从当前工作目录向上扫描 `.agents/skills`，不会向下扫描子项目。因此应以 `UnityProject` 或其子目录作为 Codex 工作目录使用这些项目技能；从仓库根目录 `Billiards` 启动时，不能假定自动发现子目录内的技能。

技能可在下一轮加载；若没有出现在技能列表中，可重启 Codex 后再查看。

参考：
- 上游安装说明：https://github.com/Unity-Technologies/unity-agent-plugin
- OpenAI Docs 技能加载规则：https://learn.chatgpt.com/docs/build-skills

## 已安装技能

1. `2d-pixel-perfect`
2. `asset-transformer-toolkit`
3. `audio-setup-mixers`
4. `build-gtk`
5. `build-live-game`
6. `create-2d-physics`
7. `create-tile-palette-or-rule-tiles`
8. `generate-editor-search-query`
9. `implement-in-app-purchases`
10. `initialize-ai-navigation`
11. `levelplay-unity-integration`
12. `localization`
13. `manage-sprite-atlas`
14. `migrate-birp-to-urp`
15. `new-unity-project`
16. `optimize-audio`
17. `optimize-text-mesh-pro`
18. `optimize-web`
19. `physics-3d-collision`
20. `setup-multiplayer-services`
21. `setup-vivox-voice-chat`
22. `shader-graph-create-custom-node`
23. `sprite-editor`
24. `sprite-segment-3x3grid`
25. `ui-imgui`
26. `ui-ugui`
27. `ui-uitk`
28. `ui`
29. `unity-cli`
30. `unity-package-management`
31. `urp-postprocessing`
32. `validate-urp-render-graph-renderer-feature`
