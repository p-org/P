#include "stdafx.h"
#define WIN32_LEAN_AND_MEAN

#include <windows.h>
#include <winsock2.h>
#include <ws2tcpip.h>

#include "TcpSocketPort.h"
#include <stdio.h>

HRESULT TcpSocketPort::Initialize()
{
	WSADATA wsaData;
	// Initialize Winsock
	int rc = WSAStartup(MAKEWORD(2, 2), (LPWSADATA)&wsaData);
	if (rc != 0) {
		printf("### WSAStartup failed with error: %d\n", rc);
		return rc;
	}
	return 0;
}

TcpSocketPort::TcpSocketPort()
{
	sock = INVALID_SOCKET;
	addrlen = sizeof(sockaddr_in);
	serverport = -1;
}

TcpSocketPort::~TcpSocketPort()
{
	Close();
}


// Bind the udp socket any available local port, and connect to the given 
// server and port.
HRESULT TcpSocketPort::Connect(const char* serverIp, int serverPort)
{
	struct sockaddr_in local;
	local.sin_family = AF_INET;
	local.sin_addr.s_addr = INADDR_ANY;
	local.sin_port = 0;

	struct addrinfo hints;
	ZeroMemory(&hints, sizeof(hints));
	hints.ai_family = AF_UNSPEC;
	hints.ai_socktype = SOCK_STREAM;
	hints.ai_protocol = IPPROTO_TCP;

	struct addrinfo *result = NULL;
	int rc = getaddrinfo(serverIp, "0", &hints, &result);
	if (rc == WSANOTINITIALISED)
	{
		TcpSocketPort::Initialize();
		rc = getaddrinfo(serverIp, "0", &hints, &result);
	}
	if (rc == WSAHOST_NOT_FOUND) 
	{
		printf("host not found: %s\n", serverIp);
	}
	if (rc != 0) {
		printf("getaddrinfo failed with error: %d\n", rc);
		return WSAGetLastError();
	}

	sock = socket(AF_INET, SOCK_STREAM, 0);
	bool found = false;
	for (struct addrinfo *ptr = result; ptr != NULL; ptr = ptr->ai_next) 
	{
		if (ptr->ai_family == AF_INET && ptr->ai_socktype == SOCK_STREAM && ptr->ai_protocol == IPPROTO_TCP)
		{
			// found it!
			sockaddr_in* sptr = (sockaddr_in*)ptr->ai_addr;
			serveraddr.sin_family = sptr->sin_family;
			serveraddr.sin_addr.s_addr = sptr->sin_addr.s_addr;
			serveraddr.sin_port = htons(serverPort);
			found = true;
			break;
		}
	}
	
	if (!found) {
        return E_FAIL;
	}

	freeaddrinfo(result);

	// bind socket to a local port.
	rc = bind(sock, (sockaddr*)&local, addrlen);
	if (rc < 0)
	{
		int hr = WSAGetLastError();
		printf("connect bind failed with error: %d\n", hr);
		return hr;
	}
	
	rc = connect(sock, (sockaddr*)&serveraddr, addrlen);
	if (rc != 0) {
		int hr = WSAGetLastError();
		if (WSAECONNREFUSED == hr)
		{
			printf("Server refused connection\n");
		}
		printf("connect failed with error: %d\n", hr);
		return hr;
	}
	
	return 0;
}

// write to the serial port
HRESULT TcpSocketPort::Write(const BYTE* ptr, int count)
{
	if (sock == INVALID_SOCKET)
	{
		printf("cannot send until we've connected this socket.\n");
		return 0;
	}

	serveraddr.sin_port = htons(serverport);
	int hr = send(sock, (const char*)ptr, count, 0);
	if (hr == SOCKET_ERROR)
	{
		//printf("#### send failed with error: %d\n", WSAGetLastError());
	}

	return hr;
}

// read a given number of bytes from the port.
HRESULT TcpSocketPort::Read(BYTE* buffer, int bytesToRead, int* bytesRead)
{
	  int size = 0;
	  int pos = 0;
	  char* buf = (char*)buffer;
		while (pos < bytesToRead)
		{
			pos = 0;
			size = 0;
			*bytesRead = 0;
			int rc = recv(sock, buf, bytesToRead - pos, 0);
			if (rc == 0)
			{
				printf("Connection closed\n");
			}
			else if (rc < 0)
			{
				int hr = WSAGetLastError();
				if (hr == WSAEMSGSIZE)
				{
					// skip this, probably noise, try again
				}
				else if (hr == WSAECONNRESET || hr == ERROR_IO_PENDING)
				{
					// try again - this can happen if server recreates the socket on their side.
					// so we need to reconnect.
					return hr;
				}
				else
				{
					printf("#### recv failed with error: %d\n", hr);
					return hr;
				}
			}
			else
			{
				pos += rc;
				buf += rc;
			}
	}

	*bytesRead = pos;
	return 0;
}

// close the port.
void TcpSocketPort::Close()
{
	if (sock != INVALID_SOCKET)
	{
		closesocket(sock);
	}
}
