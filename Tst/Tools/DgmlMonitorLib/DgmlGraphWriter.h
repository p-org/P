#pragma once

class BinaryWriter;
class TcpSocketPort;

#include "DgmlLite.h"

class DgmlGraphWriter
{
	TcpSocketPort* socket;
	BinaryWriter* writer;
	DgmlGraph graph;
	std::wstring fileName;
public:
	DgmlGraphWriter();
	~DgmlGraphWriter();
	HRESULT Connect(const char* serverName);
	HRESULT NewGraph(const wchar_t* path);
	HRESULT LoadGraph(const wchar_t* path);
	HRESULT NavigateToNode(const wchar_t* nodeId, const wchar_t* nodeLabel);
	HRESULT NavigateLink(const wchar_t* srcNodeId, const wchar_t* srcNodeLabel, const wchar_t* targetNodeId, const wchar_t* targetNodeLabel, const wchar_t* linkLabel, int linkIndex);
	HRESULT Close();

};

