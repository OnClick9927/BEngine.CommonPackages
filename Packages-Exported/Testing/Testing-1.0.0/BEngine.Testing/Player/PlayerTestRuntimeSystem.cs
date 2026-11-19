using System.Text.Json;
using BEngine.Networking;

namespace BEngine.Testing;

public sealed class PlayerTestRuntimeSystem : ISceneRuntimeSystem
{
    private const string ModeVariable = "BENGINE_TEST_RUNNER_MODE";
    private const string ResultVariable = "BENGINE_TEST_RUNNER_RESULT";
    private Task<TestRunResult>? _run;
    private BTask? _delivery;
    private TestRunResult? _result;
    private HttpNetworkRequest? _request;
    private bool _finished;
    private bool _observingAssemblies;
    private int _discoveryDirty;
    private int _typeCacheGeneration = -1;
    private HashSet<string>? _selectedIds;

    public string packageId => "com.bengine.testing";
    public int order => 9000;

    public void Update(Scene scene, Fix64 deltaTime)
    {
        _ = deltaTime;
        if (_finished || (_run is null && !_observingAssemblies && !string.Equals(Environment.GetEnvironmentVariable(ModeVariable),
                "player", StringComparison.OrdinalIgnoreCase))) return;
        if (_run is null)
        {
            if (!_observingAssemblies)
            {
                if (Environment.GetEnvironmentVariable("BENGINE_TEST_RUNNER_IDS") is { Length: > 0 } filter)
                    _selectedIds = (JsonSerializer.Deserialize<string[]>(filter) ?? []).ToHashSet(StringComparer.Ordinal);
                AppDomain.CurrentDomain.AssemblyLoad += AssemblyLoaded;
                _observingAssemblies = true;
                _discoveryDirty = 1;
            }
            // A precompiled release can become active without a CLR AssemblyLoad event.
            // The engine reflection index also changes when such a domain is registered.
            var generation = RuntimeTypeCache.stats.Generation;
            if (Interlocked.Exchange(ref _discoveryDirty, 0) == 0 && generation == _typeCacheGeneration) return;
            _typeCacheGeneration = generation;
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var tests = TestDiscovery.Discover(assemblies)
                .Where(test => test.Mode == TestMode.Player).ToArray();
            if (_selectedIds is not null)
            {
                tests = tests.Where(test => _selectedIds.Contains(test.Id)).ToArray();
                // Selected tests can live in assemblies loaded by the game's startup workflow.
                // Wait for those assemblies instead of reporting an empty successful run in its AOT scene.
                if (_selectedIds.Count > 0 && !_selectedIds.IsSubsetOf(tests.Select(test => test.Id)))
                {
                    var missing = _selectedIds.Except(tests.Select(test => test.Id), StringComparer.Ordinal).ToArray();
                    var assemblyNames = missing.Select(id => id.Split(':')[0]).ToHashSet(StringComparer.Ordinal);
                    Debug.Log("BENGINE_PLAYER_TESTS_WAITING|missing=" + string.Join(',', missing) + "|loaded=" +
                        string.Join(',', assemblies.Select(assembly => assembly.GetName().Name).Where(name => name is not null && assemblyNames.Contains(name))));
                    return;
                }
            }
            StopObservingAssemblies();
            var collectCoverage = string.Equals(Environment.GetEnvironmentVariable("BENGINE_TEST_RUNNER_COVERAGE"),
                "1", StringComparison.Ordinal);
            _run = TestExecutor.RunAsync(tests, new TestRunOptions
            {
                CollectCoverage = collectCoverage,
                PlayerScene = scene
            });
            return;
        }
        if (!_run.IsCompleted) return;
        try
        {
            if (_delivery is null)
            {
                _result = _run.GetAwaiter().GetResult();
                _delivery = DeliverResult(_result);
            }
            if (!_delivery.IsCompleted) return;
            _finished = true;
            _delivery.GetAwaiter().GetResult();
            var result = _result!;
            Debug.Log($"BENGINE_PLAYER_TESTS_COMPLETED|passed={result.Passed}|failed={result.Failed}|skipped={result.Skipped}|coverage={result.Coverage?.CoveredLines ?? 0}/{result.Coverage?.TotalLines ?? 0}");
            if (!result.Succeeded) Debug.LogError("BENGINE_PLAYER_TESTS_FAILED|One or more Player tests failed.");
        }
        catch (Exception exception)
        {
            _finished = true;
            Debug.LogError("BENGINE_PLAYER_TESTS_FAILED|" + exception);
        }
        Application.Quit();
    }

    private async BTask DeliverResult(TestRunResult result)
    {
        var json = PlayerTestReportJson.Serialize(result);
        var path = Environment.GetEnvironmentVariable(ResultVariable);
        if (!string.IsNullOrWhiteSpace(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllBytes(path, json);
        }
        var endpoint = Environment.GetEnvironmentVariable("BENGINE_TEST_RUNNER_RESULT_URL");
        if (string.IsNullOrWhiteSpace(endpoint)) return;
        using var request = HttpNetworkRequest.Post(endpoint, json, "application/json");
        _request = request;
        try
        {
            await request.SendAsync();
            if (!request.IsSuccess) throw new IOException("Player test report delivery failed: " + request.Error, request.Exception);
        }
        finally { _request = null; }
    }

    public void Stop(Scene scene)
    {
        _finished = true;
        StopObservingAssemblies();
        _request?.Dispose();
        _request = null;
    }

    private void AssemblyLoaded(object? sender, AssemblyLoadEventArgs args) =>
        Interlocked.Exchange(ref _discoveryDirty, 1);

    private void StopObservingAssemblies()
    {
        if (!_observingAssemblies) return;
        AppDomain.CurrentDomain.AssemblyLoad -= AssemblyLoaded;
        _observingAssemblies = false;
    }
}
