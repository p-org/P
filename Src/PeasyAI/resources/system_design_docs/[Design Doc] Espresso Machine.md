# Espresso Machine

## Introduction

The goal of this system is to model a coffee/espresso machine with a control panel interface. The espresso machine manages the brewing process through a series of states (idle, grinding, brewing, ready), while the control panel allows users to interact with it. This is a non-distributed state machine example focused on sequential state transitions and hardware-like behavior.

**Assumptions:**
1. The espresso machine operates sequentially — it can only make one coffee at a time.
2. The machine must complete the current brewing process before accepting new requests.
3. The grinding and brewing steps each take a fixed time simulated by a timer.
4. The machine has a water tank that must be refilled when empty.
5. The Timer machine is a pre-existing reusable module — do NOT re-implement it. Use CreateTimer(this), StartTimer(timer), and CancelTimer(timer).

## Components

### Source Components

#### 1. CoffeeMaker
- **Role:** The core espresso machine that manages the brewing process.
- **States:** Idle, Grinding, Brewing, CoffeeReady, Error
- **Local state:**
    - `waterLevel`: current water level in the tank
    - `timer`: timer for grinding and brewing durations
    - `controlPanel`: reference to the control panel
- **Initialization:** Created with a reference to the control panel.
- **Behavior:**
    - Receives commands from the CoffeeMakerControlPanel.
    - Uses a timer to simulate grinding and brewing durations.
    - Tracks water level and reports errors when water is depleted.
- **Event handling notes:**
    - In Grinding: ignore `eMakeCoffee`
    - In Brewing: ignore `eMakeCoffee`

#### 2. CoffeeMakerControlPanel
- **Role:** Interface between users and the CoffeeMaker.
- **States:** Init, Ready
- **Local state:**
    - `coffeeMaker`: reference to the coffee maker
- **Initialization:** No external configuration needed; creates the CoffeeMaker internally.
- **Behavior:**
    - Forwards user requests (make coffee, refill water) to the CoffeeMaker.
    - Receives status updates from the CoffeeMaker and reports them to the user.

### Test Components

#### 3. User
- **Role:** Simulates a user interacting with the control panel.
- **Initialization:** Created with a reference to the control panel.
- **Behavior:**
    - Sends make coffee and refill water requests to the control panel.

## Interactions

1. **eMakeCoffee**
    - **Source:** CoffeeMakerControlPanel
    - **Target:** CoffeeMaker
    - **Payload:** none
    - **Description:** Request to start making a coffee.
    - **Effects:**
        - If idle and water available, transitions to Grinding state and starts timer.
        - If busy or no water, sends error response.

2. **eCoffeeReady**
    - **Source:** CoffeeMaker
    - **Target:** CoffeeMakerControlPanel
    - **Payload:** none
    - **Description:** Coffee has been brewed and is ready for pickup.

3. **eError**
    - **Source:** CoffeeMaker
    - **Target:** CoffeeMakerControlPanel
    - **Payload:** a message describing the error
    - **Description:** An error occurred (e.g., no water, machine busy).

4. **eRefillWater**
    - **Source:** CoffeeMakerControlPanel
    - **Target:** CoffeeMaker
    - **Payload:** none
    - **Description:** Request to refill the water tank.
    - **Effects:**
        - Machine resets its water level to full.

5. **eGrindingDone**
    - **Source:** Timer (internal)
    - **Target:** CoffeeMaker
    - **Payload:** none
    - **Description:** Grinding phase timer expired, coffee is ground.
    - **Effects:**
        - Machine transitions from Grinding to Brewing state.

6. **eBrewingDone**
    - **Source:** Timer (internal)
    - **Target:** CoffeeMaker
    - **Payload:** none
    - **Description:** Brewing phase timer expired, coffee is ready.
    - **Effects:**
        - Machine transitions from Brewing to CoffeeReady state.

## Specifications

1. **NoCoffeeWhileBusy** (safety property):
   Between an eMakeCoffee that starts grinding and the eventual eCoffeeReady, no second eMakeCoffee may be accepted. The eGrindingDone and eBrewingDone transitions must complete in order before the machine returns to idle.

2. **WaterLevelTracking** (safety property):
   An eMakeCoffee must never succeed when the water level is zero. Only after an eRefillWater restores the tank should a subsequent eMakeCoffee be allowed to proceed to brewing.

## Test Scenarios

1. 1 user, 1 control panel, 1 coffee maker — a single user makes one coffee successfully.
2. 1 user, 1 control panel, 1 coffee maker — user tries to make coffee when the water tank is empty, refills, then makes coffee.
3. 2 users, 1 control panel, 1 coffee maker — two users try to make coffee in sequence, the second waits for the first to complete.
