# Reactive scheduling contracts

## Use main-thread async and reactive flows

- Use `BTask` or allocation-reduced `BValueTask` for Runtime async methods. They are frame-driven engine awaitables, not wrappers around .NET `Task` or worker-thread APIs.
- Use `BEngine.Reactive.BObservable<T>`, `Subject<T>`, `BehaviorSubject<T>`, and `ReactiveProperty<T>` for event streams and state. Compose them with `Select`, `Where`, `Take`, `Skip`, `DistinctUntilChanged`, `Merge`, `CombineLatest`, `Delay`, or `Debounce`.
- Use `Observable.EveryUpdate`, `IntervalFrame`, `Timer`, and `Interval` only through the engine scheduler. Editor update and Player runtime update own scheduler ticks; user code must not create a second ticking loop.
- Treat `OnErrorResume` as a recoverable notification and `OnCompleted(BResult)` as the terminal signal. Dispose subscriptions with `BCompositeDisposable`, `BDisposableBag`, `BSerialDisposable`, or `AddTo` when their owner leaves the scene or window lifecycle.
- Convert only through `Observable.FromBTask`, `FromBValueTask`, `FirstAsync`, and `ForEachAsync`; cancellation uses `BCancellationToken` and must release the upstream subscription immediately.
