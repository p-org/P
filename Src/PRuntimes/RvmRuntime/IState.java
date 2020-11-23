/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */ 
package mop;

import java.util.Optional;

// Interface for states.
public interface IState {
  public void entry(Optional<Object> payload) throws GotoStmtException, RaiseStmtException;
  public void exit() throws GotoStmtException, RaiseStmtException;
  public String getName();
}
