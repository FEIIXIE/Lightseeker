# Android 正式应用身份与签名配置合同

- 文档版本：v1.0
- 状态：APPLICATION_ID_FROZEN / RELEASE_BUILD_BLOCKED
- 更新日期：2026-10-09
- 适用工程基线：v0.43.363 / Android 601

## 已冻结值

| 配置 | 值 |
|---|---|
| Android Application ID | `games.lightseeker.app` |
| 正式 keystore 文件名 | `lightseeker-release.jks` |
| key alias | `lightseeker-release` |
| keystore 密码与 key 密码 | 使用同一个 24 位秘密，仅在本机受控环境传入 |

## 安全边界

1. 禁止使用 Unity Debug 证书生成正式包。
2. JKS 必须位于工程外的本机受控位置，不得复制到仓库、发布 ZIP 或文档目录。
3. keystore 密码、key 密码、私钥不得进入聊天、仓库、文档或日志。
4. 正式构建只通过受控环境变量读取 JKS 绝对路径、alias 和密码。

## 当前阻塞

在以下材料齐全前，正式 Android 构建保持暂停：

1. `lightseeker-release.jks` 已在本机受控位置生成；
2. 提供 JKS 的本机绝对路径；
3. 提供证书 SHA-256 指纹；
4. 构建前预检确认 Application ID、alias、证书指纹与密码注入均正确。

## 恢复构建时的工程操作

1. 通过构建环境注入 `games.lightseeker.app`、JKS 绝对路径与 `lightseeker-release` alias。
2. 将密码仅注入当次本机构建进程，不持久化到 Unity 工程。
3. 分配新的唯一工程版本号与 Android `versionCode`。
4. 运行版本一致性、签名、APK 静态、实机安装升级和冷启动检查。
5. 交付 APK SHA-256、证书 SHA-256 指纹和兼容性说明，不交付任何秘密。
