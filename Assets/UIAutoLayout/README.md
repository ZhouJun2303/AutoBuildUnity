# Unity Auto Layout 学习场景

Unity 2022.3.62f2 / UGUI。6 个组件独立场景 + 1 个组合场景。每个场景都有中文说明，全部布局由 Unity 原生组件完成。

## 从这里开始

1. 在 Unity Project 中进入 `Assets/Temp/AutoLayoutLab`，双击 `.unity` 文件。
2. 建议 Game 视图使用 **1600 × 1040**，或接近该比例的横屏窗口。其他比例会整体缩放并留白。
3. 独立场景无需 Play。展开 `AutoLayoutCanvas/Board`，依次查看 `Card01` ～ `Card04`；选中示例节点，在 Inspector 修改参数。
4. 组合场景进入 Play 后，把鼠标放到商品区域滚动。侧栏、价格和商品都是布局示意，不连接游戏业务。
5. 场景中的说明数字和预览图都是**初始值**，修改参数后以 Inspector 中的实际 RectTransform 为准。
6. 想恢复实验前状态，可在 Play 模式修改并退出，或在编辑模式使用 Undo。每个场景的固定 UI 节点都来自同名 `.prefab`；场景实例的实验改动不必 Apply 到预设。

| 场景 | 主要观察点 | 预览 |
| --- | --- | --- |
| [01_LayoutElement.unity](01_LayoutElement.unity) | Min、Preferred、Flexible、Ignore Layout、Layout Priority | [图片](Previews/01_LayoutElement.png) |
| [02_ContentSizeFitter.unity](02_ContentSizeFitter.unity) | 文字决定自身尺寸、换行决定高度、容器包住子项 | [图片](Previews/02_ContentSizeFitter.png) |
| [03_AspectRatioFitter.unity](03_AspectRatioFitter.unity) | 完整显示、填满裁切、宽定高、高定宽 | [图片](Previews/03_AspectRatioFitter.png) |
| [04_HorizontalLayoutGroup.unity](04_HorizontalLayoutGroup.unity) | 横排、控制子项宽度、强制扩展、间距和对齐 | [图片](Previews/04_HorizontalLayoutGroup.png) |
| [05_VerticalLayoutGroup.unity](05_VerticalLayoutGroup.unity) | 竖排、偏好高度、弹性高度、不控制高度 | [图片](Previews/05_VerticalLayoutGroup.png) |
| [06_GridLayoutGroup.unity](06_GridLayoutGroup.unity) | 固定列、固定行、自动换行、网格自适应高度 | [图片](Previews/06_GridLayoutGroup.png) |
| [07_CombinedShop.unity](07_CombinedShop.unity) | 六种组件组成可滚动商店面板 | [图片](Previews/07_CombinedShop.png) |

推荐先学 `01 → 04 → 05 → 02 → 03 → 06 → 07`：先理解尺寸信息，再理解父级如何使用它，最后看自适应和组合。

## 六种组件分别负责什么

| 组件 | 放在哪里 | 负责什么 |
| --- | --- | --- |
| Layout Element | 需要声明尺寸的对象 | 提供最小、偏好、弹性尺寸；本身不会直接修改 RectTransform |
| Content Size Fitter | 要根据内容调整自身大小的对象 | 读取同节点 Text、Layout Group 等提供的布局信息，设置自己的宽高 |
| Aspect Ratio Fitter | 需要保持比例的对象 | 按 `宽 / 高` 比例控制自己的 RectTransform |
| Horizontal Layout Group | 子项的父节点 | 水平排列直接子项，并按配置控制子项尺寸 |
| Vertical Layout Group | 子项的父节点 | 垂直排列直接子项，并按配置控制子项尺寸 |
| Grid Layout Group | 格子的父节点 | 按统一 Cell Size 排列直接子项；不使用子项的 Min / Preferred / Flexible 定格子大小 |

普通 Text、Image、Layout Group 也可以实现布局信息接口。Layout Element 是显式覆盖这些信息的一种方式，并非每个子项都必须添加。

## 01 / Layout Element

以下路径均相对 `AutoLayoutCanvas/Board`。

- **Card01**：三个 Row 都有 A/B/C，子项 `Min Width = 80`、`Preferred Width = 160`、`Flexible Width = 0`。Padding 左右各 10，Spacing 为 20。
  - `Row_W660`：每项宽 160，多余空间留空。
  - `Row_W420`：每项宽 120，处于 Min 和 Preferred 之间。
  - `Row_W300`：每项宽 80。把 Row 的 Width 改为 240，子项仍宽 80，会超出父级；Min 不是“总能放得下”的保证。
