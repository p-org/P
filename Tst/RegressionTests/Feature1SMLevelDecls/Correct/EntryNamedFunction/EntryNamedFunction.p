type tRoomInfo = (roomNumber: int, isAvailable: bool);


machine Main {
    start state Init {
        entry {
            new m1(default(map[int, tRoomInfo]));
        }
    }
}

machine m1 {
    start state Init {
        entry Init_Entry;
    }

    fun Init_Entry(initialRooms: map[int, tRoomInfo]) {
        assert true;
    }
}