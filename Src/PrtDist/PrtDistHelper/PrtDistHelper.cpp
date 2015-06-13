#include "PrtDistHelper.h"

using namespace MSXML2;

boolean _ROBOCOPY(string source, string dest)
{
	string copycommand = "robocopy " + source + " " + dest + " > " + "ROBOCOPY_PSERVICE_LOG.txt";
	if (system(copycommand.c_str()) == -1)
	{
		return false;
	}
	else
		return true;
}

void _CONCAT(char* dest, char* string1, char* string2)
{
	strcat(dest, string1);
	strcat(dest, string2);
}

string PrtDistConfigGetNetworkShare(string configFilePath) {
	int i = 0;
	char DM[200];
	XMLNODE** listofNodes;
	XMLNODE* currNode;
	string DeploymentFolder = "";
	strcpy_s(DM, 200, "NetworkShare");
	listofNodes = XMLDOMParseNodes(configFilePath.c_str());
	currNode = listofNodes[i];
	while (currNode != NULL)
	{
		if (strcmp(currNode->NodeName, DM) == 0)
		{
			DeploymentFolder = currNode->NodeValue;
		}
		currNode = listofNodes[i];
		i++;
	}
	return DeploymentFolder;
}

void PrtDistConfigGetJobNameAndJobFolder(string configFilePath, string* jobName, string* jobFolder)
{
	ifstream read;
	read.open("job.txt");
	read >> *jobName;
	read >> *jobFolder;
}

int PrtDistConfigGetCentralServerNode(string configFilePath)
{
	int i = 0;
	char DM[200];
	XMLNODE** listofNodes;
	XMLNODE* currNode;
	string centralserver = "";
	strcpy_s(DM, 200, "CentralServer");
	listofNodes = XMLDOMParseNodes(configFilePath.c_str());
	currNode = listofNodes[i];
	while (currNode != NULL)
	{
		if (strcmp(currNode->NodeName, DM) == 0)
		{
			centralserver = currNode->NodeValue;
		}
		currNode = listofNodes[i];
		i++;
	}
	int cs = atoi(centralserver.c_str());
	return cs;
}

int PrtDistConfigGetTotalNodes(string configFilePath)
{
	int i = 0;
	char DM[200];
	XMLNODE** listofNodes;
	XMLNODE* currNode;
	string totalNodes = "";
	strcpy_s(DM, 200, "NNodes");
	listofNodes = XMLDOMParseNodes(configFilePath.c_str());
	currNode = listofNodes[i];
	while (currNode != NULL)
	{
		if (strcmp(currNode->NodeName, DM) == 0)
		{
			totalNodes = currNode->NodeValue;
		}
		currNode = listofNodes[i];
		i++;
	}
	int cs = atoi(totalNodes.c_str());
	return cs;
}

string PrtDistConfigGetLocalJobFolder(string configFilePath)
{
	int i = 0;
	char DM[200];
	XMLNODE** listofNodes;
	XMLNODE* currNode;
	string localFolder = "";
	strcpy_s(DM, 200, "localFolder");
	listofNodes = XMLDOMParseNodes(configFilePath.c_str());
	currNode = listofNodes[i];
	while (currNode != NULL)
	{
		if (strcmp(currNode->NodeName, DM) == 0)
		{
			localFolder = currNode->NodeValue;
		}
		currNode = listofNodes[i];
		i++;
	}

	return localFolder;
}

//Function to throw an exception when creating COM object:
inline void TESTHR(HRESULT _hr)
{
	if FAILED(_hr)
		throw(_hr);
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

					sprintf(currNode->NodeType, "%ls", (LPCTSTR)bstrNodeType);
					//printf("Type: %ls\n", bstrNodeType);

					pIDOMNode->get_nodeName(&bstrItemNode);
					//printf("Node: %ls\n", bstrItemNode);
					sprintf(currNode->NodeName, "%ls", (LPCTSTR)bstrItemNode);

					pIDOMNode->get_text(&bstrItemText);
					//printf("Text: %ls\n", bstrItemText);
					sprintf(currNode->NodeValue, "%ls", ((LPCTSTR)bstrItemText));

					pIDOMNode->get_parentNode(&pIParentNode);
					pIParentNode->get_nodeName(&bstrItemParent);
					//printf("Parent: %ls\n",bstrItemParent);
					sprintf(currNode->NodeParent, "%ls", ((LPCTSTR)bstrItemParent));

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