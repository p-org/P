/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */
package db;

public class Record {
    public String key;
    public int val;
    public int sqr;

    public Record(String key, int val, int sqr) {
        this.key = key;
        this.val = val;
        this.sqr = sqr;
    }

}
