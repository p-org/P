#include "PrtDistConfigParser.h"
#include "PrtDistInternals.h"
#import <msxml3.dll>
using namespace MSXML2;

#define _CRT_SECURE_NO_WARNINGS

typedef	struct _XMLNODE {
	char NodeType[MAX_LOG_SIZE];
    char NodeName[MAX_LOG_SIZE];
    char NodeValue[MAX_LOG_SIZE];
    char NodeParent[MAX_LOG_SIZE];
} XMLNODE;

XMLNODE** XMLDOMParseNodes(const char*);



void PrtDistClusterConfigInitialize(char* configurationFile)
{
	int i = 0;
	int j = 0;
	XMLNODE** listofNodes;
	XMLNODE* currNode;
	ClusterConfiguration.ClusterMachines = (char**)malloc((ClusterConfiguration.TotalNodes)*sizeof(char*));
	listofNodes = XMLDOMParseNodes(configurationFile);
	currNode = listofNodes[0];

	ClusterConfiguration.configFileName = configurationFile;
	while (currNode != NULL)
	{
		if (strcmp(currNode->NodeName, "MainExe") == 0)
		{
            char* drive = (char*)malloc(sizeof(char) * 100);
            char* dir = (char*)malloc(sizeof(char) * 1000);
            char* fname = (char*)malloc(sizeof(char) * 100);
            char* ext = (char*)malloc(sizeof(char) * 100);
            // we do NOT use GetFileTitle since that returns different things depending on user settings!!
            _splitpath_s(currNode->NodeValue, drive, 100, dir, 1000, fname, 100, ext, 100);
            strcat_s(fname, 100, ext);
			ClusterConfiguration.MainExe = fname;
            free(drive);
            free(dir);
            free(ext);
		}
		else if (strcmp(currNode->NodeName, "NodeManagerPort") == 0)
		{
			ClusterConfiguration.NodeManagerPort = currNode->NodeValue;
		}
		else if (strcmp(currNode->NodeName, "ContainerPortStart") == 0)
		{
			ClusterConfiguration.ContainerPortStart = currNode->NodeValue;
		}
		else if (strcmp(currNode->NodeName, "NetworkShare") == 0)
		{
			ClusterConfiguration.NetworkShare = currNode->NodeValue;
		}
		else if (strcmp(currNode->NodeName, "LocalFolder") == 0)
		{
			ClusterConfiguration.LocalFolder = currNode->NodeValue;
		}
		else if (strcmp(currNode->NodeName, "CentralServer") == 0)
		{
			ClusterConfiguration.CentralServer = currNode->NodeValue;
		}
		else if (strcmp(currNode->NodeName, "TotalNodes") == 0)
		{
			ClusterConfiguration.TotalNodes = atoi(currNode->NodeValue);
		}
		else if (strcmp(currNode->NodeName, "MainMachineNode") == 0)
		{
			ClusterConfiguration.MainMachineNode = currNode->NodeValue;
		}
		else if (strcmp(currNode->NodeName, "Node") == 0)
		{
			
			if (j < ClusterConfiguration.TotalNodes)
				ClusterConfiguration.ClusterMachines[j] = currNode->NodeValue;
			else
			{
				continue;
			}
			j++;
		}
		currNode = listofNodes[i];
		i++;
	}
}


//Function to throw an exception when creating COM object:
inline void TESTHR(HRESULT _hr)
{
	if FAILED(_hr)
		throw(_hr);
}

static void CopyUnicodeToUtf8(LPWSTR source, char* buffer, int bufferLength)
{
    int len = WideCharToMultiByte(CP_UTF8, 0, source, (int)wcslen(source), buffer, bufferLength - 1, 0, NULL);
    buffer[len] = '\0';
}

//The following code gets all elements found in XML file,

