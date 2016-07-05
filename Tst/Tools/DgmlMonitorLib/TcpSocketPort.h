#ifndef TCPSOCKET_H
#define TCPSOCKET_H

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#include <windows.h>
#include <WinSock2.h>
#include "Port.h"

// Need to link with Ws2_32.lib
#pragma comment (lib, "Ws2_32.lib")

class TcpSocketPort : public Port
{
	SOCKET sock = INVALID_SOCKET;
	sockaddr_in serveraddr;
	sockaddr_in other;
	int addrlen = sizeof(sockaddr_in);
	int serverport = -1;

public:
	TcpSocketPort();
	~TcpSocketPort();

	static HRESULT Initialize();

	// Connect the TCP socket to the given server & port.
	HRESULT Connect(const char* serverIp, int serverPort);

	// write to the socket
	HRESULT Write(const BYTE* ptr, int count);

	// read a given number of bytes from the socket.
	HRESULT Read(BYTE* buffer, int bytesToRead, int* bytesRead);

	// close the socket.
	void Close();
};


#endif // !SOCKETPORT_H
