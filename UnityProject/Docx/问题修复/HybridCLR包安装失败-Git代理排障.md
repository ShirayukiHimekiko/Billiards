# HybridCLR 包安装失败：Git 代理排障

日期：2026-10-04。任务等级：L2 环境排障。

## 现象与原因

Unity Package Manager 安装 `com.code-philosophy.hybridclr` 时报告：

```text
Failed to connect to 127.0.0.1 port 61777: Could not connect to server
```

检查发现 Git 全局配置中存在 `http.proxy=http://127.0.0.1:61777`，检查时该端口没有监听服务，Process/User/Machine 范围的 HTTP_PROXY、HTTPS_PROXY、ALL_PROXY 等所查代理环境变量均未检出。此次失败发生在连接本机代理阶段。

已通过只读 Git 查询验证：临时绕过代理后，原 Gitee 仓库能正常返回 HEAD。这项验证不写入 Git 配置，也不安装包。

## 修复步骤

如果不需要 Git 使用代理，在 PowerShell 执行：

```powershell
git config --global --unset-all http.proxy
git ls-remote https://gitee.com/focus-creative-games/hybridclr_unity.git HEAD
```

第一条命令移除 Git 全局 HTTP 代理，对当前用户的其他 Git 仓库也生效。第二条命令应输出提交哈希和 HEAD。

验证成功后，在 Unity Package Manager 重试安装。若仍显示旧错误，保存自己的编辑内容后重新打开 Unity 再重试。

如果仍需要代理访问其他仓库，应启动代理软件并确认其 HTTP 或 mixed 监听端口，再设置 `http.proxy` 为该实际地址。不要直接照搬常见端口号。保留当前配置并启动监听 61777 的原代理服务，也可解决本次代理连接错误。

## 只读诊断与临时验证

```powershell
git config --show-origin --show-scope --get-regexp '(^http\..*proxy$|^https\..*proxy$|^remote\..*\.proxy$)'
git -c http.proxy= ls-remote https://gitee.com/focus-creative-games/hybridclr_unity.git HEAD
```

第二条只对该次 Git 调用禁用 HTTP 代理，不会改变 Unity 后续启动的 Git 子进程配置。Git 的 `http.proxy` 配置说明见 [Git 官方文档](https://git-scm.com/docs/git-config#Documentation/git-config.txt-httpproxy)。

## 本次处理范围

完成代理来源、环境变量、监听端口与 Git 直连验证。

用户随后明确要求执行修复，已运行 `git config --global --unset-all http.proxy`，移除已确认失效的全局代理。重新读取配置确认该项不存在；使用默认 Git 配置执行 `git ls-remote https://gitee.com/focus-creative-games/hybridclr_unity.git HEAD` 成功，返回 `d17a727fac8a56186c35294ec285362902f4a04d HEAD`。验证时没有使用临时代理覆盖。

未修改项目包清单或锁文件。当前环境的 PATH 中未发现 `unity` CLI，依据项目 Unity 操作规范，未代为重试包安装或验证 Unity 包导入结果；需在 Package Manager 重试安装。
