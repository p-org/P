using PChecker.Generator.Object;
using PChecker.SystematicTesting.Strategies.Probabilistic;

namespace PChecker.Generator;

internal class ParametricProvider: PriorizationProvider
{
        public RandomChoices<int> PriorityChoices;
        public RandomChoices<double> SwitchPointChoices;
        public ParametricProvider(RandomChoices<int> priority, RandomChoices<double> switchPoint)
        {
            PriorityChoices = priority;
            SwitchPointChoices = switchPoint;
        }

        public int AssignPriority(int numOps)
        {

            return PriorityChoices.Next() % numOps + 1;
        }

        public double SwitchPointChoice()
        {
            return SwitchPointChoices.Next();
        }

}