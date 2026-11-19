# Visual Scripting

Create a `.visualscript.yaml` asset, open it from **Window > Scripting > Visual Script**, and assign it to a `VisualScriptRunner`.

Graphs provide Start, Update and named custom events; typed blackboard variables; numeric comparisons and branches; variable, Transform and log actions; and explicit Stop flow. The editor depends on Node Editor (`com.bengine.node-editor`) for the shared `NodeCanvas`, with selection, connections, moving, panning and zooming. This editor-only dependency does not enter Player runtime assemblies.

The runtime depends on Pooling. Each runner reuses its internal queue nodes, retaining at most 1024 idle nodes and clearing graph references on return. Depth ordering and FIFO order at equal depth are preserved; queued Wait/breakpoint work stays live until consumed, stopped or faulted. Public trace and variable snapshots remain independent objects. Destroying the runner clears and disposes its queue pool.

Breakpoints pause before a node. The Runtime Debugger shows the current node and a copied variable snapshot, and provides Continue, Step and Stop controls. Each event has a configurable execution step limit so accidental cycles fault instead of hanging the game loop.

The version 1 YAML format stores fixed-point numbers and vectors as raw integers. Runtime loading, scene references and AssetBundle loading use the same codec.
