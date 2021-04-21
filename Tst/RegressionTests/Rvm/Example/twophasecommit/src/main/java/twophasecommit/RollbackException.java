/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */
package twophasecommit;

public class RollbackException extends Exception {

    public RollbackException(String exception) {
        super(exception);
    }
}
