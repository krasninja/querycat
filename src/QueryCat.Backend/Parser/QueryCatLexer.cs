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
		LEFT_RIGHT_BRACKET=13, PLUS=14, MINUS=15, STAR=16, DIV=17, MOD=18, EQUALS=19, 
		NOT_EQUALS=20, GREATER=21, GREATER_OR_EQUALS=22, LESS=23, LESS_OR_EQUALS=24, 
		CONCAT=25, LESS_LESS=26, GREATER_GREATER=27, TYPECAST=28, INTEGER=29, 
		STRING=30, FLOAT=31, TIMESTAMP=32, BOOLEAN=33, NUMERIC=34, OBJECT=35, 
		ANY=36, AND=37, AS=38, BY=39, CAST=40, DEFAULT=41, EXISTS=42, FALSE=43, 
		FROM=44, IN=45, IS=46, LIKE=47, NOT=48, NULL=49, ON=50, ONLY=51, OR=52, 
		SOME=53, TO=54, TRUE=55, VOID=56, TRIM=57, LEADING=58, TRAILING=59, BOTH=60, 
		CURRENT_DATE=61, CURRENT_TIMESTAMP=62, INTERVAL=63, YEAR=64, MONTH=65, 
		DAY=66, HOUR=67, MINUTE=68, SECOND=69, MILLISECOND=70, POSITION=71, EXTRACT=72, 
		ECHO=73, ALL=74, ASC=75, BETWEEN=76, DESC=77, DISTINCT=78, FETCH=79, FIRST=80, 
		FORMAT=81, GROUP=82, HAVING=83, INTO=84, LIMIT=85, NEXT=86, OFFSET=87, 
		ORDER=88, ROW=89, ROWS=90, SELECT=91, TOP=92, UNION=93, WHERE=94, TYPE=95, 
		IDENTIFIER=96, INTEGER_LITERAL=97, FLOAT_LITERAL=98, NUMERIC_LITERAL=99, 
		STRING_LITERAL=100, BOOLEAN_LITERAL=101, SINGLE_LINE_COMMENT=102, MULTILINE_COMMENT=103, 
		SPACES=104;
	public static string[] channelNames = {
		"DEFAULT_TOKEN_CHANNEL", "HIDDEN"
	};

	public static string[] modeNames = {
		"DEFAULT_MODE"
	};

	public static readonly string[] ruleNames = {
		"LEFT_PAREN", "RIGHT_PAREN", "ASSIGN", "ASSOCIATION", "COLON", "COMMA", 
		"PERIOD", "ELLIPSIS", "SEMICOLON", "QUESTION", "LEFT_BRACKET", "RIGHT_BRACKET", 
		"LEFT_RIGHT_BRACKET", "PLUS", "MINUS", "STAR", "DIV", "MOD", "EQUALS", 
		"NOT_EQUALS", "GREATER", "GREATER_OR_EQUALS", "LESS", "LESS_OR_EQUALS", 
		"CONCAT", "LESS_LESS", "GREATER_GREATER", "TYPECAST", "INTEGER", "STRING", 
		"FLOAT", "TIMESTAMP", "BOOLEAN", "NUMERIC", "OBJECT", "ANY", "AND", "AS", 
		"BY", "CAST", "DEFAULT", "EXISTS", "FALSE", "FROM", "IN", "IS", "LIKE", 
		"NOT", "NULL", "ON", "ONLY", "OR", "SOME", "TO", "TRUE", "VOID", "TRIM", 
		"LEADING", "TRAILING", "BOTH", "CURRENT_DATE", "CURRENT_TIMESTAMP", "INTERVAL", 
		"YEAR", "MONTH", "DAY", "HOUR", "MINUTE", "SECOND", "MILLISECOND", "POSITION", 
		"EXTRACT", "ECHO", "ALL", "ASC", "BETWEEN", "DESC", "DISTINCT", "FETCH", 
		"FIRST", "FORMAT", "GROUP", "HAVING", "INTO", "LIMIT", "NEXT", "OFFSET", 
		"ORDER", "ROW", "ROWS", "SELECT", "TOP", "UNION", "WHERE", "TYPE", "NameChar", 
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
		"'?'", "'['", "']'", "'[]'", "'+'", "'-'", "'*'", "'/'", "'%'", "'='", 
		"'<>'", "'>'", "'>='", "'<'", "'<='", "'||'", "'<<'", "'>>'", "'::'", 
		"'INTEGER'", "'STRING'", "'FLOAT'", "'TIMESTAMP'", "'BOOLEAN'", "'NUMERIC'", 
		"'OBJECT'", "'ANY'", "'AND'", "'AS'", "'BY'", "'CAST'", "'DEFAULT'", "'EXISTS'", 
		"'FALSE'", "'FROM'", "'IN'", "'IS'", "'LIKE'", "'NOT'", "'NULL'", "'ON'", 
		"'ONLY'", "'OR'", "'SOME'", "'TO'", "'TRUE'", "'VOID'", "'TRIM'", "'LEADING'", 
		"'TRAILING'", "'BOTH'", "'CURRENT_DATE'", "'CURRENT_TIMESTAMP'", "'INTERVAL'", 
		"'YEAR'", "'MONTH'", "'DAY'", "'HOUR'", "'MINUTE'", "'SECOND'", "'MILLISECOND'", 
		"'POSITION'", "'EXTRACT'", "'ECHO'", "'ALL'", "'ASC'", "'BETWEEN'", "'DESC'", 
		"'DISTINCT'", "'FETCH'", "'FIRST'", "'FORMAT'", "'GROUP'", "'HAVING'", 
		"'INTO'", "'LIMIT'", "'NEXT'", "'OFFSET'", "'ORDER'", "'ROW'", "'ROWS'", 
		"'SELECT'", "'TOP'", "'UNION'", "'WHERE'"
	};
	private static readonly string[] _SymbolicNames = {
		null, "LEFT_PAREN", "RIGHT_PAREN", "ASSIGN", "ASSOCIATION", "COLON", "COMMA", 
		"PERIOD", "ELLIPSIS", "SEMICOLON", "QUESTION", "LEFT_BRACKET", "RIGHT_BRACKET", 
		"LEFT_RIGHT_BRACKET", "PLUS", "MINUS", "STAR", "DIV", "MOD", "EQUALS", 
		"NOT_EQUALS", "GREATER", "GREATER_OR_EQUALS", "LESS", "LESS_OR_EQUALS", 
		"CONCAT", "LESS_LESS", "GREATER_GREATER", "TYPECAST", "INTEGER", "STRING", 
		"FLOAT", "TIMESTAMP", "BOOLEAN", "NUMERIC", "OBJECT", "ANY", "AND", "AS", 
		"BY", "CAST", "DEFAULT", "EXISTS", "FALSE", "FROM", "IN", "IS", "LIKE", 
		"NOT", "NULL", "ON", "ONLY", "OR", "SOME", "TO", "TRUE", "VOID", "TRIM", 
		"LEADING", "TRAILING", "BOTH", "CURRENT_DATE", "CURRENT_TIMESTAMP", "INTERVAL", 
		"YEAR", "MONTH", "DAY", "HOUR", "MINUTE", "SECOND", "MILLISECOND", "POSITION", 
		"EXTRACT", "ECHO", "ALL", "ASC", "BETWEEN", "DESC", "DISTINCT", "FETCH", 
		"FIRST", "FORMAT", "GROUP", "HAVING", "INTO", "LIMIT", "NEXT", "OFFSET", 
		"ORDER", "ROW", "ROWS", "SELECT", "TOP", "UNION", "WHERE", "TYPE", "IDENTIFIER", 
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
		4,0,104,843,6,-1,2,0,7,0,2,1,7,1,2,2,7,2,2,3,7,3,2,4,7,4,2,5,7,5,2,6,7,
		6,2,7,7,7,2,8,7,8,2,9,7,9,2,10,7,10,2,11,7,11,2,12,7,12,2,13,7,13,2,14,
		7,14,2,15,7,15,2,16,7,16,2,17,7,17,2,18,7,18,2,19,7,19,2,20,7,20,2,21,
		7,21,2,22,7,22,2,23,7,23,2,24,7,24,2,25,7,25,2,26,7,26,2,27,7,27,2,28,
		7,28,2,29,7,29,2,30,7,30,2,31,7,31,2,32,7,32,2,33,7,33,2,34,7,34,2,35,
		7,35,2,36,7,36,2,37,7,37,2,38,7,38,2,39,7,39,2,40,7,40,2,41,7,41,2,42,
		7,42,2,43,7,43,2,44,7,44,2,45,7,45,2,46,7,46,2,47,7,47,2,48,7,48,2,49,
		7,49,2,50,7,50,2,51,7,51,2,52,7,52,2,53,7,53,2,54,7,54,2,55,7,55,2,56,
		7,56,2,57,7,57,2,58,7,58,2,59,7,59,2,60,7,60,2,61,7,61,2,62,7,62,2,63,
		7,63,2,64,7,64,2,65,7,65,2,66,7,66,2,67,7,67,2,68,7,68,2,69,7,69,2,70,
		7,70,2,71,7,71,2,72,7,72,2,73,7,73,2,74,7,74,2,75,7,75,2,76,7,76,2,77,
		7,77,2,78,7,78,2,79,7,79,2,80,7,80,2,81,7,81,2,82,7,82,2,83,7,83,2,84,
		7,84,2,85,7,85,2,86,7,86,2,87,7,87,2,88,7,88,2,89,7,89,2,90,7,90,2,91,
		7,91,2,92,7,92,2,93,7,93,2,94,7,94,2,95,7,95,2,96,7,96,2,97,7,97,2,98,
		7,98,2,99,7,99,2,100,7,100,2,101,7,101,2,102,7,102,2,103,7,103,2,104,7,
		104,2,105,7,105,2,106,7,106,2,107,7,107,1,0,1,0,1,1,1,1,1,2,1,2,1,2,1,
		3,1,3,1,3,1,4,1,4,1,5,1,5,1,6,1,6,1,7,1,7,1,7,1,7,1,8,1,8,1,9,1,9,1,10,
		1,10,1,11,1,11,1,12,1,12,1,12,1,13,1,13,1,14,1,14,1,15,1,15,1,16,1,16,
		1,17,1,17,1,18,1,18,1,19,1,19,1,19,1,20,1,20,1,21,1,21,1,21,1,22,1,22,
		1,23,1,23,1,23,1,24,1,24,1,24,1,25,1,25,1,25,1,26,1,26,1,26,1,27,1,27,
		1,27,1,28,1,28,1,28,1,28,1,28,1,28,1,28,1,28,1,29,1,29,1,29,1,29,1,29,
		1,29,1,29,1,30,1,30,1,30,1,30,1,30,1,30,1,31,1,31,1,31,1,31,1,31,1,31,
		1,31,1,31,1,31,1,31,1,32,1,32,1,32,1,32,1,32,1,32,1,32,1,32,1,33,1,33,
		1,33,1,33,1,33,1,33,1,33,1,33,1,34,1,34,1,34,1,34,1,34,1,34,1,34,1,35,
		1,35,1,35,1,35,1,36,1,36,1,36,1,36,1,37,1,37,1,37,1,38,1,38,1,38,1,39,
		1,39,1,39,1,39,1,39,1,40,1,40,1,40,1,40,1,40,1,40,1,40,1,40,1,41,1,41,
		1,41,1,41,1,41,1,41,1,41,1,42,1,42,1,42,1,42,1,42,1,42,1,43,1,43,1,43,
		1,43,1,43,1,44,1,44,1,44,1,45,1,45,1,45,1,46,1,46,1,46,1,46,1,46,1,47,
		1,47,1,47,1,47,1,48,1,48,1,48,1,48,1,48,1,49,1,49,1,49,1,50,1,50,1,50,
		1,50,1,50,1,51,1,51,1,51,1,52,1,52,1,52,1,52,1,52,1,53,1,53,1,53,1,54,
		1,54,1,54,1,54,1,54,1,55,1,55,1,55,1,55,1,55,1,56,1,56,1,56,1,56,1,56,
		1,57,1,57,1,57,1,57,1,57,1,57,1,57,1,57,1,58,1,58,1,58,1,58,1,58,1,58,
		1,58,1,58,1,58,1,59,1,59,1,59,1,59,1,59,1,60,1,60,1,60,1,60,1,60,1,60,
		1,60,1,60,1,60,1,60,1,60,1,60,1,60,1,61,1,61,1,61,1,61,1,61,1,61,1,61,
		1,61,1,61,1,61,1,61,1,61,1,61,1,61,1,61,1,61,1,61,1,61,1,62,1,62,1,62,
		1,62,1,62,1,62,1,62,1,62,1,62,1,63,1,63,1,63,1,63,1,63,1,64,1,64,1,64,
		1,64,1,64,1,64,1,65,1,65,1,65,1,65,1,66,1,66,1,66,1,66,1,66,1,67,1,67,
		1,67,1,67,1,67,1,67,1,67,1,68,1,68,1,68,1,68,1,68,1,68,1,68,1,69,1,69,
		1,69,1,69,1,69,1,69,1,69,1,69,1,69,1,69,1,69,1,69,1,70,1,70,1,70,1,70,
		1,70,1,70,1,70,1,70,1,70,1,71,1,71,1,71,1,71,1,71,1,71,1,71,1,71,1,72,
		1,72,1,72,1,72,1,72,1,73,1,73,1,73,1,73,1,74,1,74,1,74,1,74,1,75,1,75,
		1,75,1,75,1,75,1,75,1,75,1,75,1,76,1,76,1,76,1,76,1,76,1,77,1,77,1,77,
		1,77,1,77,1,77,1,77,1,77,1,77,1,78,1,78,1,78,1,78,1,78,1,78,1,79,1,79,
		1,79,1,79,1,79,1,79,1,80,1,80,1,80,1,80,1,80,1,80,1,80,1,81,1,81,1,81,
		1,81,1,81,1,81,1,82,1,82,1,82,1,82,1,82,1,82,1,82,1,83,1,83,1,83,1,83,
		1,83,1,84,1,84,1,84,1,84,1,84,1,84,1,85,1,85,1,85,1,85,1,85,1,86,1,86,
		1,86,1,86,1,86,1,86,1,86,1,87,1,87,1,87,1,87,1,87,1,87,1,88,1,88,1,88,
		1,88,1,89,1,89,1,89,1,89,1,89,1,90,1,90,1,90,1,90,1,90,1,90,1,90,1,91,
		1,91,1,91,1,91,1,92,1,92,1,92,1,92,1,92,1,92,1,93,1,93,1,93,1,93,1,93,
		1,93,1,94,1,94,1,94,1,94,1,94,1,94,1,94,1,94,3,94,700,8,94,1,95,1,95,3,
		95,704,8,95,1,96,1,96,1,97,1,97,5,97,710,8,97,10,97,12,97,713,9,97,1,97,
		1,97,1,97,1,97,5,97,719,8,97,10,97,12,97,722,9,97,1,97,3,97,725,8,97,1,
		98,1,98,1,99,1,99,1,100,4,100,732,8,100,11,100,12,100,733,1,101,4,101,
		737,8,101,11,101,12,101,738,1,101,1,101,5,101,743,8,101,10,101,12,101,
		746,9,101,3,101,748,8,101,1,101,1,101,4,101,752,8,101,11,101,12,101,753,
		3,101,756,8,101,1,101,1,101,3,101,760,8,101,1,101,4,101,763,8,101,11,101,
		12,101,764,3,101,767,8,101,1,101,1,101,1,101,1,101,4,101,773,8,101,11,
		101,12,101,774,3,101,777,8,101,1,102,1,102,1,102,1,103,1,103,1,103,1,103,
		5,103,786,8,103,10,103,12,103,789,9,103,1,103,1,103,1,103,1,103,1,103,
		5,103,796,8,103,10,103,12,103,799,9,103,1,103,3,103,802,8,103,1,104,1,
		104,3,104,806,8,104,1,105,1,105,1,105,1,105,5,105,812,8,105,10,105,12,
		105,815,9,105,1,105,3,105,818,8,105,1,105,1,105,3,105,822,8,105,1,105,
		1,105,1,106,1,106,1,106,1,106,5,106,830,8,106,10,106,12,106,833,9,106,
		1,106,1,106,1,106,1,106,1,106,1,107,1,107,1,107,1,107,1,831,0,108,1,1,
		3,2,5,3,7,4,9,5,11,6,13,7,15,8,17,9,19,10,21,11,23,12,25,13,27,14,29,15,
		31,16,33,17,35,18,37,19,39,20,41,21,43,22,45,23,47,24,49,25,51,26,53,27,
		55,28,57,29,59,30,61,31,63,32,65,33,67,34,69,35,71,36,73,37,75,38,77,39,
		79,40,81,41,83,42,85,43,87,44,89,45,91,46,93,47,95,48,97,49,99,50,101,
		51,103,52,105,53,107,54,109,55,111,56,113,57,115,58,117,59,119,60,121,
		61,123,62,125,63,127,64,129,65,131,66,133,67,135,68,137,69,139,70,141,
		71,143,72,145,73,147,74,149,75,151,76,153,77,155,78,157,79,159,80,161,
		81,163,82,165,83,167,84,169,85,171,86,173,87,175,88,177,89,179,90,181,
		91,183,92,185,93,187,94,189,95,191,0,193,0,195,96,197,0,199,0,201,97,203,
		98,205,99,207,100,209,101,211,102,213,103,215,104,1,0,34,2,0,73,73,105,
		105,2,0,78,78,110,110,2,0,84,84,116,116,2,0,69,69,101,101,2,0,71,71,103,
		103,2,0,82,82,114,114,2,0,83,83,115,115,2,0,70,70,102,102,2,0,76,76,108,
		108,2,0,79,79,111,111,2,0,65,65,97,97,2,0,77,77,109,109,2,0,80,80,112,
		112,2,0,66,66,98,98,2,0,85,85,117,117,2,0,67,67,99,99,2,0,74,74,106,106,
		2,0,89,89,121,121,2,0,68,68,100,100,2,0,88,88,120,120,2,0,75,75,107,107,
		2,0,86,86,118,118,2,0,72,72,104,104,2,0,87,87,119,119,5,0,48,57,95,95,
		183,183,768,879,8255,8256,14,0,65,90,95,95,97,122,192,214,224,246,248,
		767,880,893,895,8191,8204,8205,8304,8591,11264,12271,12289,55295,63744,
		64975,65008,65533,1,0,93,93,3,0,48,57,65,70,97,102,1,0,48,57,2,0,43,43,
		45,45,1,0,39,39,1,0,34,34,2,0,10,10,13,13,3,0,9,11,13,13,32,32,871,0,1,
		1,0,0,0,0,3,1,0,0,0,0,5,1,0,0,0,0,7,1,0,0,0,0,9,1,0,0,0,0,11,1,0,0,0,0,
		13,1,0,0,0,0,15,1,0,0,0,0,17,1,0,0,0,0,19,1,0,0,0,0,21,1,0,0,0,0,23,1,
		0,0,0,0,25,1,0,0,0,0,27,1,0,0,0,0,29,1,0,0,0,0,31,1,0,0,0,0,33,1,0,0,0,
		0,35,1,0,0,0,0,37,1,0,0,0,0,39,1,0,0,0,0,41,1,0,0,0,0,43,1,0,0,0,0,45,
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
		0,0,195,1,0,0,0,0,201,1,0,0,0,0,203,1,0,0,0,0,205,1,0,0,0,0,207,1,0,0,
		0,0,209,1,0,0,0,0,211,1,0,0,0,0,213,1,0,0,0,0,215,1,0,0,0,1,217,1,0,0,
		0,3,219,1,0,0,0,5,221,1,0,0,0,7,224,1,0,0,0,9,227,1,0,0,0,11,229,1,0,0,
		0,13,231,1,0,0,0,15,233,1,0,0,0,17,237,1,0,0,0,19,239,1,0,0,0,21,241,1,
		0,0,0,23,243,1,0,0,0,25,245,1,0,0,0,27,248,1,0,0,0,29,250,1,0,0,0,31,252,
		1,0,0,0,33,254,1,0,0,0,35,256,1,0,0,0,37,258,1,0,0,0,39,260,1,0,0,0,41,
		263,1,0,0,0,43,265,1,0,0,0,45,268,1,0,0,0,47,270,1,0,0,0,49,273,1,0,0,
		0,51,276,1,0,0,0,53,279,1,0,0,0,55,282,1,0,0,0,57,285,1,0,0,0,59,293,1,
		0,0,0,61,300,1,0,0,0,63,306,1,0,0,0,65,316,1,0,0,0,67,324,1,0,0,0,69,332,
		1,0,0,0,71,339,1,0,0,0,73,343,1,0,0,0,75,347,1,0,0,0,77,350,1,0,0,0,79,
		353,1,0,0,0,81,358,1,0,0,0,83,366,1,0,0,0,85,373,1,0,0,0,87,379,1,0,0,
		0,89,384,1,0,0,0,91,387,1,0,0,0,93,390,1,0,0,0,95,395,1,0,0,0,97,399,1,
		0,0,0,99,404,1,0,0,0,101,407,1,0,0,0,103,412,1,0,0,0,105,415,1,0,0,0,107,
		420,1,0,0,0,109,423,1,0,0,0,111,428,1,0,0,0,113,433,1,0,0,0,115,438,1,
		0,0,0,117,446,1,0,0,0,119,455,1,0,0,0,121,460,1,0,0,0,123,473,1,0,0,0,
		125,491,1,0,0,0,127,500,1,0,0,0,129,505,1,0,0,0,131,511,1,0,0,0,133,515,
		1,0,0,0,135,520,1,0,0,0,137,527,1,0,0,0,139,534,1,0,0,0,141,546,1,0,0,
		0,143,555,1,0,0,0,145,563,1,0,0,0,147,568,1,0,0,0,149,572,1,0,0,0,151,
		576,1,0,0,0,153,584,1,0,0,0,155,589,1,0,0,0,157,598,1,0,0,0,159,604,1,
		0,0,0,161,610,1,0,0,0,163,617,1,0,0,0,165,623,1,0,0,0,167,630,1,0,0,0,
		169,635,1,0,0,0,171,641,1,0,0,0,173,646,1,0,0,0,175,653,1,0,0,0,177,659,
		1,0,0,0,179,663,1,0,0,0,181,668,1,0,0,0,183,675,1,0,0,0,185,679,1,0,0,
		0,187,685,1,0,0,0,189,699,1,0,0,0,191,703,1,0,0,0,193,705,1,0,0,0,195,
		724,1,0,0,0,197,726,1,0,0,0,199,728,1,0,0,0,201,731,1,0,0,0,203,776,1,
		0,0,0,205,778,1,0,0,0,207,801,1,0,0,0,209,805,1,0,0,0,211,807,1,0,0,0,
		213,825,1,0,0,0,215,839,1,0,0,0,217,218,5,40,0,0,218,2,1,0,0,0,219,220,
		5,41,0,0,220,4,1,0,0,0,221,222,5,58,0,0,222,223,5,61,0,0,223,6,1,0,0,0,
		224,225,5,61,0,0,225,226,5,62,0,0,226,8,1,0,0,0,227,228,5,58,0,0,228,10,
		1,0,0,0,229,230,5,44,0,0,230,12,1,0,0,0,231,232,5,46,0,0,232,14,1,0,0,
		0,233,234,5,46,0,0,234,235,5,46,0,0,235,236,5,46,0,0,236,16,1,0,0,0,237,
		238,5,59,0,0,238,18,1,0,0,0,239,240,5,63,0,0,240,20,1,0,0,0,241,242,5,
		91,0,0,242,22,1,0,0,0,243,244,5,93,0,0,244,24,1,0,0,0,245,246,5,91,0,0,
		246,247,5,93,0,0,247,26,1,0,0,0,248,249,5,43,0,0,249,28,1,0,0,0,250,251,
		5,45,0,0,251,30,1,0,0,0,252,253,5,42,0,0,253,32,1,0,0,0,254,255,5,47,0,
		0,255,34,1,0,0,0,256,257,5,37,0,0,257,36,1,0,0,0,258,259,5,61,0,0,259,
		38,1,0,0,0,260,261,5,60,0,0,261,262,5,62,0,0,262,40,1,0,0,0,263,264,5,
		62,0,0,264,42,1,0,0,0,265,266,5,62,0,0,266,267,5,61,0,0,267,44,1,0,0,0,
		268,269,5,60,0,0,269,46,1,0,0,0,270,271,5,60,0,0,271,272,5,61,0,0,272,
		48,1,0,0,0,273,274,5,124,0,0,274,275,5,124,0,0,275,50,1,0,0,0,276,277,
		5,60,0,0,277,278,5,60,0,0,278,52,1,0,0,0,279,280,5,62,0,0,280,281,5,62,
		0,0,281,54,1,0,0,0,282,283,5,58,0,0,283,284,5,58,0,0,284,56,1,0,0,0,285,
		286,7,0,0,0,286,287,7,1,0,0,287,288,7,2,0,0,288,289,7,3,0,0,289,290,7,
		4,0,0,290,291,7,3,0,0,291,292,7,5,0,0,292,58,1,0,0,0,293,294,7,6,0,0,294,
		295,7,2,0,0,295,296,7,5,0,0,296,297,7,0,0,0,297,298,7,1,0,0,298,299,7,
		4,0,0,299,60,1,0,0,0,300,301,7,7,0,0,301,302,7,8,0,0,302,303,7,9,0,0,303,
		304,7,10,0,0,304,305,7,2,0,0,305,62,1,0,0,0,306,307,7,2,0,0,307,308,7,
		0,0,0,308,309,7,11,0,0,309,310,7,3,0,0,310,311,7,6,0,0,311,312,7,2,0,0,
		312,313,7,10,0,0,313,314,7,11,0,0,314,315,7,12,0,0,315,64,1,0,0,0,316,
		317,7,13,0,0,317,318,7,9,0,0,318,319,7,9,0,0,319,320,7,8,0,0,320,321,7,
		3,0,0,321,322,7,10,0,0,322,323,7,1,0,0,323,66,1,0,0,0,324,325,7,1,0,0,
		325,326,7,14,0,0,326,327,7,11,0,0,327,328,7,3,0,0,328,329,7,5,0,0,329,
		330,7,0,0,0,330,331,7,15,0,0,331,68,1,0,0,0,332,333,7,9,0,0,333,334,7,
		13,0,0,334,335,7,16,0,0,335,336,7,3,0,0,336,337,7,15,0,0,337,338,7,2,0,
		0,338,70,1,0,0,0,339,340,7,10,0,0,340,341,7,1,0,0,341,342,7,17,0,0,342,
		72,1,0,0,0,343,344,7,10,0,0,344,345,7,1,0,0,345,346,7,18,0,0,346,74,1,
		0,0,0,347,348,7,10,0,0,348,349,7,6,0,0,349,76,1,0,0,0,350,351,7,13,0,0,
		351,352,7,17,0,0,352,78,1,0,0,0,353,354,7,15,0,0,354,355,7,10,0,0,355,
		356,7,6,0,0,356,357,7,2,0,0,357,80,1,0,0,0,358,359,7,18,0,0,359,360,7,
		3,0,0,360,361,7,7,0,0,361,362,7,10,0,0,362,363,7,14,0,0,363,364,7,8,0,
		0,364,365,7,2,0,0,365,82,1,0,0,0,366,367,7,3,0,0,367,368,7,19,0,0,368,
		369,7,0,0,0,369,370,7,6,0,0,370,371,7,2,0,0,371,372,7,6,0,0,372,84,1,0,
		0,0,373,374,7,7,0,0,374,375,7,10,0,0,375,376,7,8,0,0,376,377,7,6,0,0,377,
		378,7,3,0,0,378,86,1,0,0,0,379,380,7,7,0,0,380,381,7,5,0,0,381,382,7,9,
		0,0,382,383,7,11,0,0,383,88,1,0,0,0,384,385,7,0,0,0,385,386,7,1,0,0,386,
		90,1,0,0,0,387,388,7,0,0,0,388,389,7,6,0,0,389,92,1,0,0,0,390,391,7,8,
		0,0,391,392,7,0,0,0,392,393,7,20,0,0,393,394,7,3,0,0,394,94,1,0,0,0,395,
		396,7,1,0,0,396,397,7,9,0,0,397,398,7,2,0,0,398,96,1,0,0,0,399,400,7,1,
		0,0,400,401,7,14,0,0,401,402,7,8,0,0,402,403,7,8,0,0,403,98,1,0,0,0,404,
		405,7,9,0,0,405,406,7,1,0,0,406,100,1,0,0,0,407,408,7,9,0,0,408,409,7,
		1,0,0,409,410,7,8,0,0,410,411,7,17,0,0,411,102,1,0,0,0,412,413,7,9,0,0,
		413,414,7,5,0,0,414,104,1,0,0,0,415,416,7,6,0,0,416,417,7,9,0,0,417,418,
		7,11,0,0,418,419,7,3,0,0,419,106,1,0,0,0,420,421,7,2,0,0,421,422,7,9,0,
		0,422,108,1,0,0,0,423,424,7,2,0,0,424,425,7,5,0,0,425,426,7,14,0,0,426,
		427,7,3,0,0,427,110,1,0,0,0,428,429,7,21,0,0,429,430,7,9,0,0,430,431,7,
		0,0,0,431,432,7,18,0,0,432,112,1,0,0,0,433,434,7,2,0,0,434,435,7,5,0,0,
		435,436,7,0,0,0,436,437,7,11,0,0,437,114,1,0,0,0,438,439,7,8,0,0,439,440,
		7,3,0,0,440,441,7,10,0,0,441,442,7,18,0,0,442,443,7,0,0,0,443,444,7,1,
		0,0,444,445,7,4,0,0,445,116,1,0,0,0,446,447,7,2,0,0,447,448,7,5,0,0,448,
		449,7,10,0,0,449,450,7,0,0,0,450,451,7,8,0,0,451,452,7,0,0,0,452,453,7,
		1,0,0,453,454,7,4,0,0,454,118,1,0,0,0,455,456,7,13,0,0,456,457,7,9,0,0,
		457,458,7,2,0,0,458,459,7,22,0,0,459,120,1,0,0,0,460,461,7,15,0,0,461,
		462,7,14,0,0,462,463,7,5,0,0,463,464,7,5,0,0,464,465,7,3,0,0,465,466,7,
		1,0,0,466,467,7,2,0,0,467,468,5,95,0,0,468,469,7,18,0,0,469,470,7,10,0,
		0,470,471,7,2,0,0,471,472,7,3,0,0,472,122,1,0,0,0,473,474,7,15,0,0,474,
		475,7,14,0,0,475,476,7,5,0,0,476,477,7,5,0,0,477,478,7,3,0,0,478,479,7,
		1,0,0,479,480,7,2,0,0,480,481,5,95,0,0,481,482,7,2,0,0,482,483,7,0,0,0,
		483,484,7,11,0,0,484,485,7,3,0,0,485,486,7,6,0,0,486,487,7,2,0,0,487,488,
		7,10,0,0,488,489,7,11,0,0,489,490,7,12,0,0,490,124,1,0,0,0,491,492,7,0,
		0,0,492,493,7,1,0,0,493,494,7,2,0,0,494,495,7,3,0,0,495,496,7,5,0,0,496,
		497,7,21,0,0,497,498,7,10,0,0,498,499,7,8,0,0,499,126,1,0,0,0,500,501,
		7,17,0,0,501,502,7,3,0,0,502,503,7,10,0,0,503,504,7,5,0,0,504,128,1,0,
		0,0,505,506,7,11,0,0,506,507,7,9,0,0,507,508,7,1,0,0,508,509,7,2,0,0,509,
		510,7,22,0,0,510,130,1,0,0,0,511,512,7,18,0,0,512,513,7,10,0,0,513,514,
		7,17,0,0,514,132,1,0,0,0,515,516,7,22,0,0,516,517,7,9,0,0,517,518,7,14,
		0,0,518,519,7,5,0,0,519,134,1,0,0,0,520,521,7,11,0,0,521,522,7,0,0,0,522,
		523,7,1,0,0,523,524,7,14,0,0,524,525,7,2,0,0,525,526,7,3,0,0,526,136,1,
		0,0,0,527,528,7,6,0,0,528,529,7,3,0,0,529,530,7,15,0,0,530,531,7,9,0,0,
		531,532,7,1,0,0,532,533,7,18,0,0,533,138,1,0,0,0,534,535,7,11,0,0,535,
		536,7,0,0,0,536,537,7,8,0,0,537,538,7,8,0,0,538,539,7,0,0,0,539,540,7,
		6,0,0,540,541,7,3,0,0,541,542,7,15,0,0,542,543,7,9,0,0,543,544,7,1,0,0,
		544,545,7,18,0,0,545,140,1,0,0,0,546,547,7,12,0,0,547,548,7,9,0,0,548,
		549,7,6,0,0,549,550,7,0,0,0,550,551,7,2,0,0,551,552,7,0,0,0,552,553,7,
		9,0,0,553,554,7,1,0,0,554,142,1,0,0,0,555,556,7,3,0,0,556,557,7,19,0,0,
		557,558,7,2,0,0,558,559,7,5,0,0,559,560,7,10,0,0,560,561,7,15,0,0,561,
		562,7,2,0,0,562,144,1,0,0,0,563,564,7,3,0,0,564,565,7,15,0,0,565,566,7,
		22,0,0,566,567,7,9,0,0,567,146,1,0,0,0,568,569,7,10,0,0,569,570,7,8,0,
		0,570,571,7,8,0,0,571,148,1,0,0,0,572,573,7,10,0,0,573,574,7,6,0,0,574,
		575,7,15,0,0,575,150,1,0,0,0,576,577,7,13,0,0,577,578,7,3,0,0,578,579,
		7,2,0,0,579,580,7,23,0,0,580,581,7,3,0,0,581,582,7,3,0,0,582,583,7,1,0,
		0,583,152,1,0,0,0,584,585,7,18,0,0,585,586,7,3,0,0,586,587,7,6,0,0,587,
		588,7,15,0,0,588,154,1,0,0,0,589,590,7,18,0,0,590,591,7,0,0,0,591,592,
		7,6,0,0,592,593,7,2,0,0,593,594,7,0,0,0,594,595,7,1,0,0,595,596,7,15,0,
		0,596,597,7,2,0,0,597,156,1,0,0,0,598,599,7,7,0,0,599,600,7,3,0,0,600,
		601,7,2,0,0,601,602,7,15,0,0,602,603,7,22,0,0,603,158,1,0,0,0,604,605,
		7,7,0,0,605,606,7,0,0,0,606,607,7,5,0,0,607,608,7,6,0,0,608,609,7,2,0,
		0,609,160,1,0,0,0,610,611,7,7,0,0,611,612,7,9,0,0,612,613,7,5,0,0,613,
		614,7,11,0,0,614,615,7,10,0,0,615,616,7,2,0,0,616,162,1,0,0,0,617,618,
		7,4,0,0,618,619,7,5,0,0,619,620,7,9,0,0,620,621,7,14,0,0,621,622,7,12,
		0,0,622,164,1,0,0,0,623,624,7,22,0,0,624,625,7,10,0,0,625,626,7,21,0,0,
		626,627,7,0,0,0,627,628,7,1,0,0,628,629,7,4,0,0,629,166,1,0,0,0,630,631,
		7,0,0,0,631,632,7,1,0,0,632,633,7,2,0,0,633,634,7,9,0,0,634,168,1,0,0,
		0,635,636,7,8,0,0,636,637,7,0,0,0,637,638,7,11,0,0,638,639,7,0,0,0,639,
		640,7,2,0,0,640,170,1,0,0,0,641,642,7,1,0,0,642,643,7,3,0,0,643,644,7,
		19,0,0,644,645,7,2,0,0,645,172,1,0,0,0,646,647,7,9,0,0,647,648,7,7,0,0,
		648,649,7,7,0,0,649,650,7,6,0,0,650,651,7,3,0,0,651,652,7,2,0,0,652,174,
		1,0,0,0,653,654,7,9,0,0,654,655,7,5,0,0,655,656,7,18,0,0,656,657,7,3,0,
		0,657,658,7,5,0,0,658,176,1,0,0,0,659,660,7,5,0,0,660,661,7,9,0,0,661,
		662,7,23,0,0,662,178,1,0,0,0,663,664,7,5,0,0,664,665,7,9,0,0,665,666,7,
		23,0,0,666,667,7,6,0,0,667,180,1,0,0,0,668,669,7,6,0,0,669,670,7,3,0,0,
		670,671,7,8,0,0,671,672,7,3,0,0,672,673,7,15,0,0,673,674,7,2,0,0,674,182,
		1,0,0,0,675,676,7,2,0,0,676,677,7,9,0,0,677,678,7,12,0,0,678,184,1,0,0,
		0,679,680,7,14,0,0,680,681,7,1,0,0,681,682,7,0,0,0,682,683,7,9,0,0,683,
		684,7,1,0,0,684,186,1,0,0,0,685,686,7,23,0,0,686,687,7,22,0,0,687,688,
		7,3,0,0,688,689,7,5,0,0,689,690,7,3,0,0,690,188,1,0,0,0,691,700,3,57,28,
		0,692,700,3,59,29,0,693,700,3,61,30,0,694,700,3,63,31,0,695,700,3,65,32,
		0,696,700,3,67,33,0,697,700,3,69,34,0,698,700,3,71,35,0,699,691,1,0,0,
		0,699,692,1,0,0,0,699,693,1,0,0,0,699,694,1,0,0,0,699,695,1,0,0,0,699,
		696,1,0,0,0,699,697,1,0,0,0,699,698,1,0,0,0,700,190,1,0,0,0,701,704,3,
		193,96,0,702,704,7,24,0,0,703,701,1,0,0,0,703,702,1,0,0,0,704,192,1,0,
		0,0,705,706,7,25,0,0,706,194,1,0,0,0,707,711,3,193,96,0,708,710,3,191,
		95,0,709,708,1,0,0,0,710,713,1,0,0,0,711,709,1,0,0,0,711,712,1,0,0,0,712,
		725,1,0,0,0,713,711,1,0,0,0,714,720,5,91,0,0,715,719,8,26,0,0,716,717,
		5,93,0,0,717,719,5,93,0,0,718,715,1,0,0,0,718,716,1,0,0,0,719,722,1,0,
		0,0,720,718,1,0,0,0,720,721,1,0,0,0,721,723,1,0,0,0,722,720,1,0,0,0,723,
		725,5,93,0,0,724,707,1,0,0,0,724,714,1,0,0,0,725,196,1,0,0,0,726,727,7,
		27,0,0,727,198,1,0,0,0,728,729,7,28,0,0,729,200,1,0,0,0,730,732,3,199,
		99,0,731,730,1,0,0,0,732,733,1,0,0,0,733,731,1,0,0,0,733,734,1,0,0,0,734,
		202,1,0,0,0,735,737,3,199,99,0,736,735,1,0,0,0,737,738,1,0,0,0,738,736,
		1,0,0,0,738,739,1,0,0,0,739,747,1,0,0,0,740,744,5,46,0,0,741,743,3,199,
		99,0,742,741,1,0,0,0,743,746,1,0,0,0,744,742,1,0,0,0,744,745,1,0,0,0,745,
		748,1,0,0,0,746,744,1,0,0,0,747,740,1,0,0,0,747,748,1,0,0,0,748,756,1,
		0,0,0,749,751,5,46,0,0,750,752,3,199,99,0,751,750,1,0,0,0,752,753,1,0,
		0,0,753,751,1,0,0,0,753,754,1,0,0,0,754,756,1,0,0,0,755,736,1,0,0,0,755,
		749,1,0,0,0,756,766,1,0,0,0,757,759,7,3,0,0,758,760,7,29,0,0,759,758,1,
		0,0,0,759,760,1,0,0,0,760,762,1,0,0,0,761,763,3,199,99,0,762,761,1,0,0,
		0,763,764,1,0,0,0,764,762,1,0,0,0,764,765,1,0,0,0,765,767,1,0,0,0,766,
		757,1,0,0,0,766,767,1,0,0,0,767,777,1,0,0,0,768,769,5,48,0,0,769,770,7,
		19,0,0,770,772,1,0,0,0,771,773,3,197,98,0,772,771,1,0,0,0,773,774,1,0,
		0,0,774,772,1,0,0,0,774,775,1,0,0,0,775,777,1,0,0,0,776,755,1,0,0,0,776,
		768,1,0,0,0,777,204,1,0,0,0,778,779,3,203,101,0,779,780,7,11,0,0,780,206,
		1,0,0,0,781,787,5,39,0,0,782,786,8,30,0,0,783,784,5,39,0,0,784,786,5,39,
		0,0,785,782,1,0,0,0,785,783,1,0,0,0,786,789,1,0,0,0,787,785,1,0,0,0,787,
		788,1,0,0,0,788,790,1,0,0,0,789,787,1,0,0,0,790,802,5,39,0,0,791,797,5,
		34,0,0,792,796,8,31,0,0,793,794,5,34,0,0,794,796,5,34,0,0,795,792,1,0,
		0,0,795,793,1,0,0,0,796,799,1,0,0,0,797,795,1,0,0,0,797,798,1,0,0,0,798,
		800,1,0,0,0,799,797,1,0,0,0,800,802,5,34,0,0,801,781,1,0,0,0,801,791,1,
		0,0,0,802,208,1,0,0,0,803,806,3,109,54,0,804,806,3,85,42,0,805,803,1,0,
		0,0,805,804,1,0,0,0,806,210,1,0,0,0,807,808,5,45,0,0,808,809,5,45,0,0,
		809,813,1,0,0,0,810,812,8,32,0,0,811,810,1,0,0,0,812,815,1,0,0,0,813,811,
		1,0,0,0,813,814,1,0,0,0,814,821,1,0,0,0,815,813,1,0,0,0,816,818,5,13,0,
		0,817,816,1,0,0,0,817,818,1,0,0,0,818,819,1,0,0,0,819,822,5,10,0,0,820,
		822,5,0,0,1,821,817,1,0,0,0,821,820,1,0,0,0,822,823,1,0,0,0,823,824,6,
		105,0,0,824,212,1,0,0,0,825,826,5,47,0,0,826,827,5,42,0,0,827,831,1,0,
		0,0,828,830,9,0,0,0,829,828,1,0,0,0,830,833,1,0,0,0,831,832,1,0,0,0,831,
		829,1,0,0,0,832,834,1,0,0,0,833,831,1,0,0,0,834,835,5,42,0,0,835,836,5,
		47,0,0,836,837,1,0,0,0,837,838,6,106,0,0,838,214,1,0,0,0,839,840,7,33,
		0,0,840,841,1,0,0,0,841,842,6,107,0,0,842,216,1,0,0,0,28,0,699,703,711,
		718,720,724,733,738,744,747,753,755,759,764,766,774,776,785,787,795,797,
		801,805,813,817,821,831,1,0,1,0
	};

	public static readonly ATN _ATN =
		new ATNDeserializer().Deserialize(_serializedATN);


}
} // namespace QueryCat.Backend.Parser
