// QueryNodes.cpp : Defines the entry point for the console application.
//
//Source: queryNodes.cpp
//This example application calls both the selectSingleNode method 
//and the selectNodes method on an XML 

#include "stdafx.h"

#include <stdio.h>
#include <windows.h>
#import <msxml4.dll> raw_interfaces_only
using namespace MSXML2;

// Macro that calls a COM method returning HRESULT value:
#define HRCALL(a, errmsg) \
do { \
    hr = (a); \
    if (FAILED(hr)) { \
        dprintf( "%s:%d  HRCALL Failed: %s\n  0x%.8x = %s\n", \
                __FILE__, __LINE__, errmsg, hr, #a ); \
        goto clean; \
    } \
} while (0)

// Helper function that put output in stdout and debug window
// in Visual Studio:
void dprintf( char * format, ...)
{
    static char buf[1024];
    va_list args;
    va_start( args, format );
    vsprintf( buf, format, args );
    va_end( args);
    OutputDebugStringA( buf);
    printf("%s", buf);
}

// Helper function to create a DOM instance: 
IXMLDOMDocument * DomFromCOM()
{
   HRESULT hr;
   IXMLDOMDocument *pxmldoc = NULL;

    HRCALL( CoCreateInstance(__uuidof(DOMDocument40),
                      NULL,
                      CLSCTX_INPROC_SERVER,
                      __uuidof(IXMLDOMDocument),
                      (void**)&pxmldoc),
            "Create a new DOMDocument");

    HRCALL( pxmldoc->put_async(VARIANT_FALSE),
            "should never fail");
    HRCALL( pxmldoc->put_validateOnParse(VARIANT_FALSE),
            "should never fail");
    HRCALL( pxmldoc->put_resolveExternals(VARIANT_FALSE),
            "should never fail");

   return pxmldoc;
clean:
   if (pxmldoc)
    {
      pxmldoc->Release();
    }
   return NULL;
}

VARIANT VariantString(BSTR str)
{
   VARIANT var;
   VariantInit(&var);
   V_BSTR(&var) = SysAllocString(str);
   V_VT(&var) = VT_BSTR;
   return var;
}

void ReportParseError(IXMLDOMDocument *pDom, char *desc) {
   IXMLDOMParseError *pXMLErr=NULL;
   BSTR bstrReason = NULL;
   HRESULT hr;
   HRCALL(pDom->get_parseError(&pXMLErr),
            "dom->get_parseError: ");
   HRCALL(pXMLErr->get_reason(&bstrReason),
            "parseError->get_reason: ");
   
   dprintf("%s %S\n",desc, bstrReason);
clean:
   if (pXMLErr) pXMLErr->Release();
   if (bstrReason) SysFreeString(bstrReason);
}

int main(int argc, char* argv[])
{
   IXMLDOMDocument *pXMLDom=NULL;
   IXMLDOMNodeList *pNodes=NULL;
   IXMLDOMNode *pNode=NULL;
   BSTR bstr = NULL;
   VARIANT_BOOL status;
   VARIANT var;
   HRESULT hr;
   long length;

   CoInitialize(NULL);

   pXMLDom = DomFromCOM();
   if (!pXMLDom) goto clean;

   VariantInit(&var);
   var = VariantString(L"stocks.xml");
   HRCALL(pXMLDom->load(var, &status), "dom->load(): ");

   if (status!=VARIANT_TRUE) {
      ReportParseError(pXMLDom, 
         "Failed to load DOM from stocks.xml");
      goto clean;
   }

   // Query a single node.
   if (bstr) SysFreeString(bstr);
   bstr = SysAllocString(L"//stock[1]/*");
   HRCALL(pXMLDom->selectSingleNode(bstr, &pNode),
      "dom->selectSingleNode: ");
   if (!pNode) {
      ReportParseError(pXMLDom, "Calling selectSingleNode ");
   }
   else {
      dprintf("Result from selectSingleNode:\n");
      if (bstr) SysFreeString(bstr);
      HRCALL(pNode->get_nodeName(&bstr)," get_nodeName ");
      dprintf("Node, <%S>:\n", bstr);
      if (bstr) SysFreeString(bstr);
      HRCALL(pNode->get_xml(&bstr), "get_xml: ");
      dprintf("\t%S\n\n", bstr);
   }

   // Query a node-set.
   if (bstr) SysFreeString(bstr);
   bstr = SysAllocString(L"//stock[1]/*");
   HRCALL(pXMLDom->selectNodes(bstr, &pNodes), "selectNodes ");
   if (!pNodes) {
      ReportParseError(pXMLDom, "Error while calling selectNodes ");
   }
   else {
      dprintf("Results from selectNodes:\n");
      HRCALL(pNodes->get_length(&length), "get_length: ");
      for (long i=0; i<length; i++) {
         if (pNode) pNode->Release();
         HRCALL(pNodes->get_item(i, &pNode), "get_item: ");
         if (bstr) SysFreeString(bstr);
         HRCALL(pNode->get_nodeName(&bstr), "get_nodeName: ");
         dprintf("Node (%d), <%S>:\n",i, bstr);
         SysFreeString(bstr);
         HRCALL(pNode->get_xml(&bstr), "get_xml: ");
         dprintf("\t%S\n", bstr);
      }
   }

clean:
   if (bstr) SysFreeString(bstr);
   if (&var) VariantClear(&var);
   if (pXMLDom) pXMLDom->Release();
   if (pNodes) pNodes->Release();
   if (pNode) pNode->Release();

   CoUninitialize();
   return 0;
}