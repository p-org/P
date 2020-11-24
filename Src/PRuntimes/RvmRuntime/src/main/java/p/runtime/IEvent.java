/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */ 
package p.runtime;

import java.util.Optional;

import p.runtime.exceptions.*;

// Interface for events. It is used in the translation of the RaiseStmt.
public interface IEvent {
    public void handle(IState state, Optional<Object> payload) throws GotoStmtException, RaiseStmtException;
}
