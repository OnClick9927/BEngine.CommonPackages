# CommonPackages

这是使用 BEngine 原生工程布局维护通用包的制作工程，与 `src` 同级。
使用 Hub 打开此目录的 `Project.yaml`，选择引擎版本 `0.1.0`。

包源码位于 `Assets/Packages/<包名>`：Jobs、Reactive、StandardAsset、ArtImport2D、Codex、Cinemachine、Localization、PixelPerfect2D、PostProcessing、PropertyAttributes、ShaderGraph、SpriteShape2D、SpriteSkinning2D、Testing、TiledMap、Timeline、VisualScripting，共 17 个。

在编辑器中使用 Package Manager 的 `Create package...` 创建新包，使用 `Export package...` 选择现有包并校验、导出。
导出目录由引擎管理，为 `Packages-Exported/<包名>/<包名>-<packageVersion>/`。
编辑包的版本和依赖时修改其 `package.yaml`，程序集引用同时维护 `.asmdef.yaml`。
StandardAsset 保留 `type: project`，导入其他工程后仍是可编辑的工程内容。

`Packages/manifest.yaml` 只启用制作这些包所需的引擎依赖；本工程中的 17 个包通过 Assets 源码参与编译，不能再启用它们的仓库副本。
运行时和编辑器程序集名称、包 ID、资源及示例保持原有身份。

`Development/Projects` 保留用于仓库构建检查的 MSBuild 项目，它们引用 Assets 中的源码，并已关闭自动导出到引擎包仓库。
编辑器依据 `.asmdef.yaml` 和 `package.yaml` 编译，包源码目录不含手写 `.csproj` 或构建缓存。
IDE 工程使用编辑器的 IDE Integration 重新生成，产物位于 `Library/IDE`，无需提交。
`Library`、`Temp`、`Logs` 和 `Cache` 是本地生成目录。

这些包的旧引擎发行副本已从 `Engine/Packages` 移出，后续引擎导出会清理同名的旧发行目录。
Hub 显示的是其安装目录中的包；本工程的源码和 `Packages-Exported` 不会自动进入 Hub 列表。
在其他工程使用时，通过 Hub 单独安装所需的发布版本。
