package tutorialmonitors.espressomachine;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;

import static tutorialmonitors.espressomachine.EspressoMachine.*;

public class EspressoMachineTest {
    @Test
    @DisplayName("Can start up the espresso machine")
    public void testStartup() {
        EspressoMachineModesOfOperation m = new EspressoMachineModesOfOperation();
        m.ready();

        assertEquals(m.getCurrentState(), "StartUp");
        m.process(new eInWarmUpState());
        assertEquals(m.getCurrentState(), "WarmUp");
    }

    @Test
    @DisplayName("Can drive the espresso machine")
    public void testOperation() {
        EspressoMachineModesOfOperation m = new EspressoMachineModesOfOperation();
        m.ready();

        m.process(new eInWarmUpState());
        m.process(new eInReadyState());
        m.process(new eInReadyState()); // Duplicate; should be ignored.
        m.process(new eInBeansGrindingState());
        m.process(new eInCoffeeBrewingState());
        m.process(new eInReadyState());
    }

    @Test
    @DisplayName("Can drive the espresso machine into an error state and recover")
    public void testError() {
        EspressoMachineModesOfOperation m = new EspressoMachineModesOfOperation();
        m.ready();

        m.process(new eInWarmUpState());
        m.process(new eInReadyState());
        m.process(new eInBeansGrindingState());

        m.process(new eErrorHappened());
        assertEquals(m.getCurrentState(), "Error");
        m.process(new eResetPerformed());
        assertEquals(m.getCurrentState(), "StartUp");
    }
}
