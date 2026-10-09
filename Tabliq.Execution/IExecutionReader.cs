
namespace Tabliq.Execution;

public interface IExecutionReader : IAsyncDisposable, IAsyncEnumerable<ReadOnlySpan<object?>>
{
    public ReadOnlySpan<string> GetFields();

    public Task<bool> ReadAsync(CancellationToken cancellationToken);

    public ReadOnlySpan<object?> GetValues();

    RowAccessor GetRowAccessor();

}

public abstract class BaseExecutionReader : IExecutionReader
{
    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public abstract ReadOnlySpan<string> GetFields();

    public virtual RowAccessor GetRowAccessor() => new RowAccessor(GetFields(), GetValues());

    public abstract ReadOnlySpan<object?> GetValues();

    public abstract Task<bool> ReadAsync(CancellationToken cancellationToken);

    public IAsyncEnumerator<ReadOnlySpan<object?>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new IExecutionReaderAsyncEnumerator(this, cancellationToken);

    private class IExecutionReaderAsyncEnumerator : IAsyncEnumerator<ReadOnlySpan<object?>>
    {
        private readonly IExecutionReader _reader;
        private readonly CancellationToken _cancellationToken;
        public IExecutionReaderAsyncEnumerator(IExecutionReader reader, CancellationToken cancellationToken)
        {
            _reader = reader;
            _cancellationToken = cancellationToken;
        }
        public ReadOnlySpan<object?> Current => _reader.GetValues();
        public async ValueTask DisposeAsync()
        {
            await _reader.DisposeAsync();
        }
        public async ValueTask<bool> MoveNextAsync()
        {
            return await _reader.ReadAsync(_cancellationToken);
        }
    }
}