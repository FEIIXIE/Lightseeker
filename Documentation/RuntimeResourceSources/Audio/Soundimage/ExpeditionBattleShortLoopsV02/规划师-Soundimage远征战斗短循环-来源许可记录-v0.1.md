# Soundimage 远征与战斗短循环来源许可记录

- 文档版本：v0.1
- 更新日期：2026-10-02
- 状态：ACTIVE / ARCHIVE_ONLY / NOT_RUNTIME_INTEGRATED
- 来源角色：规划师

## 1. 归档目的

本目录保存已确认方向的远征与战斗背景音乐短循环、对应官方原始音频及许可信息，供主程后续正式接入。当前内容位于 Unity 工程的非运行时文档归档区，不会进入当前游戏构建。

## 2. 音乐来源

### 2.1 远征音乐

- 曲名：The Voyage Begins
- 作者：Eric Matyas
- 发布方：Soundimage
- 来源页面：https://soundimage.org/epic-battle/
- 官方原始文件：https://soundimage.org/wp-content/uploads/2023/12/The-Voyage-Begins.mp3
- 本地原始文件：`source/The-Voyage-Begins.mp3`
- 待接入文件：`delivery/BGM_EXPEDITION_THE_VOYAGE_BEGINS_30S_LOOP_SOUNDIMAGE_V02.ogg`
- 待接入版本时长：26.255 秒

### 2.2 战斗音乐

- 曲名：Fantasy Forest Battle
- 作者：Eric Matyas
- 发布方：Soundimage
- 来源页面：https://soundimage.org/epic-battle/
- 官方原始文件：https://soundimage.org/wp-content/uploads/2016/07/Fantasy-Forest-Battle.mp3
- 本地原始文件：`source/Fantasy-Forest-Battle.mp3`
- 待接入文件：`delivery/BGM_BATTLE_FANTASY_FOREST_BATTLE_30S_LOOP_SOUNDIMAGE_V02.ogg`
- 待接入版本时长：26.324 秒

## 3. 许可与署名

- 许可名称：Soundimage International Public License
- 许可页面：https://soundimage.org/sample-page/
- 署名说明：https://soundimage.org/attribution-info/
- 作者网站：https://soundimage.org/

正式发布时建议在游戏“设置 → 致谢与许可”页面以及随包第三方资源说明中保留以下署名：

```text
The Voyage Begins — by Eric Matyas · © Eric Matyas · www.soundimage.org
Fantasy Forest Battle — by Eric Matyas · © Eric Matyas · www.soundimage.org
```

不得删除作者、版权与来源网站信息。若后续许可页面发生变化，正式发布前须由安全员或发行负责人重新核对并留档。

## 4. 已实施的音频处理

V02 交付文件由官方原始 MP3 制作，处理范围如下：

- 选取适合循环的乐段；
- 首尾实施 0.75 秒等功率循环交叉淡化；
- 转码为 Ogg Vorbis；
- 未变更速度、音高与配器；
- 未做响度归一化或重新编曲。

详细制作与校验记录见：

- `outputs/audio/soundimage-expedition-battle-short-loops-v0.2/规划师-Soundimage远征战斗短循环-交接说明-v0.2.md`
- `outputs/audio/soundimage-expedition-battle-short-loops-v0.2/规划师-Soundimage远征战斗短循环-校验报告-v0.2.json`

## 5. 接入边界与后续要求

本次仅做非运行时归档，不修改游戏代码、配置、布局、玩家可见内容或 Unity 运行时资源，因此不递增工程版本。

主程正式接入时应：

1. 仅从 `delivery` 目录选择交付 OGG，复制到正式运行时音频目录并生成对应 `.meta`；
2. 为接入批次分配新的唯一工程版本，并同步所有版本号与测试记录；
3. 将上述署名同步到游戏内“致谢与许可”页面及发布随包文档；
4. 核查旧候选音乐是否仍被代码、配置或动态路径引用，按资源生命周期规范退出构建；
5. 在目标 Android 设备上分别连续循环不少于五次，检查首尾衔接、爆音、音量与场景切换；
6. 完成版本一致性检查、核心烟测和构建内容审计。

## 6. 变更记录

| 版本 | 日期 | 变更 |
|---|---|---|
| v0.1 | 2026-10-02 | 首次建立远征与战斗短循环的来源、许可、处理方式及接入边界记录；本次为纯归档与文档变更，不影响游戏产物。 |
