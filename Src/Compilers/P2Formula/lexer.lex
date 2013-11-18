%namespace PParser
%visibility internal

%scannertype	PScanner
%scanbasetype	PScanBase
%tokentype		PTokens

%x COMMENT
%x QUOTE
%s UNQUOTE

%{
        internal int QuotingDepth
        {
            get;
            set;
        }

		internal PParser parser {
			set;
			get;
		}

		override public void yyerror(string msg, params object[] args) {
            parser.error(new DSLLoc(yylloc), string.Format(msg, args));
		}

        internal void LoadYylval()
        {
            // Trigger lazy evaluation of yytext
            int dummy = yytext.Length;

            yylval.s = tokTxt;
            yylloc = new QUT.Gppg.LexLocation(tokLin, tokCol, tokELin, tokECol);
        }
%}

CmntStart       \/\*
CmntEnd         \*\/
CmntStartAlt    \/\/
LF              [\n\r]
NonLF           [^\n\r]*

White0          [ \t\r\f\v]
White           {White0}|\n
NonQCntrChars   [^`$\n\r]*
NonSMCntrChars  [^\'\"\n\r]*

%%

{CmntStartAlt}{NonLF}{LF}                  { return (int)PTokens.LEX_COMMENT; }
{CmntStartAlt}{NonLF}                      { return (int)PTokens.LEX_COMMENT; }
{CmntStart}                                { BEGIN(COMMENT); return (int)PTokens.LEX_COMMENT; }
<COMMENT>{CmntEnd}                         { if (QuotingDepth == 0) BEGIN(INITIAL); else BEGIN(UNQUOTE); return (int)PTokens.LEX_COMMENT; }
<COMMENT>[.]*{LF}                          { return (int)PTokens.LEX_COMMENT; }
<COMMENT>[.]*                              { return (int)PTokens.LEX_COMMENT; }

"while"									   { return (int)PTokens.WHILE;  }
"if"									   { return (int)PTokens.IF;  }
"else"									   { return (int)PTokens.ELSE;  }
"return"								   { return (int)PTokens.RETURN;  }
"new"									   { return (int)PTokens.NEW;  }
"this"									   { return (int)PTokens.THIS;  }
"null"									   { return (int)PTokens.NULL;  }
"payload"								   { return (int)PTokens.PAYLOAD;  }
"arg"									   { return (int)PTokens.ARG;  }
"trigger"								   { return (int)PTokens.TRIGGER;  }
"leave"									   { return (int)PTokens.LEAVE;  }
"true"									   { return (int)PTokens.TRUE;  }
"false"									   { return (int)PTokens.FALSE;  }
"sizeof"								   { return (int)PTokens.SIZEOF; }
"keys"									   { return (int)PTokens.KEYS; }
"fair"									   { return (int)PTokens.FAIR; }

"assert"								   { return (int)PTokens.ASSERT;  }
"send"									   { return (int)PTokens.SEND;  }
"call"									   { return (int)PTokens.SCALL;  }
"raise"								       { return (int)PTokens.RAISE;  }
"delete"								   { return (int)PTokens.DELETE;  }

"int"									   { return (int)PTokens.T_INT;  }
"bool"									   { return (int)PTokens.T_BOOL;  }
"any"									   { return (int)PTokens.T_ANY;  }
"eid"							           { return (int)PTokens.T_EVENTID;  }
"id"									   { return (int)PTokens.T_MACHINEID;  }
"mid"									   { return (int)PTokens.T_MODELMACHINEID;  }
"seq"									   { return (int)PTokens.T_SEQ;  }
"map"									   { return (int)PTokens.T_MAP; }

"main"									   { return (int)PTokens.MAIN;  }
"event"									   { return (int)PTokens.EVENT;  }
"machine"								   { return (int)PTokens.MACHINE;  }
"assume"								   { return (int)PTokens.ASSUME;  }
"ghost"									   { return (int)PTokens.GHOST;  }
"default"								   { return (int)PTokens.DEFAULT; }

"var"									   { return (int)PTokens.VAR;  }
"start"									   { return (int)PTokens.START;  }
"stable"								   { return (int)PTokens.STABLE;  }
"model"									   { return (int)PTokens.MODEL;  }
"fun"									   { return (int)PTokens.FUN;  }
"action"								   { return (int)PTokens.ACTION;  }
"state"									   { return (int)PTokens.STATE;  }
"submachine"							   { return (int)PTokens.SUBMACHINE;  }
"maxqueue"								   { return (int)PTokens.MAXQUEUE; }

"entry"									   { return (int)PTokens.ENTRY;  }
"exit"									   { return (int)PTokens.EXIT;  }
"defer"									   { return (int)PTokens.DEFER;  }
"ignore"								   { return (int)PTokens.IGNORE;  }
"goto"									   { return (int)PTokens.GOTO;  }
"push"									   { return (int)PTokens.PUSH;  }
"on"									   { return (int)PTokens.ON;  }
"do"									   { return (int)PTokens.DO;  }

"in"									   { return (int)PTokens.IN; }

[A-Za-z_][A-Za-z_0-9]*  			       { return (int)PTokens.ID;   }
[0-9]+									   { return (int)PTokens.INT;  }
[0-9]+[\.][0-9]+						   { return (int)PTokens.REAL; }

[\.]                                       { return (int)PTokens.DOT;     }
[:]                                        { return (int)PTokens.COLON;   }

[,]                                        { return (int)PTokens.COMMA;     }
[;]                                        { return (int)PTokens.SEMICOLON; }

"=="                                       { return (int)PTokens.EQ; }
"="                                        { return (int)PTokens.ASSIGN; }
"!="                                       { return (int)PTokens.NE; }
"<="                                       { return (int)PTokens.LE; }
">="                                       { return (int)PTokens.GE; }
[<]                                        { return (int)PTokens.LT; }
[>]                                        { return (int)PTokens.GT; }

[+]                                        { return (int)PTokens.PLUS;  }
[\-]                                       { return (int)PTokens.MINUS; }
[*]                                        { return (int)PTokens.MUL;   }
[\/]                                       { return (int)PTokens.DIV;   }

[!]										   { return (int)PTokens.LNOT;   }
"&&"									   { return (int)PTokens.LAND;   }
"||"									   { return (int)PTokens.LOR;   }

[{]                                        { return (int)PTokens.LCBRACE; }
[}]                                        { return (int)PTokens.RCBRACE; }
[\[]                                       { return (int)PTokens.LBRACKET; }
[\]]                                       { return (int)PTokens.RBRACKET; }
[(]                                        { return (int)PTokens.LPAREN;  }
[)]                                        { return (int)PTokens.RPAREN;  }

%{
    LoadYylval();
%}

%%
