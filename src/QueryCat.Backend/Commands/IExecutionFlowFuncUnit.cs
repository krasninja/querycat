namespace QueryCat.Backend.Commands;

/// <summary>
/// The function unit that can change execution flow.
/// </summary>
internal interface IExecutionFlowFuncUnit
{
    ExecutionJump Jump { get; }
}
