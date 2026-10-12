# Android 正式应用身份与签名配置合同

- 文档版本：v1.1
- 状态：APPLICATION_ID_AND_CERTIFICATE_FROZEN / RUNTIME_ENDPOINTS_PENDING
- 更新日期：2026-10-09
- 适用工程基线：v0.43.364 / Android 602

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
| keystore 密码与 key 密码 | 使用同一个 24 位秘密，仅在本机受控进程中临时传入 |

## 安全边界

1. 禁止使用 Unity Debug 证书生成正式包。
2. JKS 必须位于工程与 Git 仓库之外，不得复制到仓库、发布 ZIP 或文档目录。
3. keystore 绝对路径、密码、key 密码和私钥不得写入 Unity `ProjectSettings`、仓库、文档或日志。
4. 正式构建仅通过当次进程环境变量读取 JKS 路径、alias 和密码；构建器必须在 `finally` 中恢复所有签名与应用身份设置并删除临时运行配置。
5. APK 验收必须核对永久 Application ID、Android 版本码、非调试状态、ARM64、v2 签名和上述证书 SHA-256。

## 当前剩余阻塞

正式 APK 只等待以下非秘密运行参数冻结及密码临时注入：

1. 主网 API HTTPS 地址；
2. 钱包身份 HTTPS 地址；
3. 相对身份图标路径；
4. 首测赛季 ID；
5. 构建时由所有者在受控终端输入签名密码。

## 版本记录

| 版本 | 日期 | 变更 |
|---|---|---|
| v1.0 | 2026-10-09 | 冻结 Application ID、JKS 文件名、alias 与秘密处理边界。 |
| v1.1 | 2026-10-09 | 记录正式证书参数与 SHA-256；将阻塞收敛为运行端点、赛季 ID 和当次密码注入。 |
