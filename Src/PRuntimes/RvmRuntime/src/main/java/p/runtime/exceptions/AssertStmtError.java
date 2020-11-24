/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */
package p.runtime.exceptions;

public class AssertStmtError extends Error {
    public AssertStmtError(String errorMsg) {
        super(errorMsg);
    }
}
