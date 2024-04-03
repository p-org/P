namespace PChecker.SystematicTesting.Strategies.Probabilistic;

public interface PriorizationProvider
{
    public int AssignPriority(int numOps);
    public double SwitchPointChoice();
}