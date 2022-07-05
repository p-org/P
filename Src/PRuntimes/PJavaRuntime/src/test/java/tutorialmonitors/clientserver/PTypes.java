package tutorialmonitors.clientserver;

/***************************************************************************
 * This file was auto-generated on Thursday, 30 June 2022 at 14:23:48.
 * Please do not edit manually!
 **************************************************************************/

import java.util.*;

public class PTypes {
    /* Enums */
    public enum tWithDrawRespStatus {
        WITHDRAW_SUCCESS(0),
        WITHDRAW_ERROR(1);
        private final int value;
        tWithDrawRespStatus(int i) { value = i; }
    }

    /* Tuples */
    // (accountId:int,balance:int)
    public static class PTuple_accnt_blnc implements prt.values.PValue<PTuple_accnt_blnc> {
        public final int accountId;
        public final int balance;

        public PTuple_accnt_blnc() {
            this.accountId = 0;
            this.balance = 0;
        }

        public PTuple_accnt_blnc(int accountId, int balance) {
            this.accountId = accountId;
            this.balance = balance;
        }

        public PTuple_accnt_blnc deepClone() {
            return new PTuple_accnt_blnc(accountId, balance);
        } // deepClone()

        public boolean equals(Object other) {
            return (this.getClass() == other.getClass() &&
                    this.deepEquals((PTuple_accnt_blnc)other)
            );
        } // equals()

        public boolean deepEquals(PTuple_accnt_blnc other) {
            return (true
                    && this.accountId == other.accountId
                    && this.balance == other.balance
            );
        } // deepEquals()

        public String toString() {
            StringBuilder sb = new StringBuilder("PTuple_accnt_blnc");
            sb.append("[");
            sb.append("accountId=" + accountId);
            sb.append(", balance=" + balance);
            sb.append("]");
            return sb.toString();
        } // toString()
    } //PTuple_accnt_blnc class definition

    // (accountId:int)
    public static class PTuple_accnt implements prt.values.PValue<PTuple_accnt> {
        public final int accountId;

        public PTuple_accnt() {
            this.accountId = 0;
        }

        public PTuple_accnt(int accountId) {
            this.accountId = accountId;
        }

        public PTuple_accnt deepClone() {
            return new PTuple_accnt(accountId);
        } // deepClone()

        public boolean equals(Object other) {
            return (this.getClass() == other.getClass() &&
                    this.deepEquals((PTuple_accnt)other)
            );
        } // equals()

        public boolean deepEquals(PTuple_accnt other) {
            return (true
                    && this.accountId == other.accountId
            );
        } // deepEquals()

        public String toString() {
            StringBuilder sb = new StringBuilder("PTuple_accnt");
            sb.append("[");
            sb.append("accountId=" + accountId);
            sb.append("]");
            return sb.toString();
        } // toString()
    } //PTuple_accnt class definition

    // (source:Client,accountId:int,amount:int,rId:int)
    public static class PTuple_src_accnt_amnt_rId implements prt.values.PValue<PTuple_src_accnt_amnt_rId> {
        public final long source;
        public final int accountId;
        public final int amount;
        public final int rId;

        public PTuple_src_accnt_amnt_rId() {
            this.source = 0L;
            this.accountId = 0;
            this.amount = 0;
            this.rId = 0;
        }

        public PTuple_src_accnt_amnt_rId(long source, int accountId, int amount, int rId) {
            this.source = source;
            this.accountId = accountId;
            this.amount = amount;
            this.rId = rId;
        }

        public PTuple_src_accnt_amnt_rId deepClone() {
            return new PTuple_src_accnt_amnt_rId(source, accountId, amount, rId);
        } // deepClone()

        public boolean equals(Object other) {
            return (this.getClass() == other.getClass() &&
                    this.deepEquals((PTuple_src_accnt_amnt_rId)other)
            );
        } // equals()

        public boolean deepEquals(PTuple_src_accnt_amnt_rId other) {
            return (true
                    && this.source == other.source
                    && this.accountId == other.accountId
                    && this.amount == other.amount
                    && this.rId == other.rId
            );
        } // deepEquals()

        public String toString() {
            StringBuilder sb = new StringBuilder("PTuple_src_accnt_amnt_rId");
            sb.append("[");
            sb.append("source=" + source);
            sb.append(", accountId=" + accountId);
            sb.append(", amount=" + amount);
            sb.append(", rId=" + rId);
            sb.append("]");
            return sb.toString();
        } // toString()
    } //PTuple_src_accnt_amnt_rId class definition

    // (status:tWithDrawRespStatus,accountId:int,balance:int,rId:int)
    public static class PTuple_stts_accnt_blnc_rId implements prt.values.PValue<PTuple_stts_accnt_blnc_rId> {
        public PTypes.tWithDrawRespStatus status;
        public final int accountId;
        public final int balance;
        public final int rId;

        public PTuple_stts_accnt_blnc_rId() {
            this.status = PTypes.tWithDrawRespStatus.WITHDRAW_SUCCESS;
            this.accountId = 0;
            this.balance = 0;
            this.rId = 0;
        }

        public PTuple_stts_accnt_blnc_rId(PTypes.tWithDrawRespStatus status, int accountId, int balance, int rId) {
            this.status = status;
            this.accountId = accountId;
            this.balance = balance;
            this.rId = rId;
        }

        public PTuple_stts_accnt_blnc_rId deepClone() {
            return new PTuple_stts_accnt_blnc_rId(status, accountId, balance, rId);
        } // deepClone()

        public boolean equals(Object other) {
            return (this.getClass() == other.getClass() &&
                    this.deepEquals((PTuple_stts_accnt_blnc_rId)other)
            );
        } // equals()

        public boolean deepEquals(PTuple_stts_accnt_blnc_rId other) {
            return (true
                    && this.status == other.status
                    && this.accountId == other.accountId
                    && this.balance == other.balance
                    && this.rId == other.rId
            );
        } // deepEquals()

        public String toString() {
            StringBuilder sb = new StringBuilder("PTuple_stts_accnt_blnc_rId");
            sb.append("[");
            sb.append("status=" + status);
            sb.append(", accountId=" + accountId);
            sb.append(", balance=" + balance);
            sb.append(", rId=" + rId);
            sb.append("]");
            return sb.toString();
        } // toString()
    } //PTuple_stts_accnt_blnc_rId class definition


}
