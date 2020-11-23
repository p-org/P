/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */ 
package p.runtime.exceptions;

import java.util.Optional;

import p.runtime.*;

public class RaiseStmtException extends Exception {
  private IEvent event;
  private Optional<Object> payload;

  public RaiseStmtException(IEvent event, Optional<Object> payload) {
    this.event = event;
    this.payload = payload;
  }

  public Optional<Object> getPayload() { return payload; }

  public IEvent getEvent() { return event; }
}
