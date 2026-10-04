# Soundimage 三场景背景音乐运行时接入记录

文档版本：v1.0
更新日期：2026-10-02
状态：ACTIVE / RUNTIME_INTEGRATED
来源角色：规划师 + 主程

## 1. 接入范围

工程版本 `v0.43.209` 建立唯一三场景背景音乐路由：

| 音乐上下文 | 运行时曲目 | 覆盖场合 |
|---|---|---|
| 通用主页 | `The Wizard's Concoction` | 主页、商城、卡组、港口、仓库、竞技未开战页面及其他非远征页面 |
| 远征探索 | `The Voyage Begins` | 远征地图、路线选择、事件、商店、补给、奖励与结算等非备战、非战斗状态 |
| 备战与战斗 | `Fantasy Forest Battle` | 远征备战、远征战斗及竞技实际战斗 |

## 2. 正式运行时文件

| 文件 | 字节 | SHA-256 |
|---|---:|---|
| `BGM_EXPEDITION_THE_VOYAGE_BEGINS_30S_LOOP_SOUNDIMAGE_V02.ogg` | 726785 | `8CB49BCDA54722959E19370785AB4292871A93FD09710F2678DDD9C652B401EC` |
| `BGM_BATTLE_FANTASY_FOREST_BATTLE_30S_LOOP_SOUNDIMAGE_V02.ogg` | 615437 | `5F7B36655C3E09D7AB56DB84AE793CFEE03B61AA0CA02CA03969CA474B4CC559` |

两份文件均从本归档目录 `delivery/` 原样复制到 `Assets/Resources/Audio/Music/`，复制前后 SHA-256 一致。主页曲继续使用已接入的 V01 资源。

## 3. 播放规则

1. 同一音乐上下文内页面切换不重启曲目。
2. 音乐上下文改变时，新曲强制从 `0` 秒播放。
3. 使用两个 `AudioSource` 做等功率交叉淡化；首次播放淡入 `1.0` 秒。
4. 主页到远征为 `1.0` 秒，远征到战斗为 `0.8` 秒，战斗回远征为 `1.5` 秒，其他切换为 `0.8–1.2` 秒。
5. V02 文件自身已包含循环边界处理；运行时代码不在单曲循环边界重复添加淡化。
6. 主音量、音乐音量和静音仍由统一音频混音分组控制。

## 4. 许可与署名

- 作者：Eric Matyas
- 网站：https://soundimage.org/
- 作品页：https://soundimage.org/epic-battle/
- 许可：Soundimage International Public License
- 许可页：https://soundimage.org/sample-page/

游戏内致谢明确标注两首 V02 资源经过取段、循环交叉淡化与 Ogg 转码。

## 5. 资源生命周期核查

1. 规划师 v0.1 完整曲目仅位于非构建归档区，未复制进 `Assets/Resources`。
2. 运行时只接入 v0.2 两份短循环；不存在需要从运行时目录删除的旧远征或战斗曲目。
3. 主页原有曲目仍被通用页面路由使用，不属于废弃资源。
4. `outputs/audio` 和 `Documentation/RuntimeResourceSources` 保留来源、许可、试听及复现价值，不参与运行时瘦身。

## 6. 版本记录

| 版本 | 日期 | 变更 |
|---|---|---|
| v1.0 | 2026-10-02 | 接入主页、远征与战斗三场景音乐；记录路由、淡化、哈希、许可和旧资源退出构建核查。 |
