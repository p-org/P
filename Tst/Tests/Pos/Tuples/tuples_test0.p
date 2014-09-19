main machine Entry {
    var a:(int, (bool, int));

    start state dummy {
        entry {
            a = (1, (true, 10));
        }
    }
}
