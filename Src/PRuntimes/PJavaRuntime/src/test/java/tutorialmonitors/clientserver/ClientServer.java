package tutorialmonitors.clientserver;

/***************************************************************************
 * This file was auto-generated on Wednesday, 22 June 2022 at 11:21:10.
 * Please do not edit manually!
 **************************************************************************/

import java.text.MessageFormat;
import java.util.*;
import java.util.stream.Stream;


public class ClientServer {
    /* Enums */
    public static class tWithDrawRespStatus {
        public static final int WITHDRAW_SUCCESS = 0;
        public static final int WITHDRAW_ERROR = 1;
    }

    /* Tuples */
    // (accountId:int,balance:int)
    public static class PTuple_accnt_blnc implements prt.values.PValue<PTuple_accnt_blnc> {
        public int accountId;
        public int balance;

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
            sb.append(",balance=" + balance);
            sb.append("]");
            return sb.toString();
        } // toString()
    } //PTuple_accnt_blnc class definition

    // (accountId:int)
    public static class PTuple_accnt implements prt.values.PValue<PTuple_accnt> {
        public int accountId;

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
        public long source;
        public int accountId;
        public int amount;
        public int rId;

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
            sb.append(",accountId=" + accountId);
            sb.append(",amount=" + amount);
            sb.append(",rId=" + rId);
            sb.append("]");
            return sb.toString();
        } // toString()
    } //PTuple_src_accnt_amnt_rId class definition

    // (status:tWithDrawRespStatus,accountId:int,balance:int,rId:int)
    public static class PTuple_stts_accnt_blnc_rId implements prt.values.PValue<PTuple_stts_accnt_blnc_rId> {
        public int status;
        public int accountId;
        public int balance;
        public int rId;

        public PTuple_stts_accnt_blnc_rId() {
            this.status = 0;
            this.accountId = 0;
            this.balance = 0;
            this.rId = 0;
        }

        public PTuple_stts_accnt_blnc_rId(int status, int accountId, int balance, int rId) {
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
            sb.append(",accountId=" + accountId);
            sb.append(",balance=" + balance);
            sb.append(",rId=" + rId);
            sb.append("]");
            return sb.toString();
        } // toString()
    } //PTuple_stts_accnt_blnc_rId class definition


    /* Events */
    public static class DefaultEvent extends prt.events.PEvent<Void> {
        public DefaultEvent() { }
        private Void payload;
        public Void getPayload() { return payload; }

        @Override
        public String toString() {
            return "DefaultEvent";
        } // toString()

    } // PEvent definition for DefaultEvent
    public static class PHalt extends prt.events.PEvent<Void> {
        public PHalt() { }
        private Void payload;
        public Void getPayload() { return payload; }

        @Override
        public String toString() {
            return "PHalt";
        } // toString()

    } // PEvent definition for PHalt
    public static class eUpdateQuery extends prt.events.PEvent<PTuple_accnt_blnc> {
        public eUpdateQuery(PTuple_accnt_blnc p) { this.payload = p; }
        private PTuple_accnt_blnc payload;
        public PTuple_accnt_blnc getPayload() { return payload; }

        @Override
        public String toString() {
            return "eUpdateQuery[" + payload + "]";
        } // toString()

    } // PEvent definition for eUpdateQuery
    public static class eReadQuery extends prt.events.PEvent<PTuple_accnt> {
        public eReadQuery(PTuple_accnt p) { this.payload = p; }
        private PTuple_accnt payload;
        public PTuple_accnt getPayload() { return payload; }

        @Override
        public String toString() {
            return "eReadQuery[" + payload + "]";
        } // toString()

    } // PEvent definition for eReadQuery
    public static class eReadQueryResp extends prt.events.PEvent<PTuple_accnt_blnc> {
        public eReadQueryResp(PTuple_accnt_blnc p) { this.payload = p; }
        private PTuple_accnt_blnc payload;
        public PTuple_accnt_blnc getPayload() { return payload; }

        @Override
        public String toString() {
            return "eReadQueryResp[" + payload + "]";
        } // toString()

    } // PEvent definition for eReadQueryResp
    public static class eWithDrawReq extends prt.events.PEvent<PTuple_src_accnt_amnt_rId> {
        public eWithDrawReq(PTuple_src_accnt_amnt_rId p) { this.payload = p; }
        private PTuple_src_accnt_amnt_rId payload;
        public PTuple_src_accnt_amnt_rId getPayload() { return payload; }

        @Override
        public String toString() {
            return "eWithDrawReq[" + payload + "]";
        } // toString()

    } // PEvent definition for eWithDrawReq
    public static class eWithDrawResp extends prt.events.PEvent<PTuple_stts_accnt_blnc_rId> {
        public eWithDrawResp(PTuple_stts_accnt_blnc_rId p) { this.payload = p; }
        private PTuple_stts_accnt_blnc_rId payload;
        public PTuple_stts_accnt_blnc_rId getPayload() { return payload; }

        @Override
        public String toString() {
            return "eWithDrawResp[" + payload + "]";
        } // toString()

    } // PEvent definition for eWithDrawResp
    public static class eSpec_BankBalanceIsAlwaysCorrect_Init extends prt.events.PEvent<HashMap<Integer,Integer>> {
        public eSpec_BankBalanceIsAlwaysCorrect_Init(HashMap<Integer,Integer> p) { this.payload = p; }
        private HashMap<Integer,Integer> payload;
        public HashMap<Integer,Integer> getPayload() { return payload; }

        @Override
        public String toString() {
            return "eSpec_BankBalanceIsAlwaysCorrect_Init[" + payload + "]";
        } // toString()

    } // PEvent definition for eSpec_BankBalanceIsAlwaysCorrect_Init

    // PMachine BankServer elided
    // PMachine Database elided
    // PMachine Client elided
    // PMachine AbstractBankServer elided
    public static class BankBalanceIsAlwaysCorrect extends prt.Monitor {
        private HashMap<Integer,Integer> bankBalance = new HashMap<Integer,Integer>();
        public HashMap<Integer,Integer> get_bankBalance() { return this.bankBalance; };

        private HashMap<Integer,PTuple_src_accnt_amnt_rId> pendingWithDraws = new HashMap<Integer,PTuple_src_accnt_amnt_rId>();
        public HashMap<Integer,PTuple_src_accnt_amnt_rId> get_pendingWithDraws() { return this.pendingWithDraws; };


        public String INIT_STATE = "Init";
        public String WAITFORWITHDRAWREQANDRESP_STATE = "WaitForWithDrawReqAndResp";

        private void Anon(HashMap<Integer,Integer> balance) {
            bankBalance = (HashMap<Integer,Integer>)prt.values.Clone.deepClone(balance);
        }
        private void Anon_1(PTuple_src_accnt_amnt_rId req) {
            int TMP_tmp0 = 0;
            boolean TMP_tmp1 = false;
            int TMP_tmp2 = 0;
            ArrayList<Integer> TMP_tmp3 = new ArrayList<Integer>();
            String TMP_tmp4 = "";
            int TMP_tmp5 = 0;

            TMP_tmp0 = req.accountId;
            TMP_tmp1 = bankBalance.containsKey(TMP_tmp0);
            TMP_tmp2 = req.accountId;
            TMP_tmp3 = new ArrayList<Integer>(bankBalance.keySet());
            TMP_tmp4 = MessageFormat.format("Unknown accountId {0} in the withdraw request. Valid accountIds = {1}", TMP_tmp2, TMP_tmp3);
            tryAssert(TMP_tmp1, TMP_tmp4);
            TMP_tmp5 = req.rId;
            pendingWithDraws.put(TMP_tmp5,req.deepClone());
        }
        private void Anon_2(PTuple_stts_accnt_blnc_rId resp) {
            int TMP_tmp0_1 = 0;
            boolean TMP_tmp1_1 = false;
            int TMP_tmp2_1 = 0;
            String TMP_tmp3_1 = "";
            int TMP_tmp4_1 = 0;
            boolean TMP_tmp5_1 = false;
            int TMP_tmp6 = 0;
            String TMP_tmp7 = "";
            int TMP_tmp8 = 0;
            boolean TMP_tmp9 = false;
            String TMP_tmp10 = "";
            int TMP_tmp11 = 0;
            boolean TMP_tmp12 = false;
            int TMP_tmp13 = 0;
            int TMP_tmp14 = 0;
            int TMP_tmp15 = 0;
            int TMP_tmp16 = 0;
            PTuple_src_accnt_amnt_rId TMP_tmp17 = new PTuple_src_accnt_amnt_rId();
            int TMP_tmp18 = 0;
            int TMP_tmp19 = 0;
            boolean TMP_tmp20 = false;
            int TMP_tmp21 = 0;
            int TMP_tmp22 = 0;
            int TMP_tmp23 = 0;
            int TMP_tmp24 = 0;
            int TMP_tmp25 = 0;
            PTuple_src_accnt_amnt_rId TMP_tmp26 = new PTuple_src_accnt_amnt_rId();
            int TMP_tmp27 = 0;
            int TMP_tmp28 = 0;
            String TMP_tmp29 = "";
            int TMP_tmp30 = 0;
            int TMP_tmp31 = 0;
            int TMP_tmp32 = 0;
            int TMP_tmp33 = 0;
            int TMP_tmp34 = 0;
            int TMP_tmp35 = 0;
            PTuple_src_accnt_amnt_rId TMP_tmp36 = new PTuple_src_accnt_amnt_rId();
            int TMP_tmp37 = 0;
            int TMP_tmp38 = 0;
            boolean TMP_tmp39 = false;
            int TMP_tmp40 = 0;
            PTuple_src_accnt_amnt_rId TMP_tmp41 = new PTuple_src_accnt_amnt_rId();
            int TMP_tmp42 = 0;
            int TMP_tmp43 = 0;
            int TMP_tmp44 = 0;
            String TMP_tmp45 = "";
            int TMP_tmp46 = 0;
            int TMP_tmp47 = 0;
            int TMP_tmp48 = 0;
            boolean TMP_tmp49 = false;
            int TMP_tmp50 = 0;
            int TMP_tmp51 = 0;
            int TMP_tmp52 = 0;
            String TMP_tmp53 = "";

            TMP_tmp0_1 = resp.accountId;
            TMP_tmp1_1 = bankBalance.containsKey(TMP_tmp0_1);
            TMP_tmp2_1 = resp.accountId;
            TMP_tmp3_1 = MessageFormat.format("Unknown accountId {0} in the withdraw response!", TMP_tmp2_1);
            tryAssert(TMP_tmp1_1, TMP_tmp3_1);
            TMP_tmp4_1 = resp.rId;
            TMP_tmp5_1 = pendingWithDraws.containsKey(TMP_tmp4_1);
            TMP_tmp6 = resp.rId;
            TMP_tmp7 = MessageFormat.format("Unknown rId {0} in the withdraw response!", TMP_tmp6);
            tryAssert(TMP_tmp5_1, TMP_tmp7);
            TMP_tmp8 = resp.balance;
            TMP_tmp9 = TMP_tmp8 >= 10;
            TMP_tmp10 = "Bank balance in all accounts must always be greater than or equal to 10!!";
            tryAssert(TMP_tmp9, TMP_tmp10);
            TMP_tmp11 = resp.status;
            TMP_tmp12 = TMP_tmp11 == tWithDrawRespStatus.WITHDRAW_SUCCESS;
            if (TMP_tmp12) {
                TMP_tmp13 = resp.balance;
                TMP_tmp14 = resp.accountId;
                TMP_tmp15 = bankBalance.get(TMP_tmp14);
                TMP_tmp16 = resp.rId;
                TMP_tmp17 = pendingWithDraws.get(TMP_tmp16);
                TMP_tmp18 = TMP_tmp17.amount;
                TMP_tmp19 = TMP_tmp15 - TMP_tmp18;
                TMP_tmp20 = TMP_tmp13 == TMP_tmp19;
                TMP_tmp21 = resp.accountId;
                TMP_tmp22 = resp.balance;
                TMP_tmp23 = resp.accountId;
                TMP_tmp24 = bankBalance.get(TMP_tmp23);
                TMP_tmp25 = resp.rId;
                TMP_tmp26 = pendingWithDraws.get(TMP_tmp25);
                TMP_tmp27 = TMP_tmp26.amount;
                TMP_tmp28 = TMP_tmp24 - TMP_tmp27;
                TMP_tmp29 = MessageFormat.format("Bank balance for the account {0} is {1} and not the expected value {2}, Bank is lying!", TMP_tmp21, TMP_tmp22, TMP_tmp28);
                tryAssert(TMP_tmp20, TMP_tmp29);
                TMP_tmp30 = resp.accountId;
                TMP_tmp31 = resp.balance;
                TMP_tmp32 = TMP_tmp31;
                bankBalance.put(TMP_tmp30,TMP_tmp32);
            }
            else
            {
                TMP_tmp33 = resp.accountId;
                TMP_tmp34 = bankBalance.get(TMP_tmp33);
                TMP_tmp35 = resp.rId;
                TMP_tmp36 = pendingWithDraws.get(TMP_tmp35);
                TMP_tmp37 = TMP_tmp36.amount;
                TMP_tmp38 = TMP_tmp34 - TMP_tmp37;
                TMP_tmp39 = TMP_tmp38 < 10;
                TMP_tmp40 = resp.rId;
                TMP_tmp41 = pendingWithDraws.get(TMP_tmp40);
                TMP_tmp42 = TMP_tmp41.amount;
                TMP_tmp43 = resp.accountId;
                TMP_tmp44 = bankBalance.get(TMP_tmp43);
                TMP_tmp45 = MessageFormat.format("Bank must accept the withdraw request for {0}, bank balance is {1}!", TMP_tmp42, TMP_tmp44);
                tryAssert(TMP_tmp39, TMP_tmp45);
                TMP_tmp46 = resp.accountId;
                TMP_tmp47 = bankBalance.get(TMP_tmp46);
                TMP_tmp48 = resp.balance;
                TMP_tmp49 = TMP_tmp47 == TMP_tmp48;
                TMP_tmp50 = resp.accountId;
                TMP_tmp51 = bankBalance.get(TMP_tmp50);
                TMP_tmp52 = resp.balance;
                TMP_tmp53 = MessageFormat.format("Withdraw failed BUT the account balance changed! actual: {0}, bank said: {1}", TMP_tmp51, TMP_tmp52);
                tryAssert(TMP_tmp49, TMP_tmp53);
            }
        }

        public BankBalanceIsAlwaysCorrect() {
            super();
            addState(new prt.State.Builder(INIT_STATE)
                    .isInitialState(true)
                    .withEvent(eSpec_BankBalanceIsAlwaysCorrect_Init.class, p -> { Anon(p); gotoState(WAITFORWITHDRAWREQANDRESP_STATE); })
                    .build());
            addState(new prt.State.Builder(WAITFORWITHDRAWREQANDRESP_STATE)
                    .isInitialState(false)
                    .withEvent(eWithDrawReq.class, this::Anon_1)
                    .withEvent(eWithDrawResp.class, this::Anon_2)
                    .build());
        } // constructor
    } // BankBalanceIsAlwaysCorrect monitor definition
    public static class GuaranteedWithDrawProgress extends prt.Monitor {
        private LinkedHashSet<Integer> pendingWDReqs = new LinkedHashSet<Integer>();
        public LinkedHashSet<Integer> get_pendingWDReqs() { return this.pendingWDReqs; };


        public String NOPENDINGREQUESTS_STATE = "NopendingRequests";
        public String PENDINGREQS_STATE = "PendingReqs";

        private void Anon_3(PTuple_src_accnt_amnt_rId req_1) {
            int TMP_tmp0_2 = 0;

            TMP_tmp0_2 = req_1.rId;
            pendingWDReqs.add(TMP_tmp0_2);
        }
        private void Anon_4(PTuple_stts_accnt_blnc_rId resp_1)throws prt.TransitionException {
            int TMP_tmp0_3 = 0;
            boolean TMP_tmp1_2 = false;
            int TMP_tmp2_2 = 0;
            LinkedHashSet<Integer> TMP_tmp3_2 = new LinkedHashSet<Integer>();
            String TMP_tmp4_2 = "";
            int TMP_tmp5_2 = 0;
            int TMP_tmp6_1 = 0;
            boolean TMP_tmp7_1 = false;

            TMP_tmp0_3 = resp_1.rId;
            TMP_tmp1_2 = pendingWDReqs.contains(TMP_tmp0_3);
            TMP_tmp2_2 = resp_1.rId;
            TMP_tmp3_2 = (LinkedHashSet<Integer>)prt.values.Clone.deepClone(pendingWDReqs);
            TMP_tmp4_2 = MessageFormat.format("unexpected rId: {0} received, expected one of {1}", TMP_tmp2_2, TMP_tmp3_2);
            tryAssert(TMP_tmp1_2, TMP_tmp4_2);
            TMP_tmp5_2 = resp_1.rId;
            pendingWDReqs.remove(TMP_tmp5_2);
            TMP_tmp6_1 = pendingWDReqs.size();
            TMP_tmp7_1 = TMP_tmp6_1 == 0;
            if (TMP_tmp7_1) {
                gotoState(NOPENDINGREQUESTS_STATE);
                return;
            }
        }
        private void Anon_5(PTuple_src_accnt_amnt_rId req_2) {
            int TMP_tmp0_4 = 0;

            TMP_tmp0_4 = req_2.rId;
            pendingWDReqs.add(TMP_tmp0_4);
        }

        public GuaranteedWithDrawProgress() {
            super();
            addState(new prt.State.Builder(NOPENDINGREQUESTS_STATE)
                    .isInitialState(true)
                    .withEvent(eWithDrawReq.class, p -> { Anon_3(p); gotoState(PENDINGREQS_STATE); })
                    .build());
            addState(new prt.State.Builder(PENDINGREQS_STATE)
                    .isInitialState(false)
                    .withEvent(eWithDrawResp.class, this::Anon_4)
                    .withEvent(eWithDrawReq.class, p -> { Anon_5(p); gotoState(PENDINGREQS_STATE); })
                    .build());
        } // constructor
    } // GuaranteedWithDrawProgress monitor definition
    // PMachine TestWithSingleClient elided
    // PMachine TestWithMultipleClients elided
} // ClientServer.java class definition
