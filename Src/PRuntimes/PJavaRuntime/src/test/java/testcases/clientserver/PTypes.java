package testcases.clientserver;

/***************************************************************************
 * This file was auto-generated on Monday, 08 August 2022 at 17:03:57.
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

    public static class PTuple_accnt_blnc implements prt.values.PValue<PTuple_accnt_blnc> {
        // (accountId:int,balance:int)
        public long accountId;
        public long balance;

        public PTuple_accnt_blnc() {
            this.accountId = 0L;
            this.balance = 0L;
        }

        public PTuple_accnt_blnc(long accountId, long balance) {
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

        public int hashCode() {
            return Objects.hash(accountId, balance);
        } // hashCode()

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

    public static class PTuple_accnt implements prt.values.PValue<PTuple_accnt> {
        // (accountId:int)
        public long accountId;

        public PTuple_accnt() {
            this.accountId = 0L;
        }

        public PTuple_accnt(long accountId) {
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

        public int hashCode() {
            return Objects.hash(accountId);
        } // hashCode()

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

    public static class PTuple_src_accnt_amnt_rId implements prt.values.PValue<PTuple_src_accnt_amnt_rId> {
        // (source:Client,accountId:int,amount:int,rId:int)
        public long source;
        public long accountId;
        public long amount;
        public long rId;

        public PTuple_src_accnt_amnt_rId() {
            this.source = 0L;
            this.accountId = 0L;
            this.amount = 0L;
            this.rId = 0L;
        }

        public PTuple_src_accnt_amnt_rId(long source, long accountId, long amount, long rId) {
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

        public int hashCode() {
            return Objects.hash(source, accountId, amount, rId);
        } // hashCode()

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

    public static class PTuple_stts_accnt_blnc_rId implements prt.values.PValue<PTuple_stts_accnt_blnc_rId> {
        // (status:tWithDrawRespStatus,accountId:int,balance:int,rId:int)
        public PTypes.tWithDrawRespStatus status;
        public long accountId;
        public long balance;
        public long rId;

        public PTuple_stts_accnt_blnc_rId() {
            this.status = PTypes.tWithDrawRespStatus.WITHDRAW_SUCCESS;
            this.accountId = 0L;
            this.balance = 0L;
            this.rId = 0L;
        }

        public PTuple_stts_accnt_blnc_rId(PTypes.tWithDrawRespStatus status, long accountId, long balance, long rId) {
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

        public int hashCode() {
            return Objects.hash(status, accountId, balance, rId);
        } // hashCode()

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

    public static class PTuple_srvr_intlb implements prt.values.PValue<PTuple_srvr_intlb> {
        // (server:BankServer,initialBalance:map[int,int])
        public long server;
        public HashMap<Long, Long> initialBalance;

        public PTuple_srvr_intlb() {
            this.server = 0L;
            this.initialBalance = new HashMap<Long, Long>();
        }

        public PTuple_srvr_intlb(long server, HashMap<Long, Long> initialBalance) {
            this.server = server;
            this.initialBalance = initialBalance;
        }

        public PTuple_srvr_intlb deepClone() {
            return new PTuple_srvr_intlb(server, prt.values.Clone.deepClone(initialBalance));
        } // deepClone()


        public boolean equals(Object other) {
            return (this.getClass() == other.getClass() &&
                    this.deepEquals((PTuple_srvr_intlb)other)
            );
        } // equals()

        public int hashCode() {
            return Objects.hash(server, initialBalance);
        } // hashCode()

        public boolean deepEquals(PTuple_srvr_intlb other) {
            return (true
                    && this.server == other.server
                    && prt.values.Equality.deepEquals(this.initialBalance, other.initialBalance)
            );
        } // deepEquals()


        public String toString() {
            StringBuilder sb = new StringBuilder("PTuple_srvr_intlb");
            sb.append("[");
            sb.append("server=" + server);
            sb.append(", initialBalance=" + initialBalance);
            sb.append("]");
            return sb.toString();
        } // toString()

    } //PTuple_srvr_intlb class definition

    public static class PTuple_serv_accnt_blnc implements prt.values.PValue<PTuple_serv_accnt_blnc> {
        // (serv:BankServer,accountId:int,balance:int)
        public long serv;
        public long accountId;
        public long balance;

        public PTuple_serv_accnt_blnc() {
            this.serv = 0L;
            this.accountId = 0L;
            this.balance = 0L;
        }

        public PTuple_serv_accnt_blnc(long serv, long accountId, long balance) {
            this.serv = serv;
            this.accountId = accountId;
            this.balance = balance;
        }

        public PTuple_serv_accnt_blnc deepClone() {
            return new PTuple_serv_accnt_blnc(serv, accountId, balance);
        } // deepClone()


        public boolean equals(Object other) {
            return (this.getClass() == other.getClass() &&
                    this.deepEquals((PTuple_serv_accnt_blnc)other)
            );
        } // equals()

        public int hashCode() {
            return Objects.hash(serv, accountId, balance);
        } // hashCode()

        public boolean deepEquals(PTuple_serv_accnt_blnc other) {
            return (true
                    && this.serv == other.serv
                    && this.accountId == other.accountId
                    && this.balance == other.balance
            );
        } // deepEquals()


        public String toString() {
            StringBuilder sb = new StringBuilder("PTuple_serv_accnt_blnc");
            sb.append("[");
            sb.append("serv=" + serv);
            sb.append(", accountId=" + accountId);
            sb.append(", balance=" + balance);
            sb.append("]");
            return sb.toString();
        } // toString()

    } //PTuple_serv_accnt_blnc class definition


}
