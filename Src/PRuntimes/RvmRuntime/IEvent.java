/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */ 
package mop;

import java.util.Optional;

// Interface for events. It is used in the translation of the RaiseStmt.
public interface IEvent {
    public void handle(IState state, Optional<Object> payload) throws GotoStmtException, RaiseStmtException;
}
