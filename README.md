# SimpleWait

Lightweight wait/retry utility for synchronous and asynchronous operations.

- Small, dependency-free library to repeatedly evaluate conditions until success, timeout or cancellation.
- Supports sync and async conditions, ignored exception types, configurable timeout and polling interval, custom exception wrapping, and a typed generic API.

## Quick install
- Package available via NuGet (package id: SimpleWait) — or add the project to your solution and reference `SimpleWait.Core`.

## Key features
- Evaluate boolean or object-returning conditions until success or timeout.
- Async-first support with `ExecuteAsync`.
- CancellationToken support for async waits.
- Fluent configuration: timeout, polling interval, message, ignored exception types, and custom exception on timeout.
- Strongly-typed API via `RetryPolicy.For<TResult>()`.

## Usage examples

### Async (simple)
            var petResponse = await RetryPolicy.Initialize()
                .Timeout(TimeSpan.FromSeconds(10))
                .IgnoreExceptionTypes(typeof(PetStore.ApiException))
                .ExecuteAsync(async () => {
                    var r = await _client.GetPetByIdAsync(pet.Id.Value).ConfigureAwait(false);
                    return r ?? null;
                });

### Synchronous boolean (Success)
            bool ok = RetryPolicy.Initialize()
                .Timeout(TimeSpan.FromSeconds(5))
                .PollingInterval(TimeSpan.FromMilliseconds(200))
                .Success(() => SomeCheck());

### Typed API (generic)
            var dto = RetryPolicy.For<MyDto>()
                .Timeout(TimeSpan.FromSeconds(8))
                .PollingInterval(TimeSpan.FromMilliseconds(100))
                .Execute(() => client.GetDto(id));

### Custom exception on timeout
            var result = RetryPolicy.Initialize()
                .Timeout(TimeSpan.FromSeconds(2))
                .Throw<InvalidOperationException>()
                .Execute(() => SomeNullableReturn());

### Cancellation
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await RetryPolicy.Initialize()
                .PollingInterval(TimeSpan.FromMilliseconds(100))
                .ExecuteAsync(async () => await SomeAsyncOp(), cts.Token);


## Notes
- The library preserves the original TimeoutException as InnerException when wrapping into a configured custom exception (preferred constructor `(string, Exception)` is used when available).
- Default timeout: 500ms polling, default overall timeout set in policy instances (can be overridden).
- Unit tests cover sync/async behavior, cancellation and exception-constructor fallbacks.

## Contributing
- Open a PR on GitHub. Run unit tests with `dotnet test`.
- Please keep changes small and include tests for behavioral changes.

## License
See `LICENSE` in repository root.
