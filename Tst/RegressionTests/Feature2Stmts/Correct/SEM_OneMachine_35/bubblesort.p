//XYZs sequences, "while", "if-then-else"
machine Main {
    var rev, sorted:seq[int];
    var i, t, s:int;
    var swapped, b:bool;

    fun reverse(l:seq[int]):seq[int] {
        i = 0;
        s = sizeof(l);
        while (i < s) {
            t = l[i];
            l -= (i);
            l += (0, t);
            i = i + 1;
        }

        return l;
    }

    fun BubbleSort(l:seq[int]):seq[int] {
        swapped = true;
        while (swapped) {
            i = 0;
            swapped = false;
            while (i < sizeof(l) - 1) {
                if (l[i] > l[i+1]) {
                    t = l[i];
                    l[i] = l[i+1];
                    l[i+1] = t;
                    swapped = true;
                }
                i = i + 1;
            }
        }

        return l;
    }

    fun IsSorted(l:seq[int]):bool {
        i = 0;
        while (i < sizeof(l) - 1) {
            if (l[i] > l[i+1])
                return false;
            i = i + 1;
        }

        return true;
    }

    start state init {
        entry {
            i = 0;
            while (i < 10) {
                rev += (0, i);
                sorted += (sizeof(sorted), i);
                i = i + 1;
            }

            assert(sizeof(rev) == 10);
            // Assert that simply reversing the list produces a sorted list
            sorted = reverse(rev);
            //assert(sizeof(sorted) == 10);
            b = IsSorted(sorted);
            assert(b);
            b = IsSorted(rev);
            assert(!b);
            // Assert that BubbleSort returns the sorted list
            sorted = BubbleSort(rev);
            assert(sizeof(sorted) == 10);
            b = IsSorted(sorted);
            assert(b);
            b = IsSorted(rev);
            assert(!b);

        }
    }
}
