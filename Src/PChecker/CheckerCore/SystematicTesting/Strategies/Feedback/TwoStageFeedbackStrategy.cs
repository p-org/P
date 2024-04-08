using PChecker.Generator;
using PChecker.Feedback;

namespace PChecker.SystematicTesting.Strategies.Feedback;


internal class TwoStageFeedbackStrategy<TInput, TSchedule> : FeedbackGuidedStrategy<TInput, TSchedule>
    where TInput: IInputGenerator<TInput>
    where TSchedule: IScheduleGenerator<TSchedule>
{

    private int _numScheduleMutationWithoutNewSaved = 0;

    // This number should be less than `FeedbackGuidedStrategy._maxMutationsWithoutNewSaved`
    private readonly int _maxScheduleMutationsWithoutNewSaved = 25;
    public TwoStageFeedbackStrategy(CheckerConfiguration checkerConfiguration, TInput input, TSchedule schedule) : base(checkerConfiguration, input, schedule)
    {
    }

    protected override StrategyGenerator MutateGenerator(StrategyGenerator prev)
    {
        if (_numScheduleMutationWithoutNewSaved > _maxScheduleMutationsWithoutNewSaved)
        {
            _numScheduleMutationWithoutNewSaved = 0;
            return new StrategyGenerator(
                Generator.InputGenerator.Mutate(),
                // do not mutate schedule to save time?
                Generator.ScheduleGenerator.Copy()
            );
        }
        return new StrategyGenerator(
            Generator.InputGenerator.Copy(),
            Generator.ScheduleGenerator.Mutate()
        );
    }
}