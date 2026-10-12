# Android 正式应用身份与签名配置合同

- 文档版本：v1.2
- 状态：APPLICATION_ID_CERTIFICATE_AND_LOCAL_CACHE_FROZEN
- 更新日期：2026-10-10
- 适用工程基线：v0.43.374 / Android 612

## 已冻结值

| 配置 | 值 |
|---|---|
| Android Application ID | `games.lightseeker.app` |
| 正式 keystore 文件名 | `lightseeker-release.jks` |
| key alias | `lightseeker-release` |
| Store type | `JKS` |
| 密钥算法 | `RSA 4096` |
| 签名算法 | `SHA256withRSA` |
| 有效期 | `36500` 天 |
| 证书 SHA-256 | `26:69:22:23:01:4D:19:C6:E9:9A:84:06:6B:16:A9:D2:5C:FF:0F:2F:02:A3:A8:0B:B4:70:1D:A4:6E:6C:AB:AA` |
| keystore 密码与 key 密码 | 使用同一个 24 位秘密；仅在本机受控进程中解密并临时传入 |

## 本机凭据缓存

1. 首次成功验证 keystore 路径、密码和 alias 后，构建脚本保存当前 Windows 用户专属缓存。
2. 缓存位置为 `%LOCALAPPDATA%/Lightseeker/Signing/android-release-signing-v1.json`，不得复制到仓库、发布包或共享目录。
3. keystore 密码通过 Windows DPAPI 当前用户保护后写入缓存；缓存中不得出现明文密码。
4. keystore 绝对路径只保存在上述本机缓存，不写入仓库、构建日志或交付文档。
5. 后续构建默认自动读取缓存，不再重复询问路径或密码。
6. keystore 路径、密码或 Windows 用户发生变化时，使用构建脚本参数 `-ResetSigningCredentialCache` 删除旧缓存并重新录入。
7. `-UseExistingSigningEnvironment` 继续用于受控的签名轮换流程；该模式不读取或写入本机缓存。
8. DPAPI 保护边界是当前 Windows 用户账户。不得在不可信账户会话中运行正式构建，也不得降低 Windows 账户保护强度。
9. v1.1 是当前实现；已发布的 v1.0 命令保留为兼容入口并自动委托给 v1.1。

## 安全边界

1. 禁止使用 Unity Debug 证书生成正式包。
2. JKS 必须位于工程与 Git 仓库之外，不得复制到仓库、发布 ZIP 或文档目录。
3. keystore 密码、key 密码和私钥不得写入 Unity `ProjectSettings`、仓库、文档或日志。
4. 构建器仅在当前进程环境变量中临时设置签名秘密，并在 `finally` 中清除变量、恢复工程设置和删除 Unity 生成的签名缓存。
5. 只有 keytool 成功验证密码与 alias 后，首次录入的凭据才允许写入 DPAPI 缓存。
6. APK 验收必须核对永久 Application ID、Android 版本码、非调试状态、ARM64、v2 签名和上述证书 SHA-256。

## 版本影响

本次仅调整本机正式构建工具的秘密输入方式，不改变游戏代码、运行时资源、玩家可见内容或生成 APK 的身份参数，因此工程版本保持 `v0.43.374 / Android 612`。

## 版本记录

| 版本 | 日期 | 变更 |
|---|---|---|
| v1.0 | 2026-10-09 | 冻结 Application ID、JKS 文件名、alias 与秘密处理边界。 |
| v1.1 | 2026-10-09 | 记录正式证书参数与 SHA-256；将阻塞收敛为运行端点、赛季 ID 和当次密码注入。 |
| v1.2 | 2026-10-10 | 新增 Windows DPAPI 当前用户凭据缓存；首次成功验证后免除后续重复输入，并提供显式重置入口。 |
