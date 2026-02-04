package pobserve.runtime.testcases.espressomachine;

/***************************************************************************
 * This file was auto-generated on Wednesday, 22 June 2022 at 11:28:46.
 * Please do not edit manually!
 **************************************************************************/

import pobserve.runtime.Monitor;
import pobserve.runtime.State;
import pobserve.runtime.events.PEvent;

import java.util.List;

public class EspressoMachine {
    /* Enums */
    public static class tCoffeeMakerState {
        public static final int NotWarmedUp = 0;
        public static final int Ready = 1;
        public static final int NoBeansError = 2;
        public static final int NoWaterError = 3;
    }
    public static class tCoffeeMakerOperations {
        public static final int CM_PressEspressoButton = 0;
        public static final int CM_PressSteamerButton = 1;
        public static final int CM_PressResetButton = 2;
        public static final int CM_ClearGrounds = 3;
    }

    /* Events */
    public static class DefaultEvent extends PEvent<Void> {
        public DefaultEvent() { }
        private Void payload;
        public Void getPayload() {
            return payload;
        }

        @Override
        public String toString() {
            return "DefaultEvent";
        } // toString()

    } // PEvent definition for DefaultEvent
    public static class PHalt extends PEvent<Void> {
        public PHalt() { }
        private Void payload;
        public Void getPayload() {
            return payload;
        }

        @Override
        public String toString() {
            return "PHalt";
        } // toString()

    } // PEvent definition for PHalt
    public static class eWarmUpReq extends PEvent<Void> {
        public eWarmUpReq() { }
        private Void payload;
        public Void getPayload() {
            return payload;
        }

        @Override
        public String toString() {
            return "eWarmUpReq";
        } // toString()

    } // PEvent definition for eWarmUpReq
    public static class eGrindBeansReq extends PEvent<Void> {
        public eGrindBeansReq() { }
        private Void payload;
        public Void getPayload() {
            return payload;
        }

        @Override
        public String toString() {
            return "eGrindBeansReq";
        } // toString()

    } // PEvent definition for eGrindBeansReq
    public static class eStartEspressoReq extends PEvent<Void> {
        public eStartEspressoReq() { }
        private Void payload;
        public Void getPayload() {
            return payload;
        }

        @Override
        public String toString() {
            return "eStartEspressoReq";
        } // toString()

    } // PEvent definition for eStartEspressoReq
    public static class eStartSteamerReq extends PEvent<Void> {
        public eStartSteamerReq() { }
        private Void payload;
        public Void getPayload() {
            return payload;
        }

        @Override
        public String toString() {
            return "eStartSteamerReq";
        } // toString()

    } // PEvent definition for eStartSteamerReq
    public static class eStopSteamerReq extends PEvent<Void> {
        public eStopSteamerReq() { }
        private Void payload;
        public Void getPayload() {
            return payload;
        }

        @Override
        public String toString() {
            return "eStopSteamerReq";
        } // toString()

    } // PEvent definition for eStopSteamerReq
    public static class eGrindBeansCompleted extends PEvent<Void> {
        public eGrindBeansCompleted() { }
        private Void payload;
        public Void getPayload() {
            return payload;
        }

        @Override
        public String toString() {
            return "eGrindBeansCompleted";
        } // toString()

    } // PEvent definition for eGrindBeansCompleted
    public static class eEspressoCompleted extends PEvent<Void> {
        public eEspressoCompleted() { }
        private Void payload;
        public Void getPayload() {
            return payload;
        }

        @Override
        public String toString() {
            return "eEspressoCompleted";
        } // toString()

    } // PEvent definition for eEspressoCompleted
    public static class eWarmUpCompleted extends PEvent<Void> {
        public eWarmUpCompleted() { }
        private Void payload;
        public Void getPayload() {
            return payload;
        }

        @Override
        public String toString() {
            return "eWarmUpCompleted";
        } // toString()

    } // PEvent definition for eWarmUpCompleted
    public static class eNoWaterError extends PEvent<Void> {
        public eNoWaterError() { }
        private Void payload;
        public Void getPayload() {
            return payload;
        }

