namespace Tabliq.Execution.Policies;

public class SingleProviderPolicy : IExecutionPolicy
{
    private readonly IExecutionProvider? _executionProvider;

    // this is the targert provider everyting must be executed on
    public SingleProviderPolicy(IExecutionProvider executionProvider)
    {
        _executionProvider = executionProvider;
    }

    // first encounted is the target provider
    public SingleProviderPolicy()
    {
        _executionProvider = null;
    }

    public bool Validate(ExecutionPlan plan, out IEnumerable<PolicyValidationError> errors)
    {
        bool firstProviderSet = _executionProvider != null;
        IExecutionProvider? firstProvider = _executionProvider;

        foreach (var node in GetAllNodes(plan.RootNode))
        {
            if (!firstProviderSet)
            {
                firstProvider = node.Provider;
                firstProviderSet = true;
            }
            else if (firstProvider != node.Provider)
            {
                errors = [new PolicyValidationError { Message = "Multiple providers detected in the execution plan." }];
                return false;
            }
        }

        errors = Enumerable.Empty<PolicyValidationError>();
        return true;
    }

    private IEnumerable<ExecutionPlanNode> GetAllNodes(ExecutionPlanNode node)
    {
        var stack = new Stack<ExecutionPlanNode>();
        stack.Push(node);
        yield return node;

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            foreach (var input in current.GetInputs())
            {
                stack.Push(input);
                yield return input;
            }
        }
    }
}
