/**
 * A minimal machine that represents a traffic light.
 * Should contain a string currentColor to store the name of
 * the current color. Should contain three states Stop, Caution,
 * and Go. The light starts in the Stop state. Each state
 * just changes the currentColor variable to the appropriate color.
 */

// ========= Everything below this should be generated correctly by the model ==============

machine TrafficLight {
    var currentColor: string;

    start state Stop {
        entry {
            currentColor = "RED";
        }
    }

    state Caution {
        entry {
            currentColor = "YELLOW";
        }
    }

    state Go {
        entry {
            currentColor = "GREEN";
        }
    }
}