- **Card02/Row_Flexible_0_1_2**：三个子项 Preferred 都是 100，Flexible 分别是 0、1、2。宽 660 减去 Padding 20、Spacing 40、Preferred 总和 300，剩余 300，因此实际宽为 **100、200、300**。把 C 的 Flexible 改为 0，变成 **100、400、100**。
- **Card02/NoLayoutParent_Preferred280_Rect100**：Preferred 是 280，但实际宽 100。父级没有消费这份布局信息，所以 Layout Element 不会自己改宽度。
- **Card03/Row_WithIgnoredBadge/Badge_IgnoreLayout**：取消 Ignore Layout，角标会从独立定位切换到 A 与 B 之间，占用布局空间。这个选项不会隐藏对象。
- **Card04**：选择两行中的 `Text_WithLayoutElement`，观察 Layout Priority 为 1 和 -1 的差异。Text 自带的优先级是 0。优先级按属性分别选择；同优先级取较大值；未勾选的属性为 -1，表示该组件不提供这项信息。

Height 的规则与 Width 对应；弹性高度的实际演示在场景 05。

## 02 / Content Size Fitter

- **Card01**：选择两个 Text，在 Text 组件中修改字符串。Horizontal Fit 和 Vertical Fit 都是 Preferred Size，自身 RectTransform 会随内容改变。
- **Card02**：两份文字内容完全相同，宽度分别为 220、420。Horizontal Fit 是 Unconstrained，Vertical Fit 是 Preferred Size。修改 Width 会改变换行，进而改变高度。深色背景是固定的宽度参照区，不是被 Fitter 驱动的对象。
- **Card03/TagRow_FitBoth**：同一节点挂 Horizontal Layout Group + Content Size Fitter，标签子项提供布局尺寸。初始父级 **454 × 80**：宽度 `90 + 130 + 190 + 2 × 12 + 2 × 10`。把 Horizontal Fit 改为 **Min Size**，父级宽变成 **164**：三个子项最小宽度各 40，加上间距和边距。文字可能因此换行或溢出，这说明最小尺寸与舒适展示尺寸不同。
- **Card04/List_FixedWidth_AutoHeight**：父级宽 660，高度随子项变化。初始高 **220**：`36 + 60 + 84 + 2 × 10 + 2 × 10`。修改子项 Preferred Height，或取消子项 GameObject 顶部的激活勾选，观察列表高度。

`Unconstrained` 表示 Fitter 不负责该轴；不表示这个轴永远不会被其他组件修改。只有 Fitter 而没有布局信息来源时，它不会自动遍历任意子物体计算包围盒。要根据子项确定尺寸，通常由同节点 Layout Group 汇总信息。

## 03 / Aspect Ratio Fitter

- **Card01/Frame_320x180/Image_FitInParent_Ratio1**：正方形完整放进 320 × 180 外框，结果为 **180 × 180**，左右各留 70。改变 Frame 宽高，观察完整显示。
- **Card02/Frame_WithRectMask2D/Image_EnvelopeParent_Ratio1**：正方形填满同样的外框，结果为 **320 × 320**。图案居中，父级 Rect Mask 2D 裁掉上下各 70。禁用 Rect Mask 2D，可以看到实际超出外框的图案。**保持比例与裁切是两个组件各自完成的。**
- **Card03**：Width Controls Height，比例 16/9。宽 240 对应高 135，宽 360 对应高 202.5。只改 Width 或 Aspect Ratio，观察 Height。
- **Card04**：Height Controls Width，高 160。比例为 1 时宽 160，比例为 2 时宽 320。只改 Height 或 Aspect Ratio，观察 Width。

Aspect Ratio 是 **宽 ÷ 高**。这里使用三色图案帮助观察裁切；它不会从图片资源自动读取比例。Fit In Parent / Envelope Parent 会驱动锚点、位置和尺寸；本例 Pivot 设为 `(0.5, 0.5)`，所以留白和裁切围绕中心发生。

## 04 / Horizontal Layout Group

- **Card01/Row_ControlWidth_On**：开启 Control Child Size / Width，父级读取子项 Preferred Width，得到 100 / 160 / 220。
- **Card02/Row_ForceExpandWidth_On**：子项 Preferred 都是 100、Flexible 都是 0，但开启 Child Force Expand / Width 后，每项实际宽 **200**。关闭该选项，回到 **100**。
- **Card03/Row_ControlWidth_Off**：关闭 Control Child Size / Width 后，父级采用 RectTransform 的 80 / 140 / 200，忽略 Layout Element 提供的 Preferred Width 240。重新开启时，父级总空间仍会约束最终结果，不保证每项一定得到 240。
- **Card04/Row_Alignment_MiddleCenter**：依次调整 Padding、Spacing、Child Alignment，观察边距、块间距离和整体位置。可切换 Reverse Arrangement，或拖动 Hierarchy 中的子项顺序。