        @Override
        public String toString() {
            return "eNoWaterError";
        } // toString()

    } // PEvent definition for eNoWaterError
    public static class eNoBeansError extends PEvent<Void> {
        public eNoBeansError() { }
        private Void payload;
        public Void getPayload() {
            return payload;
        }

        @Override
        public String toString() {
            return "eNoBeansError";
        } // toString()

    } // PEvent definition for eNoBeansError
    public static class eWarmerError extends PEvent<Void> {
        public eWarmerError() { }
        private Void payload;
        public Void getPayload() {
            return payload;
        }

        @Override
        public String toString() {
            return "eWarmerError";
        } // toString()

    } // PEvent definition for eWarmerError
    public static class eEspressoButtonPressed extends PEvent<Void> {
        public eEspressoButtonPressed() { }
        private Void payload;
        public Void getPayload() {
            return payload;
        }

        @Override
        public String toString() {
            return "eEspressoButtonPressed";
        } // toString()

    } // PEvent definition for eEspressoButtonPressed
    public static class eSteamerButtonOff extends PEvent<Void> {
        public eSteamerButtonOff() { }
        private Void payload;
        public Void getPayload() {
            return payload;
        }

        @Override
        public String toString() {
            return "eSteamerButtonOff";
        } // toString()

    } // PEvent definition for eSteamerButtonOff
    public static class eSteamerButtonOn extends PEvent<Void> {
        public eSteamerButtonOn() { }
        private Void payload;
        public Void getPayload() {
            return payload;
        }

        @Override
        public String toString() {
            return "eSteamerButtonOn";
        } // toString()

    } // PEvent definition for eSteamerButtonOn
    public static class eOpenGroundsDoor extends PEvent<Void> {
        public eOpenGroundsDoor() { }
        private Void payload;
        public Void getPayload() {
            return payload;
        }

        @Override
        public String toString() {
            return "eOpenGroundsDoor";
        } // toString()

    } // PEvent definition for eOpenGroundsDoor
    public static class eCloseGroundsDoor extends PEvent<Void> {
        public eCloseGroundsDoor() { }
        private Void payload;
        public Void getPayload() {
            return payload;
        }

        @Override
        public String toString() {
            return "eCloseGroundsDoor";
        } // toString()

    } // PEvent definition for eCloseGroundsDoor
    public static class eResetCoffeeMaker extends PEvent<Void> {
        public eResetCoffeeMaker() { }
        private Void payload;
        public Void getPayload() {
            return payload;
        }

        @Override
        public String toString() {
            return "eResetCoffeeMaker";
        } // toString()

    } // PEvent definition for eResetCoffeeMaker
    public static class eCoffeeMakerError extends PEvent<Integer> {
        public eCoffeeMakerError(int p) {
            this.payload = p;
        }
        private Integer payload;
        public Integer getPayload() {
            return payload;
        }

        @Override
        public String toString() {
            return "eCoffeeMakerError[" + payload + "]";
        } // toString()

    } // PEvent definition for eCoffeeMakerError
    public static class eCoffeeMakerReady extends PEvent<Void> {
        public eCoffeeMakerReady() { }
        private Void payload;
        public Void getPayload() {
            return payload;
        }

        @Override
        public String toString() {
            return "eCoffeeMakerReady";
        } // toString()

    } // PEvent definition for eCoffeeMakerReady
    public static class eCoffeeMachineUser extends PEvent<Long> {
        public eCoffeeMachineUser(long p) {
            this.payload = p;
        }
        private Long payload;
        public Long getPayload() {
            return payload;
        }

        @Override
        public String toString() {
            return "eCoffeeMachineUser[" + payload + "]";
        } // toString()

    } // PEvent definition for eCoffeeMachineUser
    public static class eInWarmUpState extends PEvent<Void> {
        public eInWarmUpState() { }
        private Void payload;
        public Void getPayload() {
            return payload;
        }

        @Override
        public String toString() {
            return "eInWarmUpState";
        } // toString()

    } // PEvent definition for eInWarmUpState
    public static class eInReadyState extends PEvent<Void> {
        public eInReadyState() { }
        private Void payload;
        public Void getPayload() {
            return payload;
        }

