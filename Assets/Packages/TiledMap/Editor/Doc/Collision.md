# Tilemap Collision

Tiled Map 内置 `BEngine.TiledMap.TilemapCollider2D`，运行时程序集为 `BEngine.TiledMap`。启用 `com.bengine.tiledmap` 时自动启用 Physics2D 依赖，无需单独安装碰撞桥接包。在 Tilemap 对象上添加 **Physics 2D/Tilemap Collider 2D**；组件会自动补齐 Tilemap。

Palette 中的 `TileDefinition.HasCollider` 决定实际碰撞。关闭碰撞的瓦片和空格子不会生成形状。规则瓦片和动画帧只改变外观，碰撞仍以原始瓦片 ID 为准。

组件直接生成本地形状，不创建临时场景对象、不写回场景文件。地图增删瓦片，以及 Palette 的碰撞标记、碰撞路径、布局、尺寸、间距和锚点变化后自动重建；也可调用 `Rebuild()`。`GetPaths()` 返回组件拥有的当前路径，不应由调用方修改。

矩形布局中，同一行相邻、无间距、无旋转、使用默认形状的格子会合并，以减少碰撞形状数量；设置 `mergeAdjacentCells = false` 可关闭合并。等距布局生成菱形，HexagonPointTop / HexagonFlatTop 生成六边形，保留真实轮廓，不合并为矩形。自定义 `CollisionPaths` 保留闭合轮廓和孔洞，开放路径转换为窄条碰撞形状。

图块旋转和父级 Transform（包括镜像、非等比缩放）参与几何变换。TilemapCollider2D 可作为 Trigger，也可附加 Rigidbody2D 移动整个地图。Physics2D 保持独立于 Tiled Map；地图到物理形状的适配由 Tiled Map 实现。
