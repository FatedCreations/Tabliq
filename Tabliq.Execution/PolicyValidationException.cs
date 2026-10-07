using Tabliq.Execution.Policies;

namespace Tabliq.Execution
{
    [Serializable]
    internal class PolicyValidationException : Exception
    {
        public IEnumerable<PolicyValidationError> Errors { get; }

        public PolicyValidationException(IEnumerable<PolicyValidationError> accumulatedErrors)
            : this(accumulatedErrors.ToList())
        {
        }

        private PolicyValidationException(List<PolicyValidationError> accumulatedErrors)
            : base(accumulatedErrors.Count == 1 ? accumulatedErrors[0].Message : "One or more policy validation errors occurred. See Errors for details.")
        {
            this.Errors = accumulatedErrors;
        }
    }
}