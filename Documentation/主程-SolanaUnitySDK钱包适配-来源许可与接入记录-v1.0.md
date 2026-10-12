# Solana Unity SDK 钱包适配来源、许可与接入记录

文档版本：`v1.0`
更新日期：2026-10-05
状态：`ACTIVE / DEVNET INTEGRATION / DEVICE VERIFICATION REQUIRED`

## 1. 接入目的

本次仅为 Android / Solana Mobile Wallet Adapter（MWA）提供钱包连接与 SIWS 消息签名能力，用于 Lightseeker 服务器登录。该接入不开放交易签名、交易发送或 SKR 支付入口，也未修改任何正式美术资源。

## 2. 固定来源

- 上游仓库：`magicblock-labs/Solana.Unity-SDK`。
- 固定提交：`bd0d30f648ed2368c0775e8c2eecd578eb6e4999`，提交内容包含 MWA v2 适配。
- 上游包声明版本：`1.3.0`。
- 下载归档 SHA-256：`4B835A6AE0589A63CC0CF8605603077689ED7DB96B57413119772B25310F8ED1`。
- 工程内嵌位置：`Packages/com.solana.unity_sdk`。
- 本次仅嵌入上游的 `Editor`、`Packages`、`Resources`、`Runtime`、包元数据、README、MIT 许可和第三方声明；未把 Samples、Tests、GitHub 工作流或上游文档目录放入游戏工程。

关键文件校验：

| 文件 | SHA-256 |
|---|---|
| `Runtime/codebase/SolanaMobileStack/SolanaMobileWalletAdapter.cs` | `8997818505BDEA0F319DD00964A5F36CE8818DD681DE44EC9B53A56DFF284F68` |
| `package.json` | `4149E91855EED8E9E5FAFFC4147B62E12014893CD7C698AA4BF8A28B8EC7707D` |
| `Third Party Notices.md` | `FA232D3349ADD0D2E58026007452C36062FC1EBB9208E518217C6855D75CD8DE` |

## 3. 许可

上游包声明采用 MIT License；完整 `LICENSE.md` 与 `Third Party Notices.md` 随嵌入包保留。上游同时包含 Solana.Unity 组件、Bouncy Castle、Chaos.NaCl、NativeWebSocket、websocket-sharp、UniTask、Web3Auth 等第三方组成，其声明以嵌入包内第三方通知为准。

## 4. 工程配置

- Unity 版本：`2022.3.62f3c1`。
- Android 脚本后端：IL2CPP。
- Android 目标架构：ARM64。
- 启用 Custom Main Gradle Template。
- 按上游 Unity 2022 合同固定 AndroidX Browser `1.5.0`、AndroidX Core `1.8.0`、VersionedParcelable `1.1.1`、Guava `31.1-android`，并排除冲突的 `listenablefuture`。

## 5. 安全边界

1. Lightseeker 仅通过 `SolanaMwaSiwsGateway` 暴露 `ConnectAsync`、`SignMessageAsync` 和 `DisconnectAsync`。
2. MWA 授权令牌和钱包选择仅进入进程内缓存，不由 Lightseeker 写入 `PlayerPrefs`。
3. 上游 SDK 仍会把非秘密的公钥暂存于 `PlayerPrefs`；退出时清除，应用重启且内存令牌不存在时由 SDK 清理不一致状态。
4. SIWS 返回值只接受 64 字节 Ed25519 签名，或“原始消息 + 64 字节签名”；组合载荷中的消息必须逐字节与服务器 challenge 一致。
5. 网络固定为 `solana:devnet`；正式主网启用前必须另行评审。
6. 包本身包含更广的链上交易 API，但当前 Lightseeker 适配层不调用、不转发这些 API。

## 6. 已知风险与后续门槛

- 该 MWA v2 实现来自已固定但尚未形成正式 tag 的上游提交，不能自动跟随 `main`，升级必须重新审计并更新哈希。
- 当前尚未在 Seeker 实机完成钱包选择、授权、签名、服务器建会话与退出的端到端验收。
- 正式发布前必须使用最终 HTTPS 身份 URI、应用图标 URI 与服务器域名，并验证钱包展示内容。
- 当前批次不生成 APK，不执行交易，不消耗 SKR。

## 7. 版本记录

| 版本 | 日期 | 变更 |
|---|---|---|
| v1.0 | 2026-10-05 | 固定 Solana Unity SDK MWA v2 提交，记录裁剪范围、许可、哈希、Gradle 合同和认证安全边界。 |
