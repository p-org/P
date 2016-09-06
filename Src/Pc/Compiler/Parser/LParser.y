%namespace Microsoft.Pc.Parser
%using Microsoft.Pc.Domains;

%visibility internal
%YYSTYPE LexValue
%partial
%importtokens = tokensTokens.dat
%tokentype PTokens
%parsertype LParser

%%

DummyLinkerRule
	: {}
	;