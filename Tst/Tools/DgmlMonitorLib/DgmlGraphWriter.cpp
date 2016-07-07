#include "stdafx.h"
#include "TcpSocketPort.h"
#include "DgmlGraphWriter.h"
#include <string>

#define DefaultPort 18777
#define MessageHeader 0xFE771325

enum MessageType
{
	Connected,
	Disconnected,
	Ping,
	ClearText,
	LoadGraph,
	NavigateToNode,
	NavigateLink
};

class BinaryWriter
{
	Port* port;
	BYTE* _utf8Buffer;
	int _utf8BufLen;
public:
	BinaryWriter(Port *port)
	{
		this->port = port;
		_utf8Buffer = NULL;
		_utf8BufLen = 0;
	}

	void Write(BYTE value)
	{
		this->port->Write(&value, 1);
	}
	void Write(char value)
	{
		this->port->Write((const BYTE*)&value, 1);
	}
	void Write(int value)
	{
		BYTE buffer[4];
		buffer[0] = (BYTE)value;
		buffer[1] = (BYTE)(value >> 8);
		buffer[2] = (BYTE)(value >> 16);
		buffer[3] = (BYTE)(value >> 24);
		this->port->Write(buffer, 4);
	}
	void Write(unsigned int value)
	{
		BYTE buffer[4];
		buffer[0] = (BYTE)value;
		buffer[1] = (BYTE)(value >> 8);
		buffer[2] = (BYTE)(value >> 16);
		buffer[3] = (BYTE)(value >> 24);
		this->port->Write(buffer, 4);
	}
	void Write(int64_t value)
	{
		BYTE buffer[8];
		buffer[0] = (BYTE)value;
		buffer[1] = (BYTE)(value >> 8);
		buffer[2] = (BYTE)(value >> 16);
		buffer[3] = (BYTE)(value >> 24);
		buffer[4] = (BYTE)(value >> 32);
		buffer[5] = (BYTE)(value >> 40);
		buffer[6] = (BYTE)(value >> 48);
		buffer[7] = (BYTE)(value >> 56);
		this->port->Write(buffer, 8);
	}

	void Write(std::wstring value)
	{
		int size = (int)value.size();
		int len = size > 0 ? ::WideCharToMultiByte(CP_UTF8, 0, value.c_str(), size, NULL, 0, NULL, NULL) : 0;
		Write7BitEncodedInt(len);
		if (len == 0)
		{
			return;
		}
		if (len > _utf8BufLen)
		{
			if (_utf8Buffer != NULL)
			{
				free(_utf8Buffer);
			}
			_utf8BufLen = len;
			_utf8Buffer = (BYTE*)malloc(_utf8BufLen);
			if (_utf8Buffer == NULL)
			{
				throw new std::exception("out of memory");
				return;
			}
		}

		len = ::WideCharToMultiByte(CP_UTF8, 0, value.c_str(), size, (LPSTR)_utf8Buffer, _utf8BufLen, NULL, NULL);
		if (len > 0)
		{
			this->port->Write(_utf8Buffer, len);
		}
	}

protected:
	void Write7BitEncodedInt(int value) {
		// Write out an int 7 bits at a time.  The high bit of the byte,
		// when on, tells reader to continue reading more bytes.
		unsigned int v = (unsigned int)value;   // support negative numbers
		while (v >= 0x80) {
			Write((BYTE)(v | 0x80));
			v >>= 7;
		}
		Write((BYTE)v);
	}


};

static long nextMessage = 0;

class Message
{
	long messageId;
	long timestamp;
	MessageType type;
public:
	Message(MessageType type)
	{
		this->type = type;
		this->messageId = nextMessage++;
		this->timestamp = GetTickCount();
	}

	long GetMessageId() { return messageId; }
	void SetMessageId(long value) { messageId = value; }

	long GetTimestamp() { return timestamp; }
	void SetTimestamp(long value) { timestamp = value; }

	MessageType GetType() { return type; }

	virtual bool Merge(Message other)
	{
		return false;
	}

	virtual void Write(BinaryWriter* writer)
	{
		writer->Write(MessageHeader);
		writer->Write((int)this->type);
		writer->Write((int64_t)this->messageId);
		writer->Write((int64_t)this->timestamp);
	}
};

class ConnectedMessage : Message
{
	std::wstring userName;
public:
	ConnectedMessage()
		: Message(Connected)
	{
	}

	ConnectedMessage(std::wstring user)
		: Message(Connected)
	{
		this->userName = user;
	}

	std::wstring GetUser() { return this->userName; }
	void SetUser(std::wstring value) { this->userName = value; }

	virtual void Write(BinaryWriter* writer)
	{
		Message::Write(writer);
		writer->Write(this->userName);
	}
};

class ClearTextMessage : Message
{
	std::wstring text;
public:
	ClearTextMessage()
		: Message(ClearText)
	{
	}

	ClearTextMessage(std::wstring text)
		: Message(ClearText)
	{
		this->text = text;
	}

	std::wstring GetMessage() { return this->text; }
	void SetMessage(std::wstring value) { this->text = value; }

	void Write(BinaryWriter* writer)
	{
		Message::Write(writer);
		writer->Write(this->text);
	}
};

class LoadGraphMessage : Message
{
	std::wstring path;
public:
	LoadGraphMessage()
		: Message(LoadGraph)
	{
	}

