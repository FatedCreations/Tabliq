using System;
using System.Collections.Generic;
using System.Text;

namespace Tabliq.Execution.Policies;

public interface IExecutionPolicy
{
    public bool Validate(ExecutionPlan plan, out IEnumerable<PolicyValidationError> errors);
}

public class PolicyValidationError
{
    public required string Message { get; init; }
}
