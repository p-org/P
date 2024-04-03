namespace PChecker.Generator.Mutator;

internal class PCTScheduleMutator : IMutator<PctScheduleGenerator>
{
    private int _meanMutationCount = 5;
    private int _meanMutationSize = 5;
    private System.Random _random = new();
    public PctScheduleGenerator Mutate(PctScheduleGenerator prev)
    {
        return new PctScheduleGenerator(prev.Random,
            Utils.MutateRandomChoices(prev.PriorityChoices, _meanMutationCount, _meanMutationSize, _random),
            Utils.MutateRandomChoices(prev.SwitchPointChoices, _meanMutationCount, _meanMutationSize, _random),
            prev.MaxPrioritySwitchPoints,
            prev.ScheduleLength
        );
    }
}