XMLNODE** XMLDOMParseNodes(const char *szFileName)
{
	XMLNODE** returnList = NULL;
	try
	{
		//Qualify namespase explicitly to avoid Compiler Error C2872 "ambiguous symbol" during linking.
		//Now Msxml2.dll use the "MSXML2" namespace
		//(see http://support.microsoft.com/default.aspx?scid=kb;en-us;316317):
		MSXML2::IXMLDOMDocumentPtr docPtr;//pointer to DOMDocument object
		MSXML2::IXMLDOMNodeListPtr NodeListPtr;//indexed access. and iteration through the collection of nodes
		MSXML2::IXMLDOMNodePtr DOMNodePtr;//pointer to the node

		MSXML2::IXMLDOMNode *pIDOMNode = NULL;//pointer to element's node
		MSXML2::IXMLDOMNode *pIParentNode = NULL;//pointer to parent node
		MSXML2::IXMLDOMNode *pIAttrNode = NULL;//pointer to attribute node
		MSXML2::IXMLDOMNamedNodeMapPtr DOMNamedNodeMapPtr;//iteration through the collection of attribute nodes
		MSXML2::IXMLDOMNodeList *childList = NULL;//node list containing the child nodes

		//Variable with the name of node to find:
		BSTR strFindText = L" ";//" " means to output every node

		//Variables to store item's name, parent, text and node type:
		BSTR bstrItemText, bstrItemNode, bstrItemParent, bstrNodeType;

		//Variables to store attribute's name,type and text:
		BSTR bstrAttrName, bstrAttrType, bstrAttrText;

		HRESULT hResult;

		int i = 0;//loop-index variable
		int n = 0;//lines counter

		//Initialize COM Library:
		CoInitialize(NULL);

		//Create an instance of the DOMDocument object:
		docPtr.CreateInstance(__uuidof(DOMDocument30));

		// Load a document:
		_variant_t varXml(szFileName);//XML file to load
		_variant_t varResult((bool)TRUE);//result

		varResult = docPtr->load(varXml);

		if ((bool)varResult == FALSE)
		{
			//printf("*** Error:failed to load XML file. ***\n");
			MessageBox(0, "Error: failed to load XML file. Check the file name.", \
				"Load XML file", MB_OK | MB_ICONWARNING);
			return NULL;
		}

		//Collect all or selected nodes by tag name:
		NodeListPtr = docPtr->getElementsByTagName(strFindText);

		//Output the number of nodes:
		//printf("Number of nodes: %d\n", (NodeListPtr->length));

		//Output root node:
		docPtr->documentElement->get_nodeName(&bstrItemText);
		//%ls formatting is for wchar_t* parameter's type (%s for char* type):
		//printf("\nRoot: %ls\n", bstrItemText);

		returnList = (XMLNODE**)malloc(sizeof(XMLNODE*)*NodeListPtr->length);

		for (i = 0; i < (NodeListPtr->length); i++)
		{
			if (pIDOMNode) pIDOMNode->Release();
			NodeListPtr->get_item(i, &pIDOMNode);

			if (pIDOMNode)
			{
				pIDOMNode->get_nodeTypeString(&bstrNodeType);

				//We process only elements (nodes of "element" type):
				BSTR temp = L"element";

				if (lstrcmp((LPCTSTR)bstrNodeType, (LPCTSTR)temp) == 0)
				{
					XMLNODE* currNode = (XMLNODE*)malloc(sizeof(XMLNODE));
					n++;//element node's number
					//printf("\n\n%d\n", n);//element node's number

                    CopyUnicodeToUtf8(bstrNodeType, currNode->NodeType, MAX_LOG_SIZE);
					//printf("Type: %ls\n", bstrNodeType);

					pIDOMNode->get_nodeName(&bstrItemNode);
					//printf("Node: %ls\n", bstrItemNode);
                    CopyUnicodeToUtf8(bstrItemNode, currNode->NodeName, MAX_LOG_SIZE);

					pIDOMNode->get_text(&bstrItemText);
					//printf("Text: %ls\n", bstrItemText);
                    CopyUnicodeToUtf8(bstrItemText, currNode->NodeValue, MAX_LOG_SIZE);

					pIDOMNode->get_parentNode(&pIParentNode);
					pIParentNode->get_nodeName(&bstrItemParent);
					//printf("Parent: %ls\n",bstrItemParent);
                    CopyUnicodeToUtf8(bstrItemParent, currNode->NodeParent, MAX_LOG_SIZE);

					pIDOMNode->get_childNodes(&childList);
					//printf("Child nodes: %d\n", (childList->length));

					returnList[n - 1] = currNode;

					//Get the attributes:
					int j = 0;//loop-index variable
					long length;// number of attributes in the collection

					DOMNamedNodeMapPtr = pIDOMNode->attributes;

					hResult = DOMNamedNodeMapPtr->get_length(&length);

					if (SUCCEEDED(hResult))
					{
						//Loop through the number of attributes:
						for (j = 0; j < length; j++)
						{
							//get attribute node:
							DOMNamedNodeMapPtr->get_item(j, &pIAttrNode);

							pIAttrNode->get_nodeTypeString(&bstrAttrType);//type as string
							//printf("\nAttribute type: %ls\n", bstrAttrType);
							//pIAttrNode->get_nodeType(&bstrAttrType);//enumerated type
							//printf("Attribute type: %d\n", bstrAttrType);
							pIAttrNode->get_nodeName(&bstrAttrName);
							//printf("Attribute name: %ls\n", bstrAttrName);
							pIAttrNode->get_text(&bstrAttrText);
							//printf("Attribute value: %ls\n", bstrAttrText);
						}
					}
				}
			}
		}
		returnList[n] = NULL;
		//Do not forget to release interfaces:
		pIDOMNode->Release();
		pIDOMNode = NULL;
		pIParentNode->Release();
		pIParentNode = NULL;
	}

	catch (...)
	{
		MessageBox(NULL, ("*** Exception occurred ***"), ("Error message"), MB_OK);
	}

	CoUninitialize();
	return returnList;
}