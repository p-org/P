#pragma once
#include <windows.h>

enum Parity {
    Parity_None = ((WORD)0x0100),
    Parity_Odd = ((WORD)0x0200),
    Parity_Even = ((WORD)0x0400),
    Parity_Mark = ((WORD)0x0800),
    Parity_Space = ((WORD)0x1000)
};

enum StopBits
{
    StopBits_None = 0,
    StopBits_10 = ((WORD)0x0001),
    StopBits_15 = ((WORD)0x0002),
    StopBits_20 = ((WORD)0x0004)
};


enum Handshake
{
    Handshake_None,
    Handshake_XonXoff,
    Handshake_RequestToSend,
    Handshake_RequestToSendXonXoff
};

class SerialPort
{
public:
    SerialPort();
    ~SerialPort();

    HRESULT Open(char* portName, int baudRate, int dataBits, Parity parity, StopBits sb, bool dtrEnable, bool rtsEnable, Handshake hs, int readTimeout, int writeTimeout, int readBufferSize, int writeBufferSize);
    HRESULT Write(byte* ptr, int count);
    HRESULT Read(byte* buffer, int bytesToRead, int* bytesRead);
    void Close();
private:
    HRESULT SetAttributes(int baud_rate, Parity parity, int data_bits, StopBits bits, Handshake hs);

    HANDLE handle;
    OVERLAPPED writeOverlapped;
    OVERLAPPED readOverlapped;
};

