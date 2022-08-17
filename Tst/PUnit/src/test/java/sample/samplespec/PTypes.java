package sample.samplespec;

/***************************************************************************
 * This file was auto-generated on Thursday, 07 July 2022 at 14:47:50.
 * Please do not edit manually!
 **************************************************************************/

public class PTypes {
    /* Tuples */
    // (i:int,total:int)
    public static class PTuple_i_total implements prt.values.PValue<PTuple_i_total> {
        public int i;
        public int total;

        public PTuple_i_total() {
            this.i = 0;
            this.total = 0;
        }

        public PTuple_i_total(int i, int total) {
            this.i = i;
            this.total = total;
        }

        public PTuple_i_total deepClone() {
            return new PTuple_i_total(i, total);
        } // deepClone()

        public boolean equals(Object other) {
            return (this.getClass() == other.getClass() &&
                    this.deepEquals((PTuple_i_total)other)
            );
        } // equals()

        public boolean deepEquals(PTuple_i_total other) {
            return (true
                    && this.i == other.i
                    && this.total == other.total
            );
        } // deepEquals()

        public String toString() {
            StringBuilder sb = new StringBuilder("PTuple_i_total");
            sb.append("[");
            sb.append("i=" + i);
            sb.append(", total=" + total);
            sb.append("]");
            return sb.toString();
        } // toString()
    } //PTuple_i_total class definition


}

