#ifndef PORT_H
#define PORT_H
#include <Windows.h>

class Port
{
public:
	// write to the serial port
	virtual HRESULT Write(const BYTE* ptr, int count) = 0;

	// read a given number of bytes from the port.
	virtual HRESULT Read(BYTE* buffer, int bytesToRead, int* bytesRead) = 0;

	// close the port.
	virtual void Close() = 0;
};
#endif // !PORT_H
