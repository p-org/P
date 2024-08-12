
/* PSrc/FrontDesk.p */
type tRoomInfo = (roomNumber: int, isAvailable: bool);


machine FrontDesk {
    var rooms: map[int, tRoomInfo];

    start state Init {
        entry Init_Entry;
    }

    fun Init_Entry(initialRooms: map[int, tRoomInfo]) {
        new FrontDesk(default(map[int, tRoomInfo]));
    }
}