        @Override
        public String toString() {
            return "eInReadyState";
        } // toString()

    } // PEvent definition for eInReadyState
    public static class eInBeansGrindingState extends PEvent<Void> {
        public eInBeansGrindingState() { }
        private Void payload;
        public Void getPayload() {
            return payload;
        }

        @Override
        public String toString() {
            return "eInBeansGrindingState";
        } // toString()

    } // PEvent definition for eInBeansGrindingState
    public static class eInCoffeeBrewingState extends PEvent<Void> {
        public eInCoffeeBrewingState() { }
        private Void payload;
        public Void getPayload() {
            return payload;
        }

        @Override
        public String toString() {
            return "eInCoffeeBrewingState";
        } // toString()

    } // PEvent definition for eInCoffeeBrewingState
    public static class eErrorHappened extends PEvent<Void> {
        public eErrorHappened() { }
        private Void payload;
        public Void getPayload() {
            return payload;
        }

        @Override
        public String toString() {
            return "eErrorHappened";
        } // toString()

    } // PEvent definition for eErrorHappened
    public static class eResetPerformed extends PEvent<Void> {
        public eResetPerformed() { }
        private Void payload;
        public Void getPayload() {
            return payload;
        }

        @Override
        public String toString() {
            return "eResetPerformed";
        } // toString()

    } // PEvent definition for eResetPerformed

    // PMachine EspressoCoffeeMaker elided
    // PMachine CoffeeMakerControlPanel elided
    public static class EspressoMachineModesOfOperation extends Monitor {

        public List<Class<? extends PEvent<?>>> getEventTypes() {
            return List.of();
        } //XXX: dummy implementation.

        public void reInitializeMonitor() {}; // dummy implementation.

        public enum States {
            STARTUP_STATE,
            WARMUP_STATE,
            READY_STATE,
            BEANGRINDING_STATE,
            MAKINGCOFFEE_STATE,
            ERROR_STATE
        }


        public EspressoMachineModesOfOperation() {
            super();
            addState(new State.Builder(States.STARTUP_STATE)
                    .isInitialState(true)
                    .withEvent(eInWarmUpState.class, __ -> gotoState(States.WARMUP_STATE))
                    .build());
            addState(new State.Builder(States.WARMUP_STATE)
                    .isInitialState(false)
                    .withEvent(eErrorHappened.class, __ -> gotoState(States.ERROR_STATE))
                    .withEvent(eInReadyState.class, __ -> gotoState(States.READY_STATE))
                    .build());
            addState(new State.Builder(States.READY_STATE)
                    .isInitialState(false)
                    .withEvent(eInReadyState.class, __ -> {  })
                    .withEvent(eInBeansGrindingState.class, __ -> gotoState(States.BEANGRINDING_STATE))
                    .withEvent(eErrorHappened.class, __ -> gotoState(States.ERROR_STATE))
                    .build());
            addState(new State.Builder(States.BEANGRINDING_STATE)
                    .isInitialState(false)
                    .withEvent(eInCoffeeBrewingState.class, __ -> gotoState(States.MAKINGCOFFEE_STATE))
                    .withEvent(eErrorHappened.class, __ -> gotoState(States.ERROR_STATE))
                    .build());
            addState(new State.Builder(States.MAKINGCOFFEE_STATE)
                    .isInitialState(false)
                    .withEvent(eInReadyState.class, __ -> gotoState(States.READY_STATE))
                    .withEvent(eErrorHappened.class, __ -> gotoState(States.ERROR_STATE))
                    .build());
            addState(new State.Builder(States.ERROR_STATE)
                    .isInitialState(false)
                    .withEvent(eResetPerformed.class, __ -> gotoState(States.STARTUP_STATE))
                    .withEvent(eErrorHappened.class, __ -> {  })
                    .build());
        } // constructor
    } // EspressoMachineModesOfOperation monitor definition
    // PMachine SaneUser elided
    // PMachine CrazyUser elided
    // PMachine TestWithSaneUser elided
    // PMachine TestWithCrazyUser elided
} // EspressoMachine.java class definition
