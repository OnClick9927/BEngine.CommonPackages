# 地图编辑器制作

## Tile Palette 与 Scene 绘制

新建或打开 Palette，在 Edit Palette 中选择纹理或 Sprite Atlas、切片并保存。退出 Edit Palette 后，拖动框选调色板中的矩形图案作为画笔；Active Tilemap 选择场景地图，点击 Paint in Scene 后直接在 Scene 操作。地图有平移、旋转或缩放时仍按本地网格定位。

S / M / B / U / I / D / G 分别选择框选、移动、画笔、矩形填充、吸取、擦除和洪水填充；`[` / `]` 旋转画笔，Shift 配合方括号翻转。支持多格图案重复填充、重叠区域移动和整笔一次撤销。中键平移和滚轮缩放仍由 Scene 视图处理；切换工具或失去焦点会结束当前笔画。

此流程面向引擎的矩形 2D 网格；不支持 Unity 的 Hexagonal / Isometric Grid、RuleTile 或 Prefab Brush。详细操作见 TiledMap 包 Editor/Readme.md。