Force Expand 会把该轴的有效 Flexible 至少提升到 1；若原本是 2，仍然是 2。关闭 Control Child Size 后，Force Expand 可能扩大分配的单元空间，而子项自身尺寸仍保持不变。

## 05 / Vertical Layout Group

- **Card01/List_PreferredHeights**：子项高 40 / 60 / 80；父级控制宽度并强制横向扩展，实际宽均为 640。
- **Card02/List_Flexible_0_1_2**：父级高 250，每项 Preferred Height = 40；Flexible Height = 0 / 1 / 2。扣除 Padding 20、Spacing 20 和偏好高度总和 120，剩余 90，初始高度 **40 / 70 / 100**。
- **Card03/List_ControlHeight_Off**：关闭高度控制，实际高度取 RectTransform 的 30 / 50 / 70，而不是 Preferred Height 100。
- **Card04/List_LowerCenter**：宽度不强制扩展，子项保持宽 240；Lower Center 让整组靠下、每项横向居中。修改 Child Alignment 比较效果。

## 06 / Grid Layout Group

- **Card01/Grid_Fixed3Columns**：固定 3 列，格子 180 × 80。子项 Layout Element 故意设置 Preferred = 500 × 300，但实际仍然是 180 × 80。
- **Card02/Grid_Fixed2Rows_FillVertical**：固定 2 行，Start Axis = Vertical。编号顺序为先向下再向右。切换 Start Axis 和 Start Corner 观察变化。
- **Card03/Grid_Flexible_ResizeWidth**：宽 660 时排 4 列；改成 **500** 后排 3 列。父级高度没有 Fitter，所以行数增加后可能超出深色区域，这正好展示“布局子项”和“改变自身高度”的区别。
- **Card04/Grid_Fixed3Columns_AutoHeight**：固定 3 列，格子 180 × 56，同时设置 Vertical Fit = Preferred Size。初始 2 行，高 **148**；禁用第 4～6 个子项，只剩 1 行，高 **80**。也可以用 Ctrl+D 复制格子，观察增加行数。

Grid 的 Constraint = Flexible 指的是行列数随空间变化，与 Layout Element 的 Flexible Width/Height 权重不是一回事。常见组合是“固定列数 + 固定宽度 + 自动高度”；反向也可以采用“固定行数 + 自动宽度”。

## 07 / 组合商店

组合场景使用 6 种组件，另有 Scroll Rect、Rect Mask 2D 和 EventSystem 支持滚动与裁切。固定 UI 保存于 `07_CombinedShop.prefab`。

```text
AutoLayoutCanvas / Board
└─ Shop_VerticalLayout                     Vertical Layout Group：上中下排列
   ├─ Header_HorizontalLayout             Horizontal Layout Group + Layout Element
   │  ├─ Title_FlexibleWidth              标题占用剩余宽度
   │  └─ Currency_FixedWidth              货币栏固定偏好宽度
   ├─ Body_HorizontalLayout               Horizontal Layout Group + 弹性高度
   │  ├─ Sidebar_VerticalLayout_FixedWidth 侧栏 Preferred Width = 248
   │  └─ Products_VerticalLayout_FlexibleWidth 右侧 Flexible Width = 1
   │     ├─ ProductsTitle
   │     └─ Viewport_ScrollRect_RectMask2D 固定可视区，支持滚动与裁切
   │        └─ Content_Grid_Fitter         Grid 固定 3 列 + Fitter 自动高度
   │           └─ Product_01..09_VerticalLayout  商品内部竖向排列
   │              ├─ ImageSlot_LayoutElement_Height108 固定偏好高度的图片槽
   │              │  └─ Artwork_AspectRatio_FitInParent 图片自己保持 16:9
   │              ├─ ProductName_LayoutElement
   │              ├─ Price_HorizontalLayout
   │              └─ Badge_IgnoreLayout   部分商品的角标不占布局空间
   └─ Footer_LayoutElement_Height48
```

建议按顺序做这几个实验：

