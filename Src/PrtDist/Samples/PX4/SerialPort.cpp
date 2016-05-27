#include "stdafx.h"
#include "SerialPort.h"
#include <string>

SerialPort::SerialPort()
{
}


SerialPort::~SerialPort()
{
}

HRESULT
SerialPort::Open(char* portName, int baudRate, int dataBits, Parity parity, StopBits stopBits, bool dtrEnable, bool rtsEnable, Handshake hs, int readTimeout, int writeTimeout, int readBufferSize, int writeBufferSize)
{
    std::string port = portName;
    if (port.substr(0, 4) != "\\\\.\\")
    {
        port.insert(0, "\\\\.\\");
    }

    handle = CreateFileA(port.c_str(), GENERIC_READ | GENERIC_WRITE, 0, 0, OPEN_EXISTING, FILE_FLAG_OVERLAPPED, 0);

    if (handle == INVALID_HANDLE_VALUE)
    {
        return GetLastError();
    }

    HRESULT hr = SetAttributes(baudRate, parity, dataBits, stopBits, hs);

    if (!PurgeComm(handle, PURGE_RXCLEAR | PURGE_TXCLEAR) || !SetupComm(handle, readBufferSize, writeBufferSize))
    {
        return GetLastError();
    }

    COMMTIMEOUTS timeouts;			
    // FIXME: The windows api docs are not very clear about read timeouts,
    // and we have to simulate infinite with a big value (uint.MaxValue - 1)
    timeouts.ReadIntervalTimeout = MAXDWORD;
    timeouts.ReadTotalTimeoutMultiplier = MAXDWORD;
    timeouts.ReadTotalTimeoutConstant = (readTimeout == -1 ? MAXDWORD - 1 : (DWORD)readTimeout);

    timeouts.WriteTotalTimeoutMultiplier = 0;
    timeouts.WriteTotalTimeoutConstant = (writeTimeout == -1 ? MAXDWORD : (DWORD)writeTimeout);
    if (!SetCommTimeouts(handle, &timeouts))
    {
        return GetLastError();
    }

    // set signal
    DWORD dwFunc = (dtrEnable ? SETDTR : CLRDTR);
    if (!EscapeCommFunction(handle, dwFunc))
    {
        return GetLastError();
    }

    if (hs != Handshake_RequestToSend &&
        hs != Handshake_RequestToSendXonXoff)
    {
        dwFunc = (dtrEnable ? SETRTS : CLRRTS);
        if (!EscapeCommFunction(handle, dwFunc))
        {
            return GetLastError();
        }
    }

    writeOverlapped.Internal = 0;
    writeOverlapped.InternalHigh = 0;
    writeOverlapped.Offset = 0;
    writeOverlapped.OffsetHigh = 0;
    writeOverlapped.Pointer = 0;
    writeOverlapped.hEvent = CreateEvent(NULL, FALSE, FALSE, NULL);

    if (writeOverlapped.hEvent == NULL)
    {
        return E_OUTOFMEMORY;
    }

    readOverlapped.Internal = 0;
    readOverlapped.InternalHigh = 0;
    readOverlapped.Offset = 0;
    readOverlapped.OffsetHigh = 0;
    readOverlapped.Pointer = 0;
    readOverlapped.hEvent = CreateEvent(NULL, FALSE, FALSE, NULL);

    if (readOverlapped.hEvent == NULL)
    {
        return E_OUTOFMEMORY;
    }

    return S_OK;
}

HRESULT 
SerialPort::Write(byte* ptr, int count)
{
    BOOL rc = WriteFile(handle, ptr, count, NULL, &writeOverlapped);
    if (rc)
    {
        return S_OK;
    }
    HRESULT hr = GetLastError();
    if (hr != ERROR_IO_PENDING)
    {
        return hr;
    }

    DWORD bytesWritten = 0;
    if (!GetOverlappedResult(handle, &writeOverlapped, &bytesWritten, TRUE))
    {
        return GetLastError();
    }

    if (bytesWritten != count)
    {
        return E_FAIL;
    }
}

HRESULT 
SerialPort::Read(byte* buffer, int bytesToRead, int* bytesRead)
{
    if (bytesRead == NULL)
    {
        return E_POINTER;
    }
    if (buffer == NULL)
    {
        return E_POINTER;
    }

    DWORD numberOfBytesRead = 0;
    if (ReadFile(handle, buffer, bytesToRead, &numberOfBytesRead, &readOverlapped))
    {
        *bytesRead = numberOfBytesRead;
        return 0;
    }

    HRESULT hr = GetLastError();
    if (hr != ERROR_IO_PENDING)
    {
        return hr;
    }

    if (!GetOverlappedResult(handle, &readOverlapped, &numberOfBytesRead, TRUE))
    {
        return GetLastError();
    }

    *bytesRead = numberOfBytesRead;
    return 0;
}

void 
SerialPort::Close()
{

    if (handle != 0)
    {
        CloseHandle(handle);
        handle = 0;
    }
    if (writeOverlapped.hEvent != 0)
    {
        CloseHandle(writeOverlapped.hEvent);
        writeOverlapped.hEvent = 0;
    }
    if (readOverlapped.hEvent != 0)
    {
        CloseHandle(writeOverlapped.hEvent);
        readOverlapped.hEvent = 0;
    }
}

HRESULT
SerialPort::SetAttributes(int baudRate, Parity parity, int dataBits, StopBits bits, Handshake hs)
{
    DCB dcb;
    if (!GetCommState(this->handle, &dcb))
    {
        return GetLastError();
    }

    dcb.BaudRate = baudRate;
    dcb.Parity = (byte)parity;
    dcb.ByteSize = (byte)dataBits;

    // Clear Handshake flags
    dcb.fOutxCtsFlow = 0;
    dcb.fOutX = 0;
    dcb.fInX = 0;
    dcb.fRtsControl = 0;

    // Set Handshake flags
    switch (hs)
    {
    case Handshake_None:
        break;
    case Handshake_XonXoff:
        dcb.fOutX = 1;
        dcb.fInX = 1;
        break;
    case Handshake_RequestToSend:
        dcb.fOutxCtsFlow = 1;
        dcb.fRtsControl = 1;
        break;
    case Handshake_RequestToSendXonXoff:
        dcb.fOutX = 1;
        dcb.fInX = 1;
        dcb.fOutxCtsFlow = 1;
        dcb.fRtsControl = 1;
        break;
    default: // Shouldn't happen
        break;
    }

    if (!SetCommState(handle, &dcb))
    {
        return GetLastError();
    }
    return S_OK;
}