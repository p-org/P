package psym.runtime.scheduler.explicit.choiceorchestration;

public enum ChoiceLearningStateMode {
    None,
    SchedulerDepth,
    LastStep,
    MachineState,
    MachineStateAndLastStep,
    MachineStateAndEvents,
    FullState
}
