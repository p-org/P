namespace PChecker.Generator.Mutator;

internal class PctcpScheduleMutator: IMutator<PctcpScheduleGenerator>
{
    private int _meanMutationCount = 5;
    private int _meanMutationSize = 5;
    private System.Random _random = new();
    public PctcpScheduleGenerator Mutate(PctcpScheduleGenerator prev)
    {
        return new PctcpScheduleGenerator(prev.Random,
            Utils.MutateRandomChoices(prev.PriorityChoices, _meanMutationCount, _meanMutationSize, _random),
            Utils.MutateRandomChoices(prev.SwitchPointChoices, _meanMutationCount, _meanMutationSize, _random),
            prev.MaxPrioritySwitchPoints,
            prev.ScheduleLength
        );
    }

}