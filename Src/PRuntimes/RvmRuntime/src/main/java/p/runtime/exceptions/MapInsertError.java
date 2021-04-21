/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */ 
package p.runtime.exceptions;

// MapInsertError is thrown when inserting duplicate keys into a map.
public class MapInsertError extends Error {
    public MapInsertError(String errorMsg) {
        super(errorMsg);
    }
}
