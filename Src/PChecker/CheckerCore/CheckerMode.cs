namespace PChecker;

/// <summary>
/// P Checker exploration modes
/// </summary>
public enum CheckerMode
{
    /// <summary>
    /// Mode for prioritized random search
    /// </summary>
    BugFinding,
    /// <summary>
    /// Mode for exhaustive symbolic exploration
    /// </summary>
    Verification,
    /// <summary>
    /// Mode for exhaustive explicit-state search with state-space coverage reporting
    /// </summary>
    Coverage
}