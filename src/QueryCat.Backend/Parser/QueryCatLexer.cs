//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.11.1
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from ../QueryCat.Backend/Parser/QueryCatLexer.g4 by ANTLR 4.11.1

// Unreachable code detected
#pragma warning disable 0162
// The variable '...' is assigned but its value is never used
#pragma warning disable 0219
// Missing XML comment for publicly visible type or member '...'
#pragma warning disable 1591
// Ambiguous reference in cref attribute
#pragma warning disable 419

namespace QueryCat.Backend.Parser {
 #pragma warning disable 3021 
using System;
using System.IO;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;
using DFA = Antlr4.Runtime.Dfa.DFA;

[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.11.1")]
[System.CLSCompliant(false)]
public partial class QueryCatLexer : Lexer {
	protected static DFA[] decisionToDFA;
	protected static PredictionContextCache sharedContextCache = new PredictionContextCache();
	public const int
		LEFT_PAREN=1, RIGHT_PAREN=2, ASSIGN=3, ASSOCIATION=4, COLON=5, COMMA=6, 
		PERIOD=7, ELLIPSIS=8, SEMICOLON=9, QUESTION=10, LEFT_BRACKET=11, RIGHT_BRACKET=12, 
		LEFT_RIGHT_BRACKET=13, PIPE=14, PLUS=15, MINUS=16, STAR=17, DIV=18, MOD=19, 
		EQUALS=20, NOT_EQUALS=21, GREATER=22, GREATER_OR_EQUALS=23, LESS=24, LESS_OR_EQUALS=25, 
		CONCAT=26, LESS_LESS=27, GREATER_GREATER=28, TYPECAST=29, INTEGER=30, 
		STRING=31, FLOAT=32, TIMESTAMP=33, BOOLEAN=34, NUMERIC=35, OBJECT=36, 
		ANY=37, AND=38, AS=39, BY=40, CAST=41, DEFAULT=42, ELSE=43, END=44, EXISTS=45, 
		FALSE=46, FROM=47, IF=48, IN=49, IS=50, LIKE=51, NOT=52, NULL=53, ON=54, 
		ONLY=55, OR=56, SOME=57, THEN=58, TO=59, TRUE=60, VOID=61, TRIM=62, LEADING=63, 
		TRAILING=64, BOTH=65, CURRENT_DATE=66, CURRENT_TIMESTAMP=67, INTERVAL=68, 
		YEAR=69, MONTH=70, DAY=71, HOUR=72, MINUTE=73, SECOND=74, MILLISECOND=75, 
		CASE=76, COALESCE=77, EXTRACT=78, POSITION=79, WHEN=80, ECHO=81, ALL=82, 
		ASC=83, BETWEEN=84, DESC=85, DISTINCT=86, EXCEPT=87, FETCH=88, FIRST=89, 
		FORMAT=90, FULL=91, GROUP=92, HAVING=93, INNER=94, INTERSECT=95, INTO=96, 
		JOIN=97, LAST=98, LEFT=99, LIMIT=100, NEXT=101, NULLS=102, OFFSET=103, 
		ORDER=104, OUTER=105, OVER=106, PARTITION=107, RECURSIVE=108, RIGHT=109, 
		ROW=110, ROWS=111, SELECT=112, TOP=113, UNION=114, WHERE=115, WINDOW=116, 
		WITH=117, DECLARE=118, SET=119, TYPE=120, IDENTIFIER=121, INTEGER_LITERAL=122, 
		FLOAT_LITERAL=123, NUMERIC_LITERAL=124, STRING_LITERAL=125, BOOLEAN_LITERAL=126, 
		SINGLE_LINE_COMMENT=127, MULTILINE_COMMENT=128, SPACES=129;
	public static string[] channelNames = {
		"DEFAULT_TOKEN_CHANNEL", "HIDDEN"
	};

	public static string[] modeNames = {
		"DEFAULT_MODE"
	};

	public static readonly string[] ruleNames = {
		"LEFT_PAREN", "RIGHT_PAREN", "ASSIGN", "ASSOCIATION", "COLON", "COMMA", 
		"PERIOD", "ELLIPSIS", "SEMICOLON", "QUESTION", "LEFT_BRACKET", "RIGHT_BRACKET", 
		"LEFT_RIGHT_BRACKET", "PIPE", "PLUS", "MINUS", "STAR", "DIV", "MOD", "EQUALS", 
		"NOT_EQUALS", "GREATER", "GREATER_OR_EQUALS", "LESS", "LESS_OR_EQUALS", 
		"CONCAT", "LESS_LESS", "GREATER_GREATER", "TYPECAST", "INTEGER", "STRING", 
		"FLOAT", "TIMESTAMP", "BOOLEAN", "NUMERIC", "OBJECT", "ANY", "AND", "AS", 
		"BY", "CAST", "DEFAULT", "ELSE", "END", "EXISTS", "FALSE", "FROM", "IF", 
		"IN", "IS", "LIKE", "NOT", "NULL", "ON", "ONLY", "OR", "SOME", "THEN", 
		"TO", "TRUE", "VOID", "TRIM", "LEADING", "TRAILING", "BOTH", "CURRENT_DATE", 
		"CURRENT_TIMESTAMP", "INTERVAL", "YEAR", "MONTH", "DAY", "HOUR", "MINUTE", 
		"SECOND", "MILLISECOND", "CASE", "COALESCE", "EXTRACT", "POSITION", "WHEN", 
		"ECHO", "ALL", "ASC", "BETWEEN", "DESC", "DISTINCT", "EXCEPT", "FETCH", 
		"FIRST", "FORMAT", "FULL", "GROUP", "HAVING", "INNER", "INTERSECT", "INTO", 
		"JOIN", "LAST", "LEFT", "LIMIT", "NEXT", "NULLS", "OFFSET", "ORDER", "OUTER", 
		"OVER", "PARTITION", "RECURSIVE", "RIGHT", "ROW", "ROWS", "SELECT", "TOP", 
		"UNION", "WHERE", "WINDOW", "WITH", "DECLARE", "SET", "TYPE", "NameChar", 
		"NameStartChar", "IDENTIFIER", "HEX_DIGIT", "DIGIT", "INTEGER_LITERAL", 
		"FLOAT_LITERAL", "NUMERIC_LITERAL", "STRING_LITERAL", "BOOLEAN_LITERAL", 
		"SINGLE_LINE_COMMENT", "MULTILINE_COMMENT", "SPACES"
	};


	public QueryCatLexer(ICharStream input)
	: this(input, Console.Out, Console.Error) { }

	public QueryCatLexer(ICharStream input, TextWriter output, TextWriter errorOutput)
	: base(input, output, errorOutput)
	{
		Interpreter = new LexerATNSimulator(this, _ATN, decisionToDFA, sharedContextCache);
	}

	private static readonly string[] _LiteralNames = {
		null, "'('", "')'", "':='", "'=>'", "':'", "','", "'.'", "'...'", "';'", 
		"'?'", "'['", "']'", "'[]'", "'&>'", "'+'", "'-'", "'*'", "'/'", "'%'", 
		"'='", "'<>'", "'>'", "'>='", "'<'", "'<='", "'||'", "'<<'", "'>>'", "'::'", 
		"'INTEGER'", "'STRING'", "'FLOAT'", "'TIMESTAMP'", "'BOOLEAN'", "'NUMERIC'", 
		"'OBJECT'", "'ANY'", "'AND'", "'AS'", "'BY'", "'CAST'", "'DEFAULT'", "'ELSE'", 
		"'END'", "'EXISTS'", "'FALSE'", "'FROM'", "'IF'", "'IN'", "'IS'", "'LIKE'", 
		"'NOT'", "'NULL'", "'ON'", "'ONLY'", "'OR'", "'SOME'", "'THEN'", "'TO'", 
		"'TRUE'", "'VOID'", "'TRIM'", "'LEADING'", "'TRAILING'", "'BOTH'", "'CURRENT_DATE'", 
		"'CURRENT_TIMESTAMP'", "'INTERVAL'", "'YEAR'", "'MONTH'", "'DAY'", "'HOUR'", 
		"'MINUTE'", "'SECOND'", "'MILLISECOND'", "'CASE'", "'COALESCE'", "'EXTRACT'", 
		"'POSITION'", "'WHEN'", "'ECHO'", "'ALL'", "'ASC'", "'BETWEEN'", "'DESC'", 
		"'DISTINCT'", "'EXCEPT'", "'FETCH'", "'FIRST'", "'FORMAT'", "'FULL'", 
		"'GROUP'", "'HAVING'", "'INNER'", "'INTERSECT'", "'INTO'", "'JOIN'", "'LAST'", 
		"'LEFT'", "'LIMIT'", "'NEXT'", "'NULLS'", "'OFFSET'", "'ORDER'", "'OUTER'", 
		"'OVER'", "'PARTITION'", "'RECURSIVE'", "'RIGHT'", "'ROW'", "'ROWS'", 
		"'SELECT'", "'TOP'", "'UNION'", "'WHERE'", "'WINDOW'", "'WITH'", "'DECLARE'", 
		"'SET'"
	};
	private static readonly string[] _SymbolicNames = {
		null, "LEFT_PAREN", "RIGHT_PAREN", "ASSIGN", "ASSOCIATION", "COLON", "COMMA", 
		"PERIOD", "ELLIPSIS", "SEMICOLON", "QUESTION", "LEFT_BRACKET", "RIGHT_BRACKET", 
		"LEFT_RIGHT_BRACKET", "PIPE", "PLUS", "MINUS", "STAR", "DIV", "MOD", "EQUALS", 
		"NOT_EQUALS", "GREATER", "GREATER_OR_EQUALS", "LESS", "LESS_OR_EQUALS", 
		"CONCAT", "LESS_LESS", "GREATER_GREATER", "TYPECAST", "INTEGER", "STRING", 
		"FLOAT", "TIMESTAMP", "BOOLEAN", "NUMERIC", "OBJECT", "ANY", "AND", "AS", 
		"BY", "CAST", "DEFAULT", "ELSE", "END", "EXISTS", "FALSE", "FROM", "IF", 
		"IN", "IS", "LIKE", "NOT", "NULL", "ON", "ONLY", "OR", "SOME", "THEN", 
		"TO", "TRUE", "VOID", "TRIM", "LEADING", "TRAILING", "BOTH", "CURRENT_DATE", 
		"CURRENT_TIMESTAMP", "INTERVAL", "YEAR", "MONTH", "DAY", "HOUR", "MINUTE", 
		"SECOND", "MILLISECOND", "CASE", "COALESCE", "EXTRACT", "POSITION", "WHEN", 
		"ECHO", "ALL", "ASC", "BETWEEN", "DESC", "DISTINCT", "EXCEPT", "FETCH", 
		"FIRST", "FORMAT", "FULL", "GROUP", "HAVING", "INNER", "INTERSECT", "INTO", 
		"JOIN", "LAST", "LEFT", "LIMIT", "NEXT", "NULLS", "OFFSET", "ORDER", "OUTER", 
		"OVER", "PARTITION", "RECURSIVE", "RIGHT", "ROW", "ROWS", "SELECT", "TOP", 
		"UNION", "WHERE", "WINDOW", "WITH", "DECLARE", "SET", "TYPE", "IDENTIFIER", 
		"INTEGER_LITERAL", "FLOAT_LITERAL", "NUMERIC_LITERAL", "STRING_LITERAL", 
		"BOOLEAN_LITERAL", "SINGLE_LINE_COMMENT", "MULTILINE_COMMENT", "SPACES"
	};
	public static readonly IVocabulary DefaultVocabulary = new Vocabulary(_LiteralNames, _SymbolicNames);

	[NotNull]
	public override IVocabulary Vocabulary
	{
		get
		{
			return DefaultVocabulary;
		}
	}

	public override string GrammarFileName { get { return "QueryCatLexer.g4"; } }

	public override string[] RuleNames { get { return ruleNames; } }

	public override string[] ChannelNames { get { return channelNames; } }

	public override string[] ModeNames { get { return modeNames; } }

	public override int[] SerializedAtn { get { return _serializedATN; } }

	static QueryCatLexer() {
		decisionToDFA = new DFA[_ATN.NumberOfDecisions];
		for (int i = 0; i < _ATN.NumberOfDecisions; i++) {
			decisionToDFA[i] = new DFA(_ATN.GetDecisionState(i), i);
		}
	}
	private static int[] _serializedATN = {
		4,0,129,1045,6,-1,2,0,7,0,2,1,7,1,2,2,7,2,2,3,7,3,2,4,7,4,2,5,7,5,2,6,
		7,6,2,7,7,7,2,8,7,8,2,9,7,9,2,10,7,10,2,11,7,11,2,12,7,12,2,13,7,13,2,
		14,7,14,2,15,7,15,2,16,7,16,2,17,7,17,2,18,7,18,2,19,7,19,2,20,7,20,2,
		21,7,21,2,22,7,22,2,23,7,23,2,24,7,24,2,25,7,25,2,26,7,26,2,27,7,27,2,
		28,7,28,2,29,7,29,2,30,7,30,2,31,7,31,2,32,7,32,2,33,7,33,2,34,7,34,2,
		35,7,35,2,36,7,36,2,37,7,37,2,38,7,38,2,39,7,39,2,40,7,40,2,41,7,41,2,
		42,7,42,2,43,7,43,2,44,7,44,2,45,7,45,2,46,7,46,2,47,7,47,2,48,7,48,2,
		49,7,49,2,50,7,50,2,51,7,51,2,52,7,52,2,53,7,53,2,54,7,54,2,55,7,55,2,
		56,7,56,2,57,7,57,2,58,7,58,2,59,7,59,2,60,7,60,2,61,7,61,2,62,7,62,2,
		63,7,63,2,64,7,64,2,65,7,65,2,66,7,66,2,67,7,67,2,68,7,68,2,69,7,69,2,
		70,7,70,2,71,7,71,2,72,7,72,2,73,7,73,2,74,7,74,2,75,7,75,2,76,7,76,2,
		77,7,77,2,78,7,78,2,79,7,79,2,80,7,80,2,81,7,81,2,82,7,82,2,83,7,83,2,
		84,7,84,2,85,7,85,2,86,7,86,2,87,7,87,2,88,7,88,2,89,7,89,2,90,7,90,2,
		91,7,91,2,92,7,92,2,93,7,93,2,94,7,94,2,95,7,95,2,96,7,96,2,97,7,97,2,
		98,7,98,2,99,7,99,2,100,7,100,2,101,7,101,2,102,7,102,2,103,7,103,2,104,
		7,104,2,105,7,105,2,106,7,106,2,107,7,107,2,108,7,108,2,109,7,109,2,110,
		7,110,2,111,7,111,2,112,7,112,2,113,7,113,2,114,7,114,2,115,7,115,2,116,
		7,116,2,117,7,117,2,118,7,118,2,119,7,119,2,120,7,120,2,121,7,121,2,122,
		7,122,2,123,7,123,2,124,7,124,2,125,7,125,2,126,7,126,2,127,7,127,2,128,
		7,128,2,129,7,129,2,130,7,130,2,131,7,131,2,132,7,132,1,0,1,0,1,1,1,1,
		1,2,1,2,1,2,1,3,1,3,1,3,1,4,1,4,1,5,1,5,1,6,1,6,1,7,1,7,1,7,1,7,1,8,1,
		8,1,9,1,9,1,10,1,10,1,11,1,11,1,12,1,12,1,12,1,13,1,13,1,13,1,14,1,14,
		1,15,1,15,1,16,1,16,1,17,1,17,1,18,1,18,1,19,1,19,1,20,1,20,1,20,1,21,
		1,21,1,22,1,22,1,22,1,23,1,23,1,24,1,24,1,24,1,25,1,25,1,25,1,26,1,26,
		1,26,1,27,1,27,1,27,1,28,1,28,1,28,1,29,1,29,1,29,1,29,1,29,1,29,1,29,
		1,29,1,30,1,30,1,30,1,30,1,30,1,30,1,30,1,31,1,31,1,31,1,31,1,31,1,31,
		1,32,1,32,1,32,1,32,1,32,1,32,1,32,1,32,1,32,1,32,1,33,1,33,1,33,1,33,
		1,33,1,33,1,33,1,33,1,34,1,34,1,34,1,34,1,34,1,34,1,34,1,34,1,35,1,35,
		1,35,1,35,1,35,1,35,1,35,1,36,1,36,1,36,1,36,1,37,1,37,1,37,1,37,1,38,
		1,38,1,38,1,39,1,39,1,39,1,40,1,40,1,40,1,40,1,40,1,41,1,41,1,41,1,41,
		1,41,1,41,1,41,1,41,1,42,1,42,1,42,1,42,1,42,1,43,1,43,1,43,1,43,1,44,
		1,44,1,44,1,44,1,44,1,44,1,44,1,45,1,45,1,45,1,45,1,45,1,45,1,46,1,46,
		1,46,1,46,1,46,1,47,1,47,1,47,1,48,1,48,1,48,1,49,1,49,1,49,1,50,1,50,
		1,50,1,50,1,50,1,51,1,51,1,51,1,51,1,52,1,52,1,52,1,52,1,52,1,53,1,53,
		1,53,1,54,1,54,1,54,1,54,1,54,1,55,1,55,1,55,1,56,1,56,1,56,1,56,1,56,
		1,57,1,57,1,57,1,57,1,57,1,58,1,58,1,58,1,59,1,59,1,59,1,59,1,59,1,60,
		1,60,1,60,1,60,1,60,1,61,1,61,1,61,1,61,1,61,1,62,1,62,1,62,1,62,1,62,
		1,62,1,62,1,62,1,63,1,63,1,63,1,63,1,63,1,63,1,63,1,63,1,63,1,64,1,64,
		1,64,1,64,1,64,1,65,1,65,1,65,1,65,1,65,1,65,1,65,1,65,1,65,1,65,1,65,
		1,65,1,65,1,66,1,66,1,66,1,66,1,66,1,66,1,66,1,66,1,66,1,66,1,66,1,66,
		1,66,1,66,1,66,1,66,1,66,1,66,1,67,1,67,1,67,1,67,1,67,1,67,1,67,1,67,
		1,67,1,68,1,68,1,68,1,68,1,68,1,69,1,69,1,69,1,69,1,69,1,69,1,70,1,70,
		1,70,1,70,1,71,1,71,1,71,1,71,1,71,1,72,1,72,1,72,1,72,1,72,1,72,1,72,
		1,73,1,73,1,73,1,73,1,73,1,73,1,73,1,74,1,74,1,74,1,74,1,74,1,74,1,74,
		1,74,1,74,1,74,1,74,1,74,1,75,1,75,1,75,1,75,1,75,1,76,1,76,1,76,1,76,
		1,76,1,76,1,76,1,76,1,76,1,77,1,77,1,77,1,77,1,77,1,77,1,77,1,77,1,78,
		1,78,1,78,1,78,1,78,1,78,1,78,1,78,1,78,1,79,1,79,1,79,1,79,1,79,1,80,
		1,80,1,80,1,80,1,80,1,81,1,81,1,81,1,81,1,82,1,82,1,82,1,82,1,83,1,83,
		1,83,1,83,1,83,1,83,1,83,1,83,1,84,1,84,1,84,1,84,1,84,1,85,1,85,1,85,
		1,85,1,85,1,85,1,85,1,85,1,85,1,86,1,86,1,86,1,86,1,86,1,86,1,86,1,87,
		1,87,1,87,1,87,1,87,1,87,1,88,1,88,1,88,1,88,1,88,1,88,1,89,1,89,1,89,
		1,89,1,89,1,89,1,89,1,90,1,90,1,90,1,90,1,90,1,91,1,91,1,91,1,91,1,91,
		1,91,1,92,1,92,1,92,1,92,1,92,1,92,1,92,1,93,1,93,1,93,1,93,1,93,1,93,
		1,94,1,94,1,94,1,94,1,94,1,94,1,94,1,94,1,94,1,94,1,95,1,95,1,95,1,95,
		1,95,1,96,1,96,1,96,1,96,1,96,1,97,1,97,1,97,1,97,1,97,1,98,1,98,1,98,
		1,98,1,98,1,99,1,99,1,99,1,99,1,99,1,99,1,100,1,100,1,100,1,100,1,100,
		1,101,1,101,1,101,1,101,1,101,1,101,1,102,1,102,1,102,1,102,1,102,1,102,
		1,102,1,103,1,103,1,103,1,103,1,103,1,103,1,104,1,104,1,104,1,104,1,104,
		1,104,1,105,1,105,1,105,1,105,1,105,1,106,1,106,1,106,1,106,1,106,1,106,
		1,106,1,106,1,106,1,106,1,107,1,107,1,107,1,107,1,107,1,107,1,107,1,107,
		1,107,1,107,1,108,1,108,1,108,1,108,1,108,1,108,1,109,1,109,1,109,1,109,
		1,110,1,110,1,110,1,110,1,110,1,111,1,111,1,111,1,111,1,111,1,111,1,111,
		1,112,1,112,1,112,1,112,1,113,1,113,1,113,1,113,1,113,1,113,1,114,1,114,
		1,114,1,114,1,114,1,114,1,115,1,115,1,115,1,115,1,115,1,115,1,115,1,116,
		1,116,1,116,1,116,1,116,1,117,1,117,1,117,1,117,1,117,1,117,1,117,1,117,
		1,118,1,118,1,118,1,118,1,119,1,119,1,119,1,119,1,119,1,119,1,119,1,119,
		3,119,899,8,119,1,120,1,120,3,120,903,8,120,1,121,1,121,1,122,1,122,5,
		122,909,8,122,10,122,12,122,912,9,122,1,122,1,122,1,122,1,122,5,122,918,
		8,122,10,122,12,122,921,9,122,1,122,3,122,924,8,122,1,123,1,123,1,124,
		1,124,1,125,4,125,931,8,125,11,125,12,125,932,1,126,4,126,936,8,126,11,
		126,12,126,937,1,126,1,126,5,126,942,8,126,10,126,12,126,945,9,126,3,126,
		947,8,126,1,126,1,126,4,126,951,8,126,11,126,12,126,952,3,126,955,8,126,
		1,126,1,126,3,126,959,8,126,1,126,4,126,962,8,126,11,126,12,126,963,3,
		126,966,8,126,1,126,1,126,1,126,1,126,4,126,972,8,126,11,126,12,126,973,
		3,126,976,8,126,1,127,1,127,1,127,1,128,1,128,1,128,1,128,5,128,985,8,
		128,10,128,12,128,988,9,128,1,128,1,128,1,128,1,128,1,128,5,128,995,8,
		128,10,128,12,128,998,9,128,1,128,3,128,1001,8,128,1,129,1,129,3,129,1005,
		8,129,1,130,1,130,1,130,1,130,3,130,1011,8,130,1,130,5,130,1014,8,130,
		10,130,12,130,1017,9,130,1,130,3,130,1020,8,130,1,130,1,130,3,130,1024,
		8,130,1,130,1,130,1,131,1,131,1,131,1,131,5,131,1032,8,131,10,131,12,131,
		1035,9,131,1,131,1,131,1,131,1,131,1,131,1,132,1,132,1,132,1,132,1,1033,
		0,133,1,1,3,2,5,3,7,4,9,5,11,6,13,7,15,8,17,9,19,10,21,11,23,12,25,13,
		27,14,29,15,31,16,33,17,35,18,37,19,39,20,41,21,43,22,45,23,47,24,49,25,
		51,26,53,27,55,28,57,29,59,30,61,31,63,32,65,33,67,34,69,35,71,36,73,37,
		75,38,77,39,79,40,81,41,83,42,85,43,87,44,89,45,91,46,93,47,95,48,97,49,
		99,50,101,51,103,52,105,53,107,54,109,55,111,56,113,57,115,58,117,59,119,
		60,121,61,123,62,125,63,127,64,129,65,131,66,133,67,135,68,137,69,139,
		70,141,71,143,72,145,73,147,74,149,75,151,76,153,77,155,78,157,79,159,
		80,161,81,163,82,165,83,167,84,169,85,171,86,173,87,175,88,177,89,179,
		90,181,91,183,92,185,93,187,94,189,95,191,96,193,97,195,98,197,99,199,
		100,201,101,203,102,205,103,207,104,209,105,211,106,213,107,215,108,217,
		109,219,110,221,111,223,112,225,113,227,114,229,115,231,116,233,117,235,
		118,237,119,239,120,241,0,243,0,245,121,247,0,249,0,251,122,253,123,255,
		124,257,125,259,126,261,127,263,128,265,129,1,0,34,2,0,73,73,105,105,2,
		0,78,78,110,110,2,0,84,84,116,116,2,0,69,69,101,101,2,0,71,71,103,103,
		2,0,82,82,114,114,2,0,83,83,115,115,2,0,70,70,102,102,2,0,76,76,108,108,
		2,0,79,79,111,111,2,0,65,65,97,97,2,0,77,77,109,109,2,0,80,80,112,112,
		2,0,66,66,98,98,2,0,85,85,117,117,2,0,67,67,99,99,2,0,74,74,106,106,2,
		0,89,89,121,121,2,0,68,68,100,100,2,0,88,88,120,120,2,0,75,75,107,107,
		2,0,72,72,104,104,2,0,86,86,118,118,2,0,87,87,119,119,5,0,48,57,95,95,
		183,183,768,879,8255,8256,14,0,65,90,95,95,97,122,192,214,224,246,248,
		767,880,893,895,8191,8204,8205,8304,8591,11264,12271,12289,55295,63744,
		64975,65008,65533,1,0,93,93,3,0,48,57,65,70,97,102,1,0,48,57,2,0,43,43,
		45,45,1,0,39,39,1,0,34,34,2,0,10,10,13,13,3,0,9,11,13,13,32,32,1074,0,
		1,1,0,0,0,0,3,1,0,0,0,0,5,1,0,0,0,0,7,1,0,0,0,0,9,1,0,0,0,0,11,1,0,0,0,
		0,13,1,0,0,0,0,15,1,0,0,0,0,17,1,0,0,0,0,19,1,0,0,0,0,21,1,0,0,0,0,23,
		1,0,0,0,0,25,1,0,0,0,0,27,1,0,0,0,0,29,1,0,0,0,0,31,1,0,0,0,0,33,1,0,0,
		0,0,35,1,0,0,0,0,37,1,0,0,0,0,39,1,0,0,0,0,41,1,0,0,0,0,43,1,0,0,0,0,45,
		1,0,0,0,0,47,1,0,0,0,0,49,1,0,0,0,0,51,1,0,0,0,0,53,1,0,0,0,0,55,1,0,0,
		0,0,57,1,0,0,0,0,59,1,0,0,0,0,61,1,0,0,0,0,63,1,0,0,0,0,65,1,0,0,0,0,67,
		1,0,0,0,0,69,1,0,0,0,0,71,1,0,0,0,0,73,1,0,0,0,0,75,1,0,0,0,0,77,1,0,0,
		0,0,79,1,0,0,0,0,81,1,0,0,0,0,83,1,0,0,0,0,85,1,0,0,0,0,87,1,0,0,0,0,89,
		1,0,0,0,0,91,1,0,0,0,0,93,1,0,0,0,0,95,1,0,0,0,0,97,1,0,0,0,0,99,1,0,0,
		0,0,101,1,0,0,0,0,103,1,0,0,0,0,105,1,0,0,0,0,107,1,0,0,0,0,109,1,0,0,
		0,0,111,1,0,0,0,0,113,1,0,0,0,0,115,1,0,0,0,0,117,1,0,0,0,0,119,1,0,0,
		0,0,121,1,0,0,0,0,123,1,0,0,0,0,125,1,0,0,0,0,127,1,0,0,0,0,129,1,0,0,
		0,0,131,1,0,0,0,0,133,1,0,0,0,0,135,1,0,0,0,0,137,1,0,0,0,0,139,1,0,0,
		0,0,141,1,0,0,0,0,143,1,0,0,0,0,145,1,0,0,0,0,147,1,0,0,0,0,149,1,0,0,
		0,0,151,1,0,0,0,0,153,1,0,0,0,0,155,1,0,0,0,0,157,1,0,0,0,0,159,1,0,0,
		0,0,161,1,0,0,0,0,163,1,0,0,0,0,165,1,0,0,0,0,167,1,0,0,0,0,169,1,0,0,
		0,0,171,1,0,0,0,0,173,1,0,0,0,0,175,1,0,0,0,0,177,1,0,0,0,0,179,1,0,0,
		0,0,181,1,0,0,0,0,183,1,0,0,0,0,185,1,0,0,0,0,187,1,0,0,0,0,189,1,0,0,
		0,0,191,1,0,0,0,0,193,1,0,0,0,0,195,1,0,0,0,0,197,1,0,0,0,0,199,1,0,0,
		0,0,201,1,0,0,0,0,203,1,0,0,0,0,205,1,0,0,0,0,207,1,0,0,0,0,209,1,0,0,
		0,0,211,1,0,0,0,0,213,1,0,0,0,0,215,1,0,0,0,0,217,1,0,0,0,0,219,1,0,0,
		0,0,221,1,0,0,0,0,223,1,0,0,0,0,225,1,0,0,0,0,227,1,0,0,0,0,229,1,0,0,
		0,0,231,1,0,0,0,0,233,1,0,0,0,0,235,1,0,0,0,0,237,1,0,0,0,0,239,1,0,0,
		0,0,245,1,0,0,0,0,251,1,0,0,0,0,253,1,0,0,0,0,255,1,0,0,0,0,257,1,0,0,
		0,0,259,1,0,0,0,0,261,1,0,0,0,0,263,1,0,0,0,0,265,1,0,0,0,1,267,1,0,0,
		0,3,269,1,0,0,0,5,271,1,0,0,0,7,274,1,0,0,0,9,277,1,0,0,0,11,279,1,0,0,
		0,13,281,1,0,0,0,15,283,1,0,0,0,17,287,1,0,0,0,19,289,1,0,0,0,21,291,1,
		0,0,0,23,293,1,0,0,0,25,295,1,0,0,0,27,298,1,0,0,0,29,301,1,0,0,0,31,303,
		1,0,0,0,33,305,1,0,0,0,35,307,1,0,0,0,37,309,1,0,0,0,39,311,1,0,0,0,41,
		313,1,0,0,0,43,316,1,0,0,0,45,318,1,0,0,0,47,321,1,0,0,0,49,323,1,0,0,
		0,51,326,1,0,0,0,53,329,1,0,0,0,55,332,1,0,0,0,57,335,1,0,0,0,59,338,1,
		0,0,0,61,346,1,0,0,0,63,353,1,0,0,0,65,359,1,0,0,0,67,369,1,0,0,0,69,377,
		1,0,0,0,71,385,1,0,0,0,73,392,1,0,0,0,75,396,1,0,0,0,77,400,1,0,0,0,79,
		403,1,0,0,0,81,406,1,0,0,0,83,411,1,0,0,0,85,419,1,0,0,0,87,424,1,0,0,
		0,89,428,1,0,0,0,91,435,1,0,0,0,93,441,1,0,0,0,95,446,1,0,0,0,97,449,1,
		0,0,0,99,452,1,0,0,0,101,455,1,0,0,0,103,460,1,0,0,0,105,464,1,0,0,0,107,
		469,1,0,0,0,109,472,1,0,0,0,111,477,1,0,0,0,113,480,1,0,0,0,115,485,1,
		0,0,0,117,490,1,0,0,0,119,493,1,0,0,0,121,498,1,0,0,0,123,503,1,0,0,0,
		125,508,1,0,0,0,127,516,1,0,0,0,129,525,1,0,0,0,131,530,1,0,0,0,133,543,
		1,0,0,0,135,561,1,0,0,0,137,570,1,0,0,0,139,575,1,0,0,0,141,581,1,0,0,
		0,143,585,1,0,0,0,145,590,1,0,0,0,147,597,1,0,0,0,149,604,1,0,0,0,151,
		616,1,0,0,0,153,621,1,0,0,0,155,630,1,0,0,0,157,638,1,0,0,0,159,647,1,
		0,0,0,161,652,1,0,0,0,163,657,1,0,0,0,165,661,1,0,0,0,167,665,1,0,0,0,
		169,673,1,0,0,0,171,678,1,0,0,0,173,687,1,0,0,0,175,694,1,0,0,0,177,700,
		1,0,0,0,179,706,1,0,0,0,181,713,1,0,0,0,183,718,1,0,0,0,185,724,1,0,0,
		0,187,731,1,0,0,0,189,737,1,0,0,0,191,747,1,0,0,0,193,752,1,0,0,0,195,
		757,1,0,0,0,197,762,1,0,0,0,199,767,1,0,0,0,201,773,1,0,0,0,203,778,1,
		0,0,0,205,784,1,0,0,0,207,791,1,0,0,0,209,797,1,0,0,0,211,803,1,0,0,0,
		213,808,1,0,0,0,215,818,1,0,0,0,217,828,1,0,0,0,219,834,1,0,0,0,221,838,
		1,0,0,0,223,843,1,0,0,0,225,850,1,0,0,0,227,854,1,0,0,0,229,860,1,0,0,
		0,231,866,1,0,0,0,233,873,1,0,0,0,235,878,1,0,0,0,237,886,1,0,0,0,239,
		898,1,0,0,0,241,902,1,0,0,0,243,904,1,0,0,0,245,923,1,0,0,0,247,925,1,
		0,0,0,249,927,1,0,0,0,251,930,1,0,0,0,253,975,1,0,0,0,255,977,1,0,0,0,
		257,1000,1,0,0,0,259,1004,1,0,0,0,261,1010,1,0,0,0,263,1027,1,0,0,0,265,
		1041,1,0,0,0,267,268,5,40,0,0,268,2,1,0,0,0,269,270,5,41,0,0,270,4,1,0,
		0,0,271,272,5,58,0,0,272,273,5,61,0,0,273,6,1,0,0,0,274,275,5,61,0,0,275,
		276,5,62,0,0,276,8,1,0,0,0,277,278,5,58,0,0,278,10,1,0,0,0,279,280,5,44,
		0,0,280,12,1,0,0,0,281,282,5,46,0,0,282,14,1,0,0,0,283,284,5,46,0,0,284,
		285,5,46,0,0,285,286,5,46,0,0,286,16,1,0,0,0,287,288,5,59,0,0,288,18,1,
		0,0,0,289,290,5,63,0,0,290,20,1,0,0,0,291,292,5,91,0,0,292,22,1,0,0,0,
		293,294,5,93,0,0,294,24,1,0,0,0,295,296,5,91,0,0,296,297,5,93,0,0,297,
		26,1,0,0,0,298,299,5,38,0,0,299,300,5,62,0,0,300,28,1,0,0,0,301,302,5,
		43,0,0,302,30,1,0,0,0,303,304,5,45,0,0,304,32,1,0,0,0,305,306,5,42,0,0,
		306,34,1,0,0,0,307,308,5,47,0,0,308,36,1,0,0,0,309,310,5,37,0,0,310,38,
		1,0,0,0,311,312,5,61,0,0,312,40,1,0,0,0,313,314,5,60,0,0,314,315,5,62,
		0,0,315,42,1,0,0,0,316,317,5,62,0,0,317,44,1,0,0,0,318,319,5,62,0,0,319,
		320,5,61,0,0,320,46,1,0,0,0,321,322,5,60,0,0,322,48,1,0,0,0,323,324,5,
		60,0,0,324,325,5,61,0,0,325,50,1,0,0,0,326,327,5,124,0,0,327,328,5,124,
		0,0,328,52,1,0,0,0,329,330,5,60,0,0,330,331,5,60,0,0,331,54,1,0,0,0,332,
		333,5,62,0,0,333,334,5,62,0,0,334,56,1,0,0,0,335,336,5,58,0,0,336,337,
		5,58,0,0,337,58,1,0,0,0,338,339,7,0,0,0,339,340,7,1,0,0,340,341,7,2,0,
		0,341,342,7,3,0,0,342,343,7,4,0,0,343,344,7,3,0,0,344,345,7,5,0,0,345,
		60,1,0,0,0,346,347,7,6,0,0,347,348,7,2,0,0,348,349,7,5,0,0,349,350,7,0,
		0,0,350,351,7,1,0,0,351,352,7,4,0,0,352,62,1,0,0,0,353,354,7,7,0,0,354,
		355,7,8,0,0,355,356,7,9,0,0,356,357,7,10,0,0,357,358,7,2,0,0,358,64,1,
		0,0,0,359,360,7,2,0,0,360,361,7,0,0,0,361,362,7,11,0,0,362,363,7,3,0,0,
		363,364,7,6,0,0,364,365,7,2,0,0,365,366,7,10,0,0,366,367,7,11,0,0,367,
		368,7,12,0,0,368,66,1,0,0,0,369,370,7,13,0,0,370,371,7,9,0,0,371,372,7,
		9,0,0,372,373,7,8,0,0,373,374,7,3,0,0,374,375,7,10,0,0,375,376,7,1,0,0,
		376,68,1,0,0,0,377,378,7,1,0,0,378,379,7,14,0,0,379,380,7,11,0,0,380,381,
		7,3,0,0,381,382,7,5,0,0,382,383,7,0,0,0,383,384,7,15,0,0,384,70,1,0,0,
		0,385,386,7,9,0,0,386,387,7,13,0,0,387,388,7,16,0,0,388,389,7,3,0,0,389,
		390,7,15,0,0,390,391,7,2,0,0,391,72,1,0,0,0,392,393,7,10,0,0,393,394,7,
		1,0,0,394,395,7,17,0,0,395,74,1,0,0,0,396,397,7,10,0,0,397,398,7,1,0,0,
		398,399,7,18,0,0,399,76,1,0,0,0,400,401,7,10,0,0,401,402,7,6,0,0,402,78,
		1,0,0,0,403,404,7,13,0,0,404,405,7,17,0,0,405,80,1,0,0,0,406,407,7,15,
		0,0,407,408,7,10,0,0,408,409,7,6,0,0,409,410,7,2,0,0,410,82,1,0,0,0,411,
		412,7,18,0,0,412,413,7,3,0,0,413,414,7,7,0,0,414,415,7,10,0,0,415,416,
		7,14,0,0,416,417,7,8,0,0,417,418,7,2,0,0,418,84,1,0,0,0,419,420,7,3,0,
		0,420,421,7,8,0,0,421,422,7,6,0,0,422,423,7,3,0,0,423,86,1,0,0,0,424,425,
		7,3,0,0,425,426,7,1,0,0,426,427,7,18,0,0,427,88,1,0,0,0,428,429,7,3,0,
		0,429,430,7,19,0,0,430,431,7,0,0,0,431,432,7,6,0,0,432,433,7,2,0,0,433,
		434,7,6,0,0,434,90,1,0,0,0,435,436,7,7,0,0,436,437,7,10,0,0,437,438,7,
		8,0,0,438,439,7,6,0,0,439,440,7,3,0,0,440,92,1,0,0,0,441,442,7,7,0,0,442,
		443,7,5,0,0,443,444,7,9,0,0,444,445,7,11,0,0,445,94,1,0,0,0,446,447,7,
		0,0,0,447,448,7,7,0,0,448,96,1,0,0,0,449,450,7,0,0,0,450,451,7,1,0,0,451,
		98,1,0,0,0,452,453,7,0,0,0,453,454,7,6,0,0,454,100,1,0,0,0,455,456,7,8,
		0,0,456,457,7,0,0,0,457,458,7,20,0,0,458,459,7,3,0,0,459,102,1,0,0,0,460,
		461,7,1,0,0,461,462,7,9,0,0,462,463,7,2,0,0,463,104,1,0,0,0,464,465,7,
		1,0,0,465,466,7,14,0,0,466,467,7,8,0,0,467,468,7,8,0,0,468,106,1,0,0,0,
		469,470,7,9,0,0,470,471,7,1,0,0,471,108,1,0,0,0,472,473,7,9,0,0,473,474,
		7,1,0,0,474,475,7,8,0,0,475,476,7,17,0,0,476,110,1,0,0,0,477,478,7,9,0,
		0,478,479,7,5,0,0,479,112,1,0,0,0,480,481,7,6,0,0,481,482,7,9,0,0,482,
		483,7,11,0,0,483,484,7,3,0,0,484,114,1,0,0,0,485,486,7,2,0,0,486,487,7,
		21,0,0,487,488,7,3,0,0,488,489,7,1,0,0,489,116,1,0,0,0,490,491,7,2,0,0,
		491,492,7,9,0,0,492,118,1,0,0,0,493,494,7,2,0,0,494,495,7,5,0,0,495,496,
		7,14,0,0,496,497,7,3,0,0,497,120,1,0,0,0,498,499,7,22,0,0,499,500,7,9,
		0,0,500,501,7,0,0,0,501,502,7,18,0,0,502,122,1,0,0,0,503,504,7,2,0,0,504,
		505,7,5,0,0,505,506,7,0,0,0,506,507,7,11,0,0,507,124,1,0,0,0,508,509,7,
		8,0,0,509,510,7,3,0,0,510,511,7,10,0,0,511,512,7,18,0,0,512,513,7,0,0,
		0,513,514,7,1,0,0,514,515,7,4,0,0,515,126,1,0,0,0,516,517,7,2,0,0,517,
		518,7,5,0,0,518,519,7,10,0,0,519,520,7,0,0,0,520,521,7,8,0,0,521,522,7,
		0,0,0,522,523,7,1,0,0,523,524,7,4,0,0,524,128,1,0,0,0,525,526,7,13,0,0,
		526,527,7,9,0,0,527,528,7,2,0,0,528,529,7,21,0,0,529,130,1,0,0,0,530,531,
		7,15,0,0,531,532,7,14,0,0,532,533,7,5,0,0,533,534,7,5,0,0,534,535,7,3,
		0,0,535,536,7,1,0,0,536,537,7,2,0,0,537,538,5,95,0,0,538,539,7,18,0,0,
		539,540,7,10,0,0,540,541,7,2,0,0,541,542,7,3,0,0,542,132,1,0,0,0,543,544,
		7,15,0,0,544,545,7,14,0,0,545,546,7,5,0,0,546,547,7,5,0,0,547,548,7,3,
		0,0,548,549,7,1,0,0,549,550,7,2,0,0,550,551,5,95,0,0,551,552,7,2,0,0,552,
		553,7,0,0,0,553,554,7,11,0,0,554,555,7,3,0,0,555,556,7,6,0,0,556,557,7,
		2,0,0,557,558,7,10,0,0,558,559,7,11,0,0,559,560,7,12,0,0,560,134,1,0,0,
		0,561,562,7,0,0,0,562,563,7,1,0,0,563,564,7,2,0,0,564,565,7,3,0,0,565,
		566,7,5,0,0,566,567,7,22,0,0,567,568,7,10,0,0,568,569,7,8,0,0,569,136,
		1,0,0,0,570,571,7,17,0,0,571,572,7,3,0,0,572,573,7,10,0,0,573,574,7,5,
		0,0,574,138,1,0,0,0,575,576,7,11,0,0,576,577,7,9,0,0,577,578,7,1,0,0,578,
		579,7,2,0,0,579,580,7,21,0,0,580,140,1,0,0,0,581,582,7,18,0,0,582,583,
		7,10,0,0,583,584,7,17,0,0,584,142,1,0,0,0,585,586,7,21,0,0,586,587,7,9,
		0,0,587,588,7,14,0,0,588,589,7,5,0,0,589,144,1,0,0,0,590,591,7,11,0,0,
		591,592,7,0,0,0,592,593,7,1,0,0,593,594,7,14,0,0,594,595,7,2,0,0,595,596,
		7,3,0,0,596,146,1,0,0,0,597,598,7,6,0,0,598,599,7,3,0,0,599,600,7,15,0,
		0,600,601,7,9,0,0,601,602,7,1,0,0,602,603,7,18,0,0,603,148,1,0,0,0,604,
		605,7,11,0,0,605,606,7,0,0,0,606,607,7,8,0,0,607,608,7,8,0,0,608,609,7,
		0,0,0,609,610,7,6,0,0,610,611,7,3,0,0,611,612,7,15,0,0,612,613,7,9,0,0,
		613,614,7,1,0,0,614,615,7,18,0,0,615,150,1,0,0,0,616,617,7,15,0,0,617,
		618,7,10,0,0,618,619,7,6,0,0,619,620,7,3,0,0,620,152,1,0,0,0,621,622,7,
		15,0,0,622,623,7,9,0,0,623,624,7,10,0,0,624,625,7,8,0,0,625,626,7,3,0,
		0,626,627,7,6,0,0,627,628,7,15,0,0,628,629,7,3,0,0,629,154,1,0,0,0,630,
		631,7,3,0,0,631,632,7,19,0,0,632,633,7,2,0,0,633,634,7,5,0,0,634,635,7,
		10,0,0,635,636,7,15,0,0,636,637,7,2,0,0,637,156,1,0,0,0,638,639,7,12,0,
		0,639,640,7,9,0,0,640,641,7,6,0,0,641,642,7,0,0,0,642,643,7,2,0,0,643,
		644,7,0,0,0,644,645,7,9,0,0,645,646,7,1,0,0,646,158,1,0,0,0,647,648,7,
		23,0,0,648,649,7,21,0,0,649,650,7,3,0,0,650,651,7,1,0,0,651,160,1,0,0,
		0,652,653,7,3,0,0,653,654,7,15,0,0,654,655,7,21,0,0,655,656,7,9,0,0,656,
		162,1,0,0,0,657,658,7,10,0,0,658,659,7,8,0,0,659,660,7,8,0,0,660,164,1,
		0,0,0,661,662,7,10,0,0,662,663,7,6,0,0,663,664,7,15,0,0,664,166,1,0,0,
		0,665,666,7,13,0,0,666,667,7,3,0,0,667,668,7,2,0,0,668,669,7,23,0,0,669,
		670,7,3,0,0,670,671,7,3,0,0,671,672,7,1,0,0,672,168,1,0,0,0,673,674,7,
		18,0,0,674,675,7,3,0,0,675,676,7,6,0,0,676,677,7,15,0,0,677,170,1,0,0,
		0,678,679,7,18,0,0,679,680,7,0,0,0,680,681,7,6,0,0,681,682,7,2,0,0,682,
		683,7,0,0,0,683,684,7,1,0,0,684,685,7,15,0,0,685,686,7,2,0,0,686,172,1,
		0,0,0,687,688,7,3,0,0,688,689,7,19,0,0,689,690,7,15,0,0,690,691,7,3,0,
		0,691,692,7,12,0,0,692,693,7,2,0,0,693,174,1,0,0,0,694,695,7,7,0,0,695,
		696,7,3,0,0,696,697,7,2,0,0,697,698,7,15,0,0,698,699,7,21,0,0,699,176,
		1,0,0,0,700,701,7,7,0,0,701,702,7,0,0,0,702,703,7,5,0,0,703,704,7,6,0,
		0,704,705,7,2,0,0,705,178,1,0,0,0,706,707,7,7,0,0,707,708,7,9,0,0,708,
		709,7,5,0,0,709,710,7,11,0,0,710,711,7,10,0,0,711,712,7,2,0,0,712,180,
		1,0,0,0,713,714,7,7,0,0,714,715,7,14,0,0,715,716,7,8,0,0,716,717,7,8,0,
		0,717,182,1,0,0,0,718,719,7,4,0,0,719,720,7,5,0,0,720,721,7,9,0,0,721,
		722,7,14,0,0,722,723,7,12,0,0,723,184,1,0,0,0,724,725,7,21,0,0,725,726,
		7,10,0,0,726,727,7,22,0,0,727,728,7,0,0,0,728,729,7,1,0,0,729,730,7,4,
		0,0,730,186,1,0,0,0,731,732,7,0,0,0,732,733,7,1,0,0,733,734,7,1,0,0,734,
		735,7,3,0,0,735,736,7,5,0,0,736,188,1,0,0,0,737,738,7,0,0,0,738,739,7,
		1,0,0,739,740,7,2,0,0,740,741,7,3,0,0,741,742,7,5,0,0,742,743,7,6,0,0,
		743,744,7,3,0,0,744,745,7,15,0,0,745,746,7,2,0,0,746,190,1,0,0,0,747,748,
		7,0,0,0,748,749,7,1,0,0,749,750,7,2,0,0,750,751,7,9,0,0,751,192,1,0,0,
		0,752,753,7,16,0,0,753,754,7,9,0,0,754,755,7,0,0,0,755,756,7,1,0,0,756,
		194,1,0,0,0,757,758,7,8,0,0,758,759,7,10,0,0,759,760,7,6,0,0,760,761,7,
		2,0,0,761,196,1,0,0,0,762,763,7,8,0,0,763,764,7,3,0,0,764,765,7,7,0,0,
		765,766,7,2,0,0,766,198,1,0,0,0,767,768,7,8,0,0,768,769,7,0,0,0,769,770,
		7,11,0,0,770,771,7,0,0,0,771,772,7,2,0,0,772,200,1,0,0,0,773,774,7,1,0,
		0,774,775,7,3,0,0,775,776,7,19,0,0,776,777,7,2,0,0,777,202,1,0,0,0,778,
		779,7,1,0,0,779,780,7,14,0,0,780,781,7,8,0,0,781,782,7,8,0,0,782,783,7,
		6,0,0,783,204,1,0,0,0,784,785,7,9,0,0,785,786,7,7,0,0,786,787,7,7,0,0,
		787,788,7,6,0,0,788,789,7,3,0,0,789,790,7,2,0,0,790,206,1,0,0,0,791,792,
		7,9,0,0,792,793,7,5,0,0,793,794,7,18,0,0,794,795,7,3,0,0,795,796,7,5,0,
		0,796,208,1,0,0,0,797,798,7,9,0,0,798,799,7,14,0,0,799,800,7,2,0,0,800,
		801,7,3,0,0,801,802,7,5,0,0,802,210,1,0,0,0,803,804,7,9,0,0,804,805,7,
		22,0,0,805,806,7,3,0,0,806,807,7,5,0,0,807,212,1,0,0,0,808,809,7,12,0,
		0,809,810,7,10,0,0,810,811,7,5,0,0,811,812,7,2,0,0,812,813,7,0,0,0,813,
		814,7,2,0,0,814,815,7,0,0,0,815,816,7,9,0,0,816,817,7,1,0,0,817,214,1,
		0,0,0,818,819,7,5,0,0,819,820,7,3,0,0,820,821,7,15,0,0,821,822,7,14,0,
		0,822,823,7,5,0,0,823,824,7,6,0,0,824,825,7,0,0,0,825,826,7,22,0,0,826,
		827,7,3,0,0,827,216,1,0,0,0,828,829,7,5,0,0,829,830,7,0,0,0,830,831,7,
		4,0,0,831,832,7,21,0,0,832,833,7,2,0,0,833,218,1,0,0,0,834,835,7,5,0,0,
		835,836,7,9,0,0,836,837,7,23,0,0,837,220,1,0,0,0,838,839,7,5,0,0,839,840,
		7,9,0,0,840,841,7,23,0,0,841,842,7,6,0,0,842,222,1,0,0,0,843,844,7,6,0,
		0,844,845,7,3,0,0,845,846,7,8,0,0,846,847,7,3,0,0,847,848,7,15,0,0,848,
		849,7,2,0,0,849,224,1,0,0,0,850,851,7,2,0,0,851,852,7,9,0,0,852,853,7,
		12,0,0,853,226,1,0,0,0,854,855,7,14,0,0,855,856,7,1,0,0,856,857,7,0,0,
		0,857,858,7,9,0,0,858,859,7,1,0,0,859,228,1,0,0,0,860,861,7,23,0,0,861,
		862,7,21,0,0,862,863,7,3,0,0,863,864,7,5,0,0,864,865,7,3,0,0,865,230,1,
		0,0,0,866,867,7,23,0,0,867,868,7,0,0,0,868,869,7,1,0,0,869,870,7,18,0,
		0,870,871,7,9,0,0,871,872,7,23,0,0,872,232,1,0,0,0,873,874,7,23,0,0,874,
		875,7,0,0,0,875,876,7,2,0,0,876,877,7,21,0,0,877,234,1,0,0,0,878,879,7,
		18,0,0,879,880,7,3,0,0,880,881,7,15,0,0,881,882,7,8,0,0,882,883,7,10,0,
		0,883,884,7,5,0,0,884,885,7,3,0,0,885,236,1,0,0,0,886,887,7,6,0,0,887,
		888,7,3,0,0,888,889,7,2,0,0,889,238,1,0,0,0,890,899,3,59,29,0,891,899,
		3,61,30,0,892,899,3,63,31,0,893,899,3,65,32,0,894,899,3,67,33,0,895,899,
		3,69,34,0,896,899,3,71,35,0,897,899,3,73,36,0,898,890,1,0,0,0,898,891,
		1,0,0,0,898,892,1,0,0,0,898,893,1,0,0,0,898,894,1,0,0,0,898,895,1,0,0,
		0,898,896,1,0,0,0,898,897,1,0,0,0,899,240,1,0,0,0,900,903,3,243,121,0,
		901,903,7,24,0,0,902,900,1,0,0,0,902,901,1,0,0,0,903,242,1,0,0,0,904,905,
		7,25,0,0,905,244,1,0,0,0,906,910,3,243,121,0,907,909,3,241,120,0,908,907,
		1,0,0,0,909,912,1,0,0,0,910,908,1,0,0,0,910,911,1,0,0,0,911,924,1,0,0,
		0,912,910,1,0,0,0,913,919,5,91,0,0,914,918,8,26,0,0,915,916,5,93,0,0,916,
		918,5,93,0,0,917,914,1,0,0,0,917,915,1,0,0,0,918,921,1,0,0,0,919,917,1,
		0,0,0,919,920,1,0,0,0,920,922,1,0,0,0,921,919,1,0,0,0,922,924,5,93,0,0,
		923,906,1,0,0,0,923,913,1,0,0,0,924,246,1,0,0,0,925,926,7,27,0,0,926,248,
		1,0,0,0,927,928,7,28,0,0,928,250,1,0,0,0,929,931,3,249,124,0,930,929,1,
		0,0,0,931,932,1,0,0,0,932,930,1,0,0,0,932,933,1,0,0,0,933,252,1,0,0,0,
		934,936,3,249,124,0,935,934,1,0,0,0,936,937,1,0,0,0,937,935,1,0,0,0,937,
		938,1,0,0,0,938,946,1,0,0,0,939,943,5,46,0,0,940,942,3,249,124,0,941,940,
		1,0,0,0,942,945,1,0,0,0,943,941,1,0,0,0,943,944,1,0,0,0,944,947,1,0,0,
		0,945,943,1,0,0,0,946,939,1,0,0,0,946,947,1,0,0,0,947,955,1,0,0,0,948,
		950,5,46,0,0,949,951,3,249,124,0,950,949,1,0,0,0,951,952,1,0,0,0,952,950,
		1,0,0,0,952,953,1,0,0,0,953,955,1,0,0,0,954,935,1,0,0,0,954,948,1,0,0,
		0,955,965,1,0,0,0,956,958,7,3,0,0,957,959,7,29,0,0,958,957,1,0,0,0,958,
		959,1,0,0,0,959,961,1,0,0,0,960,962,3,249,124,0,961,960,1,0,0,0,962,963,
		1,0,0,0,963,961,1,0,0,0,963,964,1,0,0,0,964,966,1,0,0,0,965,956,1,0,0,
		0,965,966,1,0,0,0,966,976,1,0,0,0,967,968,5,48,0,0,968,969,7,19,0,0,969,
		971,1,0,0,0,970,972,3,247,123,0,971,970,1,0,0,0,972,973,1,0,0,0,973,971,
		1,0,0,0,973,974,1,0,0,0,974,976,1,0,0,0,975,954,1,0,0,0,975,967,1,0,0,
		0,976,254,1,0,0,0,977,978,3,253,126,0,978,979,7,11,0,0,979,256,1,0,0,0,
		980,986,5,39,0,0,981,985,8,30,0,0,982,983,5,39,0,0,983,985,5,39,0,0,984,
		981,1,0,0,0,984,982,1,0,0,0,985,988,1,0,0,0,986,984,1,0,0,0,986,987,1,
		0,0,0,987,989,1,0,0,0,988,986,1,0,0,0,989,1001,5,39,0,0,990,996,5,34,0,
		0,991,995,8,31,0,0,992,993,5,34,0,0,993,995,5,34,0,0,994,991,1,0,0,0,994,
		992,1,0,0,0,995,998,1,0,0,0,996,994,1,0,0,0,996,997,1,0,0,0,997,999,1,
		0,0,0,998,996,1,0,0,0,999,1001,5,34,0,0,1000,980,1,0,0,0,1000,990,1,0,
		0,0,1001,258,1,0,0,0,1002,1005,3,119,59,0,1003,1005,3,91,45,0,1004,1002,
		1,0,0,0,1004,1003,1,0,0,0,1005,260,1,0,0,0,1006,1007,5,45,0,0,1007,1011,
		5,45,0,0,1008,1009,5,35,0,0,1009,1011,5,33,0,0,1010,1006,1,0,0,0,1010,
		1008,1,0,0,0,1011,1015,1,0,0,0,1012,1014,8,32,0,0,1013,1012,1,0,0,0,1014,
		1017,1,0,0,0,1015,1013,1,0,0,0,1015,1016,1,0,0,0,1016,1023,1,0,0,0,1017,
		1015,1,0,0,0,1018,1020,5,13,0,0,1019,1018,1,0,0,0,1019,1020,1,0,0,0,1020,
		1021,1,0,0,0,1021,1024,5,10,0,0,1022,1024,5,0,0,1,1023,1019,1,0,0,0,1023,
		1022,1,0,0,0,1024,1025,1,0,0,0,1025,1026,6,130,0,0,1026,262,1,0,0,0,1027,
		1028,5,47,0,0,1028,1029,5,42,0,0,1029,1033,1,0,0,0,1030,1032,9,0,0,0,1031,
		1030,1,0,0,0,1032,1035,1,0,0,0,1033,1034,1,0,0,0,1033,1031,1,0,0,0,1034,
		1036,1,0,0,0,1035,1033,1,0,0,0,1036,1037,5,42,0,0,1037,1038,5,47,0,0,1038,
		1039,1,0,0,0,1039,1040,6,131,0,0,1040,264,1,0,0,0,1041,1042,7,33,0,0,1042,
		1043,1,0,0,0,1043,1044,6,132,0,0,1044,266,1,0,0,0,29,0,898,902,910,917,
		919,923,932,937,943,946,952,954,958,963,965,973,975,984,986,994,996,1000,
		1004,1010,1015,1019,1023,1033,1,0,1,0
	};

	public static readonly ATN _ATN =
		new ATNDeserializer().Deserialize(_serializedATN);


}
} // namespace QueryCat.Backend.Parser
