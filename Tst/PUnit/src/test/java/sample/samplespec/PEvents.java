package sample.samplespec;

/***************************************************************************
 * This file was auto-generated on Thursday, 07 July 2022 at 14:47:50.
 * Please do not edit manually!
 **************************************************************************/

import java.util.*;

public class PEvents {
    public static class addEvent extends prt.events.PEvent<PTypes.PTuple_i_total> {
        public addEvent(PTypes.PTuple_i_total p) { this.payload = p; }
        private PTypes.PTuple_i_total payload;
        public PTypes.PTuple_i_total getPayload() { return payload; }

        @Override
        public String toString() { return "addEvent[" + payload + "]"; }
    } // addEvent

    public static class mulEvent extends prt.events.PEvent<PTypes.PTuple_i_total> {
        public mulEvent(PTypes.PTuple_i_total p) { this.payload = p; }
        private PTypes.PTuple_i_total payload;
        public PTypes.PTuple_i_total getPayload() { return payload; }

        @Override
        public String toString() { return "mulEvent[" + payload + "]"; }
    } // mulEvent

}
