# SimpleWait

Free to use

Waiting for result (async/sync) with differenct conditions

# Example
## Async example:
            var petResponse = await RetryPolicy.Initialize()
                .Timeout(TimeSpan.FromSeconds(10))
                .IgnoreExceptionTypes(typeof(PetStore.ApiException))
                .ExecuteAsync(async () =>
                {
                    var r = await _client.GetPetByIdAsync(pet.Id.Value).ConfigureAwait(false);

                    return r ?? null;
                });

• Uses a 10-second timeout with a default polling interval of 500ms.

• Ignores exceptions of type ApiException.

• Repeatedly fetches the result until a non-null value is obtained, at which point it returns that value.  
