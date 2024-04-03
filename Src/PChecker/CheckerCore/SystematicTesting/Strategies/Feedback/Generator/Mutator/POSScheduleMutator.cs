namespace PChecker.Generator.Mutator;

internal class POSScheduleMutator: IMutator<POSScheduleGenerator>
{
    private int _meanMutationCount = 5;
    private int _meanMutationSize = 5;
    private System.Random _random = new();
    public POSScheduleGenerator Mutate(POSScheduleGenerator prev)
    {
        return new POSScheduleGenerator(prev.Random,
            Utils.MutateRandomChoices(prev.PriorityChoices, _meanMutationCount, _meanMutationSize, _random),
            Utils.MutateRandomChoices(prev.SwitchPointChoices, _meanMutationCount, _meanMutationSize, _random),
            prev.Monitor
        );
    }
}