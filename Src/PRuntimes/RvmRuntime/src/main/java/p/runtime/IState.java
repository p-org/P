/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */ 
package p.runtime;

import java.util.Optional;

import p.runtime.exceptions.*;

// Interface for states.
public interface IState {
  public void entry(Optional<Object> payload) throws GotoStmtException, RaiseStmtException;
  public void exit() throws GotoStmtException, RaiseStmtException;
  public String getName();
}
