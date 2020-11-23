/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */
package p.runtime.exceptions;

import java.util.Optional;

import p.runtime.*;

public class GotoStmtException extends Exception {
  private IState state;
  private Optional<Object> payload;

  public GotoStmtException(IState state, Optional<Object> payload) {
    this.state = state;
    this.payload = payload;
  }

  public Optional<Object> getPayload() { return payload; }

  public IState getState() { return state; }
}
