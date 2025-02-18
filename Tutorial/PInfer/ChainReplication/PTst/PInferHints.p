hint Linear(e0: eReadSuccess, e1: eWriteResponse) {}
hint Hist(e0: eNotifyLog, e1: eNotifyLog) {
    @prop(refl, antisym, trans)
    fun prefixOf(x: seq[tRid], y: seq[tRid]): bool {
        var i: int;
        if (sizeof(x) > sizeof(y)) {
            return false;
        }
        i = 0;
        while (i < sizeof(x)) {
            if (!(x[i] == y[i])) {
                return false;
            }
        }
        return true;
    }
}