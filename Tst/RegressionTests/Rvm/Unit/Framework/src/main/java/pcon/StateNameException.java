/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */
package pcon;

public class StateNameException extends Error {
  private String stateName;

  public StateNameException(String stateName) {
    this.stateName = stateName;
  }

  public String getStateName() { return stateName; }
}
