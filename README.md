# 寻灯者｜Unity 工程源代码快照

文档版本：`v0.3`
快照版本：`v0.43.184`
状态：`PRIVATE_REPOSITORY_SNAPSHOT / VALIDATED_BASELINE`

## 内容范围

本快照保留 Unity 项目重建所需内容：

- `Assets/`
- `Packages/`
- `ProjectSettings/`
- `Documentation/`
- `.vsconfig`

不提交 `Library/`、`Temp/`、`Logs/`、`obj/`、`UserSettings/`、APK/AAB、签名材料、本地生成项目文件、内部协作工具或候选资源归档。

## 验证基线

- Unity：`2022.3.62f3c1`
- 工程版本：`v0.43.184`
- Android bundle version：`0.43.184`
- Android version code：`422`
- EditMode：`302/302 PASS`
- 核心烟测：`26/26 PASS`
- 音频烟测：`10/10 PASS`

## 打开方式

1. 安装 Unity `2022.3.62f3c1`；
2. 在 Unity Hub 中选择仓库根目录；
3. 等待 Unity 重新生成本地缓存；
4. 不要提交 Unity 自动生成的缓存、构建产物或本机用户设置。

## 安全与第三方资源

- 仓库中不应包含 API Key、钱包助记词、私钥、Android Keystore 或本机绝对路径。
- 第三方字体与音频记录见 `Documentation/主程-第三方运行时资源来源与许可-公开说明-v1.0.md`。
- 当前仓库按私有开发仓库管理；改为公开仓库前必须重新执行隐私、许可和二进制资源分发审计。

## 版本记录

| 版本 | 日期 | 变更 |
|---|---|---|
| v0.1 | 2026-09-28 | 创建脱敏的 v0.43.96 早期源代码快照说明。 |
| v0.2 | 2026-09-28 | 增加 Unity 版本、打开方式、缓存排除和第三方署名提示。 |
| v0.3 | 2026-10-01 | 更新为 v0.43.184 私有仓库验证基线，补充当前测试结果与第三方资源说明。 |