1. 修改 Sidebar 的 Preferred Width（例如 248 → 300），观察右侧区域得到的宽度减少。Grid 的格子仍固定为 352 × 202，不会自动均分；侧栏太宽会导致格子超出视口并被裁切。
2. 在 Content 上修改 Cell Size 或 Spacing，观察商品尺寸、间距和 Content 高度。初始 9 件、3 行，Content 高 **662**：`3 × 202 + 2 × 16 + 2 × 12`。
3. 复制 Product 添加商品，观察 Fitter 自动增加 Content 高度；Play 后滚到最下面，确认新增行能显示。
4. 修改图片子节点 Aspect Ratio（如 16/9 → 1），观察图片变成正方形、商品格子仍由 Grid 控制。
5. 取消某个 Badge 的 Ignore Layout，观察它进入商品的 Vertical Layout Group，开始占据空间。
6. 选中 Viewport 的 Scroll Rect，确认 Content 和 Viewport 引用；遮罩裁切内容，滚动组件移动 Content，Grid 排列商品，职责各自独立。

## 组合时避免尺寸控制冲突

- 一个轴尽量只有一个明确的尺寸控制者。父级 Layout Group 控制子项高度时，不要又让子项的 Fitter 控制同一高度。
- **Layout Group 与 Fitter 同节点**可以合理配合：布局组提供内容所需的尺寸，Fitter 据此改变自身。场景 02 和 06 展示了这种搭配。
- **图片包一层槽位**：外层槽位由 Layout Group / Layout Element 控制，内层图片由 Aspect Ratio Fitter 控制。不要让父级 Grid 和直接子项 Aspect Ratio Fitter 同时决定同一块矩形的宽高。
- RectTransform 某字段在 Inspector 中显示被驱动时，修改其控制源。若手改后立刻恢复，通常是布局组件下一次重算重新写入。
- 学习场景用了固定大小的演示板以便比较数字。Canvas Scaler 的整体缩放不是 Layout Element 的 Flexible 分配；示例数字使用 Canvas 逻辑单位。

## 验证记录

2026-10-20，在当前 Unity 2022.3.62f2 编辑器中完成：

- 七个 `.unity` 原生加载，预设实例及原生组件引用检查；中文字符覆盖检查通过。
- 各场景关键 RectTransform 尺寸与说明数值核对通过。
- 实际切换参数验证：Min 下限、Flexible 重新分配、Ignore Layout、Fitter 的 Min Size、Force Expand、Grid 换列和自适应高度通过。
- 组合场景直接调用原生 ScrollRect 的滚轮处理，内容移动 150；滚到末端后位置与内容高度差一致，最后一行可达。此项在编辑模式调用组件 API 完成，未切入 Play 人工操作鼠标。
- 七个场景均由 Unity Camera 渲染为 `Previews` 中的 PNG，并完成画面检查；图片轴心和换行示例已修正。

所有场景使用原生 UGUI，不依赖登录、热更或游戏数据初始化。教学说明直接序列化在资源内，不新增业务语言 key 或运行时 UI 脚本。`.meta` 均由 Unity 自动生成。该目录受仓库既有 `.gitignore` 忽略，当前产物只在本地工作区保存，未提交 Git。

## 固定自检

引用规则文件：

- `.cursor/rules/core/architecture.mdc`
- `.cursor/rules/core/conventions.mdc`
- `.cursor/rules/core/api.mdc`
- `.cursor/rules/workflows/development.mdc`
- `.cursor/rules/workflows/requirements.mdc`
- `.cursor/rules/modules/ui-patterns.mdc`
- `.cursor/rules/tools/extension-methods.mdc`
- `.cursor/rules/quick-ref/directory-structure.mdc`
- `.cursor/rules/quick-ref/checklist.mdc`

写入影响范围：

- `Assets/Temp/AutoLayoutLab` 下的学习场景、同名 UI 预设、中文指南和预览图片；资源导入产生的 `.meta` 由 Unity 管理。
- UI / 资源。未改动 AOT、Hotfix、Query/Ctrl/Model、业务事件、GM、任务、语言表或模块注册。
- 按用户指定将实验预设放在 `Assets/Temp`，不进入正式游戏资源目录。

最小校验结论：

- **通过**：固定视觉节点已保存到 Prefab，运行时不创建层级。
- **通过**：未新增或修改 UIElement 业务类，OnAwake / OnDestroy 与标记要求无新增适用项。
- **通过**：教学文案直接序列化，无 `SetText` 硬编码业务文案。
- **通过**：无查询或业务数据写入，无需增加 Ctrl、Query 或业务事件。
- **通过**：Unity 原生加载、组件引用、中文字符、关键尺寸、参数变化和预览图检查。
- **通过**：未手写 `.meta`；未暂存或提交 Git。
- **验证边界**：滚动使用编辑模式原生 API 验证；未进行 Play 模式人工鼠标操作验证。
