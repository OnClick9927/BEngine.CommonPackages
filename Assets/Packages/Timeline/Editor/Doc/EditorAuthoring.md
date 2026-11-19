# 序列编辑器制作

## Timeline 窗口

双击 `.timeline.yaml` 或选择带 PlayableDirector 的对象打开 Timeline。顶部选择资产、新建与保存；轨道列表可添加 Animation、Activation、Signal、Audio、Video，点击轨道头的 M / L 静音或锁定，右键调整顺序。双击轨道添加片段，拖动片段移动或从两端修剪；右键或使用 S / Ctrl+D 拆分、复制片段。Snap 控制帧吸附，Frame All 适配整个序列，右侧可编辑片段的时间、资源、播放倍率与动画混合时长。重叠的 Animation 片段自动交叉渐变，选中片段可拖动混合手柄调整淡入/淡出；短片段完全包含在长片段内时，会渐入并渐出到长片段。显式混合时长覆盖对应侧的自动时长。相同目标的多个 Animation 轨道按属性权重合成，无活动片段时恢复记录的姿态。

选中场景中绑定同一资产的 PlayableDirector 后，Preview、播放、逐帧与时间输入才会驱动场景；关闭 Preview 或切换资产会停止并还原 Director。Timeline 资产只保存轨道与片段，场景目标绑定保存在 Director 上。当前提供这套 2D 序列编辑流程，不包含 Unity 的 Control Track、Cinemachine Track 和嵌套 Timeline。
