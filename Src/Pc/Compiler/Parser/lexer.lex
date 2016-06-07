%namespace Microsoft.Pc.Parser
%visibility internal
%using Microsoft.Formula.API;

%x COMMENT

%{
		 private Dictionary<string, int> keywords = null;

		 internal ProgramName SourceProgram
		 {
			get;
			set;
		 }

		 internal List<Flag> Flags
		 {
			get;
			set;
		 }

		 internal bool Failed
		 {
			 get;
			 set;
		 }

         override public void yyerror(string message, params object[] args)
         {
		   var errFlag = new Flag(
							SeverityKind.Error,
							new Span(yylloc.StartLine, yylloc.StartColumn, yylloc.EndLine, yylloc.StartColumn + yyleng),
							Constants.BadSyntax.ToString(string.Format(message, args)),
							Constants.BadSyntax.Code,
							SourceProgram);
		   Failed = true;
		   Flags.Add(errFlag);
         }

		 private void MkKeywords()
		 {   
		     if (keywords != null)
			 {
				return;
			 }

			 keywords = new Dictionary<string, int>(64);

			 keywords.Add("while", (int)Tokens.WHILE);
			 keywords.Add("if", (int)Tokens.IF);
			 keywords.Add("else", (int)Tokens.ELSE);
			 keywords.Add("return", (int)Tokens.RETURN);
			 keywords.Add("new", (int)Tokens.NEW);
			 keywords.Add("this", (int)Tokens.THIS);
			 keywords.Add("null", (int)Tokens.NULL);
			 keywords.Add("pop", (int)Tokens.POP);
			 keywords.Add("true", (int)Tokens.TRUE);
			 keywords.Add("false", (int)Tokens.FALSE);
			 keywords.Add("ref", (int)Tokens.REF);
			 keywords.Add("xfer", (int)Tokens.XFER);
			 keywords.Add("sizeof", (int)Tokens.SIZEOF);
			 keywords.Add("keys", (int)Tokens.KEYS);
			 keywords.Add("values", (int)Tokens.VALUES);

			 keywords.Add("assert", (int)Tokens.ASSERT);
			 keywords.Add("print", (int)Tokens.PRINT);
			 keywords.Add("send", (int)Tokens.SEND);
			 keywords.Add("monitor", (int)Tokens.MONITOR);
			 keywords.Add("spec", (int)Tokens.SPEC);
			 keywords.Add("monitors", (int)Tokens.MONITORS);
			 keywords.Add("raise", (int)Tokens.RAISE);
			 keywords.Add("halt", (int)Tokens.HALT);

			 keywords.Add("int", (int)Tokens.INT);
			 keywords.Add("bool", (int)Tokens.BOOL);
			 keywords.Add("any", (int)Tokens.ANY);
			 keywords.Add("seq", (int)Tokens.SEQ);
			 keywords.Add("map", (int)Tokens.MAP);

			 keywords.Add("type", (int)Tokens.TYPE);
			 keywords.Add("include", (int)Tokens.INCLUDE);
			 keywords.Add("main", (int)Tokens.MAIN);
			 keywords.Add("event", (int)Tokens.EVENT);
			 keywords.Add("machine", (int)Tokens.MACHINE);
			 keywords.Add("assume", (int)Tokens.ASSUME);
			 keywords.Add("default", (int)Tokens.DEFAULT);

			 keywords.Add("var", (int)Tokens.VAR);
			 keywords.Add("start", (int)Tokens.START);
			 keywords.Add("hot", (int)Tokens.HOT);
			 keywords.Add("cold", (int)Tokens.COLD);
			 keywords.Add("model", (int)Tokens.MODEL);
			 keywords.Add("fun", (int)Tokens.FUN);
			 keywords.Add("action", (int)Tokens.ACTION);
			 keywords.Add("state", (int)Tokens.STATE);
			 keywords.Add("group", (int)Tokens.GROUP);

			 keywords.Add("entry", (int)Tokens.ENTRY);
			 keywords.Add("exit", (int)Tokens.EXIT);
			 keywords.Add("defer", (int)Tokens.DEFER);
			 keywords.Add("ignore", (int)Tokens.IGNORE);
			 keywords.Add("goto", (int)Tokens.GOTO);
			 keywords.Add("push", (int)Tokens.PUSH);
			 keywords.Add("on", (int)Tokens.ON);
			 keywords.Add("do", (int)Tokens.DO);
			 keywords.Add("with", (int)Tokens.WITH);

			 keywords.Add("receive", (int)Tokens.RECEIVE);
			 keywords.Add("case", (int)Tokens.CASE);

			 keywords.Add("in", (int)Tokens.IN);
			 keywords.Add("as", (int)Tokens.AS);
		 }

         int GetIdToken(string txt)
         {
		    MkKeywords();

		    int tokId;
			if (keywords.TryGetValue(txt, out tokId))
			{
			   return tokId;
			}
			else 
			{
			   return (int)Tokens.ID;
			}
		}

       internal void LoadYylval()
       {
            // Trigger lazy evaluation of yytext
            int dummy = yytext.Length;
            
            yylval.str = tokTxt;
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
Id              [A-Za-z_]([A-Za-z_0-9]*)

%%

{CmntStartAlt}{NonLF}{LF}                  { return (int)Tokens.LEX_COMMENT; }
{CmntStartAlt}{NonLF}                      { return (int)Tokens.LEX_COMMENT; }
{CmntStart}                                { BEGIN(COMMENT); return (int)Tokens.LEX_COMMENT; }
<COMMENT>{CmntEnd}                         { BEGIN(INITIAL); return (int)Tokens.LEX_COMMENT; }
<COMMENT>[.]*{LF}                          { return (int)Tokens.LEX_COMMENT; }
<COMMENT>[.]*                              { return (int)Tokens.LEX_COMMENT; }

[A-Za-z_][A-Za-z_0-9]*  			       { return GetIdToken(yytext);  }
[0-9]+									   { return (int)Tokens.INT;     }
[\"][^\"\n\r]*[\"]						   { return (int)Tokens.STR; }

[\.]                                       { return (int)Tokens.DOT;     }
[:]                                        { return (int)Tokens.COLON;   }

[,]                                        { return (int)Tokens.COMMA;     }
[;]                                        { return (int)Tokens.SEMICOLON; }

"=="                                       { return (int)Tokens.EQ;     }
"="                                        { return (int)Tokens.ASSIGN; }
"+="									   { return (int)Tokens.INSERT; }
"-="                                       { return (int)Tokens.REMOVE; }
"!="                                       { return (int)Tokens.NE;     }
"<="                                       { return (int)Tokens.LE;     }
">="                                       { return (int)Tokens.GE;     }
[<]                                        { return (int)Tokens.LT;     }
[>]                                        { return (int)Tokens.GT;     }

[+]                                        { return (int)Tokens.PLUS;  }
[\-]                                       { return (int)Tokens.MINUS; }
[*]                                        { return (int)Tokens.MUL;   }
[\/]                                       { return (int)Tokens.DIV;   }

[!]										   { return (int)Tokens.LNOT;   }
"&&"									   { return (int)Tokens.LAND;   }
"||"									   { return (int)Tokens.LOR;    }

"$"									       { return (int)Tokens.NONDET; }
"$$"    							       { return (int)Tokens.FAIRNONDET; }

[{]                                        { return (int)Tokens.LCBRACE;  }
[}]                                        { return (int)Tokens.RCBRACE;  }
[\[]                                       { return (int)Tokens.LBRACKET; }
[\]]                                       { return (int)Tokens.RBRACKET; }
[(]                                        { return (int)Tokens.LPAREN;   }
[)]                                        { return (int)Tokens.RPAREN;   }

%{
    LoadYylval();
%}

%%
