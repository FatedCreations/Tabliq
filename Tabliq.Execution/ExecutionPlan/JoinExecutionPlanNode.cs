using Tabliq.Execution.ExecutionReader;
using Tabliq.Sql.Ast;

namespace Tabliq.Execution;

public sealed class JoinExecutionPlanNode : ExecutionPlanNode
{
    private readonly ExecutionPlanNode _left;
    private readonly ExecutionPlanNode _right;
    private readonly JoinType _joinType;
    private readonly Condition? _onCondition;
    private readonly JoinSide _joinSide;

    public JoinExecutionPlanNode(ExecutionPlanNode left, ExecutionPlanNode right, JoinType joinType, Condition? onCondition, JoinSide joinSide)
    {
        _left = left;
        _right = right;
        _joinType = joinType;
        _onCondition = onCondition;
        _joinSide = joinSide;
    }

    public ExecutionPlanNode Left => _left;

    public ExecutionPlanNode Right => _right;
    public JoinType JoinType => _joinType;
    public Condition? Condition => _onCondition;
    public JoinSide JoinSide => _joinSide;
    public override IExecutionProvider? Provider => null;

    public override ExecutionPlanNode? TryRewrite()
    {
        ExecutionPlanNode currentNode = this;
        var newLeft = _left.TryRewrite() ?? _left;
        var newRight = _right.TryRewrite() ?? _right;
        if (newLeft != _left || newRight != _right)
        {
            currentNode = new JoinExecutionPlanNode(newLeft, newRight, _joinType, _onCondition, _joinSide);
        }

        currentNode = newLeft?.Provider?.TryRewrite(currentNode) ?? currentNode;
        currentNode = newRight?.Provider?.TryRewrite(currentNode) ?? currentNode;

        return currentNode;
    }

    public override async Task<IExecutionReader> ExecuteAsync(CancellationToken cancellationToken)
    {
        var right = await _right.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        var left = await _left.ExecuteAsync(cancellationToken).ConfigureAwait(false);

        var leftFields = left.GetFields();
        var rightFields = right.GetFields();
        var leftFieldCount = leftFields.Length;
        var rightFieldCount = rightFields.Length;

        var fields = new string[leftFieldCount + rightFieldCount];

        leftFields.CopyTo(fields.AsSpan());
        rightFields.CopyTo(fields.AsSpan(leftFieldCount));

        var combined = new object?[leftFieldCount + rightFieldCount];

        var nullsValues = new object?[Math.Max(leftFieldCount, rightFieldCount)];

        object?[] CombineRows(ReadOnlySpan<object?> leftRow, ReadOnlySpan<object?> rightRow)
        {
            leftRow.CopyTo(combined);
            rightRow.CopyTo(combined.AsSpan(leftRow.Length));
            return combined;
        }
        async IAsyncEnumerable<object?[]> Join(string[] combinedColumns)
        {
            List<object?[]> rightRows = new();
            while (await right.ReadAsync(cancellationToken))
            {
                rightRows.Add(right.GetValues().ToArray());
            }

            if (_joinType == JoinType.Cross)
            {
                while (await left.ReadAsync(cancellationToken))
                {
                    foreach (var rightRow in rightRows)
                    {
                        var leftRow = left.GetValues();
                        yield return CombineRows(leftRow, rightRow);
                    }
                }

                yield break;
            }

            var matchedRightIndexes = new HashSet<int>();
            while (await left.ReadAsync(cancellationToken))
            {
                var matched = false;

                for (var i = 0; i < rightRows.Count; i++)
                {
                    var rightRow = rightRows[i];
                    var leftRow = left.GetValues();
                    var combinedRow = CombineRows(leftRow, rightRow);
                    if (_onCondition is not null && !EvaluationHelpers.EvaluateCondition(_onCondition, new RowAccessor(fields, combinedRow)))
                    {
                        continue;
                    }

                    yield return combinedRow;
                    matchedRightIndexes.Add(i);
                    matched = true;
                }

                if (_joinSide == JoinSide.Left && !matched)
                {
                    var leftRow = left.GetValues();
                    yield return CombineRows(leftRow, nullsValues.AsSpan(0, rightFieldCount));
                }
            }

            if (_joinSide == JoinSide.Right)
            {
                for (var i = 0; i < rightRows.Count; i++)
                {
                    if (matchedRightIndexes.Contains(i))
                    {
                        continue;
                    }

                    yield return CombineRows(nullsValues.AsSpan(0, leftFieldCount), rightRows[i]);
                }
            }
        }

        return new AsyncEnumeratorExecutionReader(fields, Join(fields).GetAsyncEnumerator(cancellationToken), [left, right]);
    }

}
