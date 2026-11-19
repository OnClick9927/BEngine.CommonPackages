# Test Framework

Open **Window > General > Test Runner** to discover and run tests from the current project.

- `[Test(TestMode.Edit)]` runs without a scene.
- `[Test(TestMode.Play)]` creates a real `Scene` and `SceneRuntime`; use `TestContext.Current.Scene` and `Tick()` for lifecycle checks.
- `[Test(TestMode.Player)]` requests a Player from an optional build workflow, then runs it locally or through a device/browser launcher. Frameworks connect through Core Editor's `IPlayerValidationBuildProvider`; test-specific integrations can use `IPlayerTestBuildProvider`. Results return to the editor as JSON.
- Double-click a result, or use **Open**, to jump to the first portable-PDB sequence point.
- Add `[assembly: BEngine.Testing.Coverage]`, or the same fully qualified attribute to a type or method, to enable statement and branch probes. Export produces LCOV (`DA` and `BRDA`) and JSON in `Temp/TestRunner/Coverage`.

The package has its own assertions and does not require NUnit or xUnit in game assemblies.

Edit/Play tests, source navigation and coverage work independently of a game framework. Frameworks can implement Core Editor's `IPlayerValidationBuildProvider` and register it through DI or Core's service-contribution API; Testing adapts its `PlayerValidationBuild` metadata and startup arguments. Test-specific integrations can continue to implement `IPlayerTestBuildProvider`, pass it to `PlayerTestSession.RunProjectAsync`, or register it through DI or as a default. Explicit and registered test providers take precedence over the neutral workflow. The runner does not read framework build profiles or publish settings. Already-built device/browser artifacts can be passed directly to `RunBuiltPlayerAsync` as either `PlayerValidationBuild` or `PlayerTestBuild`.

The runner has built-in launchers for Android and Web target IDs and supports custom launchers through `PlayerTestLaunchers.Register`. Local executable launches require a target matching the editor's operating system. Device/browser execution also requires the target's external tools and a usable device or browser.

Web Player tests create an isolated browser profile in the owning project's
`Temp/TestRunner/BrowserProfiles` and remove that profile after the run. Direct launcher callers can
set `PlayerTestLaunchRequest.ProjectPath`; `RunBuiltPlayerAsync` accepts an optional `projectPath`.
When no project is open, provide an existing absolute project path explicitly.
