%namespace Microsoft.Pc.Parser
%using Microsoft.Pc.Domains;
%using Microsoft.Pc.Tokens;

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