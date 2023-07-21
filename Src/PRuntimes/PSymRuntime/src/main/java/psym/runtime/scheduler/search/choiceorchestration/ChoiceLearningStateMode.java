package psym.runtime.scheduler.search.choiceorchestration;

public enum ChoiceLearningStateMode {
    None,
    SchedulerDepth,
    LastStep,
    MachineState,
    MachineStateAndLastStep,
    MachineStateAndEvents,
    FullState,
    TimelineAbstraction
}
