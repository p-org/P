// PX4.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "SerialPort.h"

#define MAVLINK_PACKED
#include "mavlink.h"
#include "mavlink_helpers.h"

static const uint8_t mavlink_message_crcs[256] = MAVLINK_MESSAGE_CRCS;
static const uint8_t mavlink_message_lengths[256] = MAVLINK_MESSAGE_LENGTHS;

int ReadPackets(SerialPort port)
{
    mavlink_message_t msg;
    mavlink_status_t status;
    int channel = 0;

    HRESULT hr = 0;
    while (hr == 0)
    {
        int read = 0;
        byte data = 0;
        hr = port.Read(&data, 1, &read);

        if (hr == 0 && read == 1)
        {
            if (mavlink_frame_char(channel, data, &msg, &status) != MAVLINK_FRAMING_INCOMPLETE)
            {
                printf("Received message with ID %d, sequence: %d from component %d of system %d\n", msg.msgid, msg.seq, msg.compid, msg.sysid);


                if (msg.msgid == (byte)MAVLINK_MSG_ID_HEARTBEAT)
                {
                    mavlink_heartbeat_t heartbeat;
                    mavlink_msg_heartbeat_decode(&msg, &heartbeat);

                    printf("Received heartbeat, autopilot=%d, base_mode=%d, custom_mode=%d, mavlink_version=%d, system_status=%d, type=%d\n", 
                        heartbeat.autopilot, heartbeat.base_mode, heartbeat.custom_mode, heartbeat.mavlink_version, heartbeat.system_status, heartbeat.type);
                }
            }
        }

    }

    return 0;
}

int main()
{
    SerialPort port;

    int baudRate = 115200;
    int dataBits = 8;
    bool dtr_enable = false;
    bool rts_enable = false;
    int readTimeout = -1;
    int write_timeout = 500;
    int readBufferSize = 16384;
    int writeBufferSize = 2048;

    int hr = port.Open("COM4", baudRate, dataBits, Parity_None, StopBits_10, dtr_enable, rts_enable, Handshake_None, readTimeout, write_timeout, readBufferSize, writeBufferSize);
    if (hr != 0)
    {
        printf("Could not open the COM port, error=0x%x\n", hr);
        if (hr == ERROR_ACCESS_DENIED)
        {
            printf("Error ERROR_ACCESS_DENIED, is another app using the COM port?\n");
        }
        return 1;
    }
    char* initString = "sh /etc/init.d/rc.usb\n";
    hr = port.Write((byte*)initString, strlen(initString));

    ReadPackets(port);
    
    return 0;
}