	LoadGraphMessage(std::wstring path)
		: Message(LoadGraph)
	{
		this->path = path;
	}

	std::wstring GetPath() { return this->path; }
	void SetPath(std::wstring value) { this->path = value; }

	virtual void Write(BinaryWriter* writer)
	{
		Message::Write(writer);
		writer->Write(this->path);
	}

};


class NavigateNodeMessage : Message
{
	std::wstring nodeId;
	std::wstring nodeLabel;
public:
	NavigateNodeMessage()
		: Message(NavigateToNode)
	{
	}

	NavigateNodeMessage(std::wstring nodeId, std::wstring nodeLabel)
		: Message(NavigateToNode)
	{
		this->nodeId = nodeId;
		this->nodeLabel = nodeLabel;
	}

	std::wstring GetNodeId() { return this->nodeId; }
	void SetNodeId(std::wstring value) { this->nodeId = value; }

	std::wstring NodeLabel() { return this->nodeLabel; }
	void SetNodeLabel(std::wstring value) { this->nodeLabel = value; }

	virtual void Write(BinaryWriter* writer)
	{
		Message::Write(writer);
		writer->Write(this->nodeId);
		writer->Write(this->nodeLabel);
	}
};

class NavigateLinkMessage : Message
{
	std::wstring srcNodeId;
	std::wstring srcNodeLabel;
	std::wstring targetNodeId;
	std::wstring targetNodeLabel;
	std::wstring label;
	int index;
public:
	NavigateLinkMessage()
		: Message(NavigateLink)
	{
	}

	NavigateLinkMessage(std::wstring srcNodeId, std::wstring srcNodeLabel, std::wstring targetNodeId, std::wstring targetNodeLabel, std::wstring label, int index)
		: Message(NavigateLink)
	{
		this->srcNodeId = srcNodeId;
		this->srcNodeLabel = srcNodeLabel;
		this->targetNodeId = targetNodeId;
		this->targetNodeLabel = targetNodeLabel;
		this->label = label;
		this->index = index;
	}

	std::wstring GetSourceNodeId() { return this->srcNodeId; }
	void SetSourceNodeId(std::wstring value) { this->srcNodeId = value; }

	std::wstring GetSourceNodeLabel() { return this->srcNodeLabel; }
	void SetSourceNodeLabel(std::wstring value) { this->srcNodeLabel = value; }

	std::wstring GetTargetNodeId() { return this->targetNodeId; }
	void SetTargetNodeId(std::wstring value) { this->targetNodeId = value; }

	std::wstring GetTargetNodeLabel() { return this->targetNodeLabel; }
	void SetTargetNodeLabel(std::wstring value) { this->targetNodeLabel = value; }

	std::wstring GetLabel() { return this->label; }
	void SetLabel(std::wstring value) { this->label = value; }

	int GetIndex() { return this->index; }
	void SetIndex(int value) { this->index = value; }

	virtual void Write(BinaryWriter* writer)
	{
		Message::Write(writer);
		writer->Write(this->srcNodeId);
		writer->Write(this->srcNodeLabel);
		writer->Write(this->targetNodeId);
		writer->Write(this->targetNodeLabel);
		writer->Write(this->label);
		writer->Write(this->index);
	}
};


DgmlGraphWriter::DgmlGraphWriter()
{
	socket = NULL;
}


DgmlGraphWriter::~DgmlGraphWriter()
{
	if (socket != NULL)
	{
		Close();
	}
}


HRESULT DgmlGraphWriter::Connect(const char* serverName)
{
	Close();
	socket = new TcpSocketPort();
	HRESULT hr = socket->Connect(serverName, DefaultPort);
	writer = new BinaryWriter(socket);
	if (writer == NULL)
	{
		return E_OUTOFMEMORY;
	}
	return hr;
}

HRESULT DgmlGraphWriter::NewGraph(const wchar_t* path)
{
	FILE* ptr;
	errno_t rc = _wfopen_s(&ptr, path, L"w");
	if (rc == 0)
	{
		fwprintf(ptr, L"<DirectedGraph xmlns='http://schemas.microsoft.com/vs/2009/dgml'/>");
		fclose(ptr);
		LoadGraphMessage m(path);
		m.Write(writer);
	}
	return 0;
}


HRESULT DgmlGraphWriter::LoadGraph(const wchar_t* path)
{
	LoadGraphMessage m(path);
	m.Write(writer);
	return 0;
}

HRESULT DgmlGraphWriter::NavigateToNode(const wchar_t* nodeId, const wchar_t* nodeLabel)
{
	NavigateNodeMessage m(nodeId, nodeLabel);
	m.Write(writer);
	return 0;
}

HRESULT DgmlGraphWriter::NavigateLink(const wchar_t* srcNodeId, const wchar_t* srcNodeLabel, const wchar_t* targetNodeId, const wchar_t* targetNodeLabel, const wchar_t* linkLabel, int linkIndex)
{
	NavigateLinkMessage m(srcNodeId, srcNodeLabel, targetNodeId, targetNodeLabel, linkLabel, linkIndex);
	m.Write(writer);
	return 0;
}

HRESULT DgmlGraphWriter::Close()
{
	if (socket != NULL)
	{
		socket->Close();
		delete socket;
		socket = NULL;
	}
	return 0;
}