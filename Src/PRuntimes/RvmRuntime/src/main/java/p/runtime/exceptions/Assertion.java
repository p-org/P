/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */
package p.runtime.exceptions;

public class Assertion {
    // "assert cond, msg" be translated to Assertion.rvmAssert(cond, msg)
    public static void rvmAssert(boolean cond, String errorMsg) {
        if(!cond) {
            throw new AssertStmtError(errorMsg);
        }
    }
}
