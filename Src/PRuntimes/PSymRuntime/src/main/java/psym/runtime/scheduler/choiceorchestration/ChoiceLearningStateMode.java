package psym.runtime.scheduler.choiceorchestration;

public enum ChoiceLearningStateMode {
    None,
    SchedulerDepth,
    LastStep,
    MachineState,
    MachineStateAndLastStep,
    MachineStateAndEvents,
    FullState
}
