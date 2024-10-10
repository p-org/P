
/* PSrc/FrontDesk.p */
type tRoomInfo = (roomNumber: int, isAvailable: bool);


machine Main {
    var rooms: map[int, tRoomInfo];

    start state Init {
        entry Init_Entry;
        exit {
            assert true;
        }
    }

    fun Init_Entry(initialRooms: map[int, tRoomInfo]) {
        new Main(default(map[int, tRoomInfo]));
    }
}


