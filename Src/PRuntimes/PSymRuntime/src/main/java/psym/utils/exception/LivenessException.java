package psym.utils.exception;

import psym.valuesummary.Guard;

public class LivenessException extends BugFoundException {
  public LivenessException(String message, Guard pathConstraint) {
    super(message, pathConstraint);
  }
}
