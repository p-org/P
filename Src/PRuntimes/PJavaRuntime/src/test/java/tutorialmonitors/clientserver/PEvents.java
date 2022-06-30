package tutorialmonitors.clientserver;

/***************************************************************************
 * This file was auto-generated on Thursday, 30 June 2022 at 14:23:48.
 * Please do not edit manually!
 **************************************************************************/

import java.util.*;

public class PEvents {
    public static class eSpec_BankBalanceIsAlwaysCorrect_Init extends prt.events.PEvent<HashMap<Integer,Integer>> {
        public eSpec_BankBalanceIsAlwaysCorrect_Init(HashMap<Integer,Integer> p) { this.payload = p; }
        private HashMap<Integer,Integer> payload;
        public HashMap<Integer,Integer> getPayload() { return payload; }

        @Override
        public String toString() { return "eSpec_BankBalanceIsAlwaysCorrect_Init[" + payload + "]"; }
    } // eSpec_BankBalanceIsAlwaysCorrect_Init

    public static class eWithDrawReq extends prt.events.PEvent<PTypes.PTuple_src_accnt_amnt_rId> {
        public eWithDrawReq(PTypes.PTuple_src_accnt_amnt_rId p) { this.payload = p; }
        private PTypes.PTuple_src_accnt_amnt_rId payload;
        public PTypes.PTuple_src_accnt_amnt_rId getPayload() { return payload; }

        @Override
        public String toString() { return "eWithDrawReq[" + payload + "]"; }
    } // eWithDrawReq

    public static class eWithDrawResp extends prt.events.PEvent<PTypes.PTuple_stts_accnt_blnc_rId> {
        public eWithDrawResp(PTypes.PTuple_stts_accnt_blnc_rId p) { this.payload = p; }
        private PTypes.PTuple_stts_accnt_blnc_rId payload;
        public PTypes.PTuple_stts_accnt_blnc_rId getPayload() { return payload; }

        @Override
        public String toString() { return "eWithDrawResp[" + payload + "]"; }
    } // eWithDrawResp

}
