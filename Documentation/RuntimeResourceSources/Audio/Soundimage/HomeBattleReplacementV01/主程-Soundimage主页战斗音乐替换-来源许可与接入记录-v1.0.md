# Soundimage 主页与战斗音乐替换来源许可及接入记录

文档版本：v1.0
更新日期：2026-10-02
状态：ACTIVE / RUNTIME_INTEGRATED
来源角色：主程

## 1. 替换范围

工程版本 `v0.43.210` 执行以下替换：

| 上下文 | 新曲目 | 运行时文件 | 官方作品页 |
|---|---|---|---|
| 主页及通用页面 | `Kingdom Quest` | `BGM_HOME_KINGDOM_QUEST_SOUNDIMAGE_V01.ogg` | https://soundimage.org/fantasy-11/ |
| 实际战斗 | `Training` | `BGM_BATTLE_TRAINING_SOUNDIMAGE_V01.mp3` | https://soundimage.org/sports/ |
| 远征及远征备战 | `The Voyage Begins` | `BGM_EXPEDITION_THE_VOYAGE_BEGINS_30S_LOOP_SOUNDIMAGE_V02.ogg` | https://soundimage.org/epic-battle/ |

远征备战不再切换战斗曲；竞技排名、对手详情、确认及阵容编辑继续使用主页曲，只有实际竞技战斗才使用 `Training`。

## 2. 新文件审计

| 文件 | 格式 | 时长 | 音频规格 | 字节 | SHA-256 |
|---|---|---:|---|---:|---|
| `Kingdom-Quest.ogg` | Ogg Vorbis | 34.32 秒 | 44.1kHz / stereo | 1720033 | `99F9841388F0517757F168314F0C7D55E519CE14D25F9646207C3AE5EFCA9976` |
| `Training.mp3` | MP3 | 37.96 秒 | 44.1kHz / stereo / 192kbps | 918814 | `6798F27610B77234D22E646917DE6D33FE31059AB888E7A51C405077124CA406` |

两份运行时文件均由用户交付文件原样复制，只修改运行时文件名；未裁剪、转码、调速、调音高或归一化。复制前后 SHA-256 一致。`Training.mp3` 内嵌元数据记录作者与作曲者为 Eric Matyas、标题为 Training、版权年份为 2023。

## 3. 来源与许可

- 作者：Eric Matyas
- 网站：https://soundimage.org/
- 许可：Soundimage International Public License
- 许可正文：https://soundimage.org/sample-page/
- 署名要求：https://soundimage.org/attribution-info/

Soundimage 官方页面将 `Kingdom Quest` 与 `Training` 均标注为循环曲目。游戏内必须列出具体曲名、作者和 `www.soundimage.org`，并提供对应作品页与许可页。

## 4. 运行时规则

1. 主页、商城、卡组、港口、仓库、竞技未开战及其他非远征页面使用 `Kingdom Quest`。
2. 远征地图、事件、补给、商店、战后流程以及远征备战使用 `The Voyage Begins`。
3. 远征和竞技的实际战斗使用 `Training`。
4. 音乐上下文变化时，新曲从 0 秒播放；旧曲淡出、新曲淡入。
5. 同一上下文内页面变化不重启曲目。

## 5. 资源生命周期

1. 旧主页曲 `The Wizard's Concoction` 已连同原 Unity `.meta` 移出 `Assets/Resources`，保存在本目录 `retired-runtime/`。
2. 旧战斗曲 `Fantasy Forest Battle` 已移出 `Assets/Resources`；正式交付原件仍保存在 `ExpeditionBattleShortLoopsV02/delivery/`，本目录另保留退出运行时副本与旧 `.meta` 便于审计。
3. 旧资源路径已从代码、测试和当前游戏内署名清单移除。
4. 远征曲继续在运行时使用，不属于退出构建资源。

## 6. 版本记录

| 版本 | 日期 | 变更 |
|---|---|---|
| v1.0 | 2026-10-02 | 接入 Kingdom Quest 与 Training，调整战斗路由边界，更新许可并完成旧资源退出构建。 |
