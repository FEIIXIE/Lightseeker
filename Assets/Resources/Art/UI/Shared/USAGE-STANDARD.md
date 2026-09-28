# 《寻灯者》共享 UI 素材库规范 v1.0

## 核心原则

1. **一种语义只对应一个资产 ID。** 页面不得因为尺寸或开发便利重新绘制同义图标。
2. **共享素材只引用，不复制。** Unity 运行时应从统一 Resources/Addressables 路径加载；页面目录不得保存同图异名副本。
3. **视觉更新通过版本升级完成。** 新设计先更新共享素材库和注册表，再由所有使用页面共同切换。
4. **布局与素材分离。** 图标尺寸由页面 RectTransform 控制，禁止 `Set Native Size`、Content Size Fitter 或通过透明留白修正位置。
5. **默认中心 Pivot。** 除非注册表明确声明，所有图标统一 `(0.5, 0.5)`。
6. **严禁改变原始宽高比。** 所有适配只能等比缩放并光学居中；不得分别指定目标宽高拉伸素材。
7. **运行时统一入口。** Unity 中只从 `Resources/Art/UI/Shared/` 按注册表 semanticKey 解析；页面专属目录不得再保存共享元素副本。
8. **禁止截图裁切复用。** 页面原画、预览图和运行时截图只用于验收，不得从中裁切共享图标作为新资产。

## 标准流程

1. 先在 `ui-asset-registry.json` 查询 semanticKey。
2. 已存在：直接复用，不能重新生成相似素材。
3. 不存在：先设计候选并确认，再登记为 CANONICAL。
4. 修改 CANONICAL：提升素材库版本，记录受影响页面，统一回归验证。

## 图文控件内容组对齐

- 同一父框中的图标／标签与文字视为一个内容组。先按素材原始宽高比确定图标实宽，测量当前语言文字，再以固定 `gap` 拼成内容组，最后将整个组在 `parentRect` 内水平、垂直光学居中。
- 中文、英文或数字位数变化时允许组宽动态变化，但 `group-center` 必须保持在父框中心；禁止分别硬编码图标与文字绝对坐标。
- 文本过长时缩小到 `textMinSize` 为止；图标不得拉伸，父框不得为单一文案漂移。
- 每份相关布局 JSON 必须提供：`parentRect`、`iconSize` 或 `iconMaxSize`、`gap`、`alignment: "group-center"`、`textMinSize`。统一字段契约见 `content-group-layout-contract.schema.json`。
- 本规则适用于顶部回合／金币／生命、放弃、刷新、价格、品质标签和所有后续同类控件。

## 圆形控件与图标光学中心

- 圆度按可见外环或有效 Alpha bounds 验收，不按正方形画布猜测；有效外环宽高比误差须 ≤1%。
- 图标先裁取有效 Alpha bounds 等比缩放，再以 Alpha bounds 中心定位，并校验不透明度加权光学重心；光学重心与父框中心误差须 `<1` 个 841×1870 逻辑像素。
- 透明画布留白不参与居中。交付 JSON 分开记录 `sourceSlice`、`visualRect`、`interactionRect`、`effectiveAlphaBounds`、`opticalCenterDelta`。
- 点击区保持产品规定的父框尺寸；禁止为了追随图标有效像素而缩小可点击区域。

## 当前旗帜标准

- semanticKey：`action.abandon.flag.white`
- 中文语义：放弃、撤离、投降、退出当前远征或战斗流程。
- 唯一运行时文件：`runtime/actions/ART-UI-SHARED-ACTION-ABANDON-FLAG-01.png`
- 默认显示尺寸：32×32。
- 禁止使用火焰旗、纯色三角旗或页面独立白旗替代。

## 当前罗盘标准

- semanticKey：`decor.compass.rose`
- 所有顶部装饰、面板水印和航海分隔符均从同一母版派生。
- 只允许等比 contain、透明度和颜色样式调整；禁止分别拉宽或拉高。

## 命名

`ART-UI-SHARED-{CATEGORY}-{NAME}-{NN}.png`

- `SHARED` 表示跨页面复用。
- `CATEGORY` 使用 ACTION、STATUS、CURRENCY、MAP-NODE、MAP-LEGEND 等稳定分类。
- 文件升级时递增末尾编号，不覆盖历史定稿。

## Unity 建议路径

`Assets/Resources/Art/UI/Shared/`

注册表中的 `unityResourcesRoot` 对应运行时根路径 `Art/UI/Shared`。建议代码仅暴露 semanticKey 或统一 AssetId，不让页面脚本硬编码散落的文件名。
