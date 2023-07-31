using System.Collections.Generic;
using PChecker.SystematicTesting.Operations;

namespace PChecker.Generator;

internal interface IScheduleGenerator<T>: IGenerator<T>
{
    /// <summary>
    /// Get the next scheduled operation.
    /// </summary>
    /// <param name="enabledOperations">All enabled operations.</param>
    /// <param name="current">Current operation.</param>
    /// <returns>Next enabled operation.</returns>
    public AsyncOperation? NextRandomOperation(List<AsyncOperation> enabledOperations, AsyncOperation current);


    public void PrepareForNextInput();
}