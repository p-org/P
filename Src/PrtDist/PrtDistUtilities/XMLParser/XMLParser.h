#ifdef __cplusplus
extern "C" {
#endif

typedef	struct _XMLNODE {
	char NodeType[100];
	char NodeName[100];
	char NodeValue[100];
	char NodeParent[100];
} XMLNODE;

XMLNODE** XMLDOMParseNodes(const char*);

#ifdef __cplusplus
}
#endif