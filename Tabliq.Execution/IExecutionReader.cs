namespace Tabliq.Execution;

public interface IExecutionReader : IAsyncDisposable
{
    public ReadOnlySpan<string> GetFields();

    public Task<bool> ReadAsync(CancellationToken cancellationToken);

    public ReadOnlySpan<object?> GetValues();
}
