#pragma once

#include <map>
#include <string>

class DgmlNode {
public:
	std::wstring id;
	std::wstring label;
};

class DgmlLink {
public:
	DgmlNode* source;
	DgmlNode* target;
	std::wstring label;
	std::wstring index;
};

class DgmlGraph {
private:
	std::map<std::wstring, DgmlLink*> links;
	std::map<std::wstring, DgmlNode*> nodes;
public:
	DgmlNode* GetOrCreateNode(std::wstring& id, std::wstring& label)
	{
		DgmlNode* result = NULL;
		auto iter = nodes.find(id);
		if (iter == nodes.end()) {
			result = new DgmlNode();
			result->id = id;
			result->label = label;
			nodes[id] = result;
		}
		else {
			result = (*iter).second;
		}
		return result;
	}
	DgmlLink* GetOrCreateLink(std::wstring& srcNodeId, std::wstring& srcNodeLabel, std::wstring& targetNodeId, std::wstring& targetNodeLabel, std::wstring& linkLabel, int linkIndex)
	{
		DgmlNode* srcNode = GetOrCreateNode(srcNodeId, srcNodeLabel);
		DgmlNode* targetNode = GetOrCreateNode(targetNodeId, targetNodeLabel);
		DgmlLink* result = NULL;		
		char buffer[10];
		_itoa_s(linkIndex, buffer, sizeof(buffer), 10);
		std::string index = buffer;
		std::wstring windex(index.begin(), index.end());
		std::wstring linkKey = srcNodeId + L"->" + targetNodeId + L"(" + windex + L")";

		auto iter = links.find(linkKey);
		if (iter == links.end()) {
			result = new DgmlLink();
			result->label = linkLabel;
			result->source = srcNode;
			result->target = targetNode;
			if (linkIndex > 0) {
				result->index = windex;
			}
			links[linkKey] = result;
		}
		else {
			result = (*iter).second;
		}
		return result;
	}
	void WriteAttributeValue(FILE* ptr, const wchar_t* value)
	{
		wchar_t buffer[2];
		buffer[1] = '\0';
		while (*value != '\0')
		{
			wchar_t ch = *value++;
			switch (ch)
			{
			case '<':
				fwprintf(ptr, L"&lt;");
				break;
			case '\'':
				fwprintf(ptr, L"&apos;");
				break;
			case '\n':
				fwprintf(ptr, L"&#xa;");
				break;
			case '>':
				fwprintf(ptr, L"&gt;");
				break;
			case '&':
				fwprintf(ptr, L"&amp;");
				break;
			default:
				buffer[0] = ch;
				fwprintf(ptr, buffer);
				break;
			}
		}
	}
	void Save(std::wstring& fileName)
	{
		FILE* ptr;
		errno_t rc = _wfopen_s(&ptr, fileName.c_str(), L"w");
		if (rc == 0)
		{
			fwprintf(ptr, L"<DirectedGraph xmlns='http://schemas.microsoft.com/vs/2009/dgml'>\n");
			fwprintf(ptr, L"<Nodes>\n");

			for (auto iter = nodes.begin(); iter != nodes.end(); iter++)
			{
				DgmlNode* node = (*iter).second;
				fwprintf(ptr, L"  <Node Id='");
				WriteAttributeValue(ptr, node->id.c_str());
				if (node->label.length() > 0) {
					fwprintf(ptr, L"' Label='");
					WriteAttributeValue(ptr, node->label.c_str());
				}
				fwprintf(ptr, L"'/>\n");
			}
			fwprintf(ptr, L"</Nodes>\n");
			fwprintf(ptr, L"<Links>\n");
			for (auto iter = links.begin(); iter != links.end(); iter++)
			{
				DgmlLink* link = (*iter).second;
				fwprintf(ptr, L"  <Link Source='");
				WriteAttributeValue(ptr, link->source->id.c_str());
				fwprintf(ptr, L"' Target='");
				WriteAttributeValue(ptr, link->target->id.c_str());
				if (link->label.length() > 0) {
					fwprintf(ptr, L"' Label='");
					WriteAttributeValue(ptr, link->label.c_str());
				}
				if (link->index.length() > 0) {
					fwprintf(ptr, L"' Index='");
					WriteAttributeValue(ptr, link->index.c_str());
				}
				fwprintf(ptr, L"'/>\n");
			}
			fwprintf(ptr, L"</Links>\n");
			fwprintf(ptr, L"</DirectedGraph>\n");

			fclose(ptr);			
		}
	}
};