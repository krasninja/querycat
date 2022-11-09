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
		FROM=44, IN=45, IS=46, LIKE=47, NOT=48, NULL=49, ONLY=50, OR=51, SOME=52, 
		TO=53, TRUE=54, VOID=55, TRIM=56, LEADING=57, TRAILING=58, BOTH=59, CURRENT_DATE=60, 
		CURRENT_TIMESTAMP=61, INTERVAL=62, YEAR=63, MONTH=64, DAY=65, HOUR=66, 
		MINUTE=67, SECOND=68, MILLISECOND=69, POSITION=70, EXTRACT=71, ECHO=72, 
		ALL=73, ASC=74, BETWEEN=75, DESC=76, DISTINCT=77, FETCH=78, FIRST=79, 
		FORMAT=80, GROUP=81, HAVING=82, INTO=83, LIMIT=84, NEXT=85, OFFSET=86, 
		ORDER=87, ROW=88, ROWS=89, SELECT=90, TOP=91, UNION=92, WHERE=93, TYPE=94, 
		IDENTIFIER=95, INTEGER_LITERAL=96, FLOAT_LITERAL=97, NUMERIC_LITERAL=98, 
		STRING_LITERAL=99, BOOLEAN_LITERAL=100, SINGLE_LINE_COMMENT=101, MULTILINE_COMMENT=102, 
		SPACES=103;
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
		"NOT", "NULL", "ONLY", "OR", "SOME", "TO", "TRUE", "VOID", "TRIM", "LEADING", 
		"TRAILING", "BOTH", "CURRENT_DATE", "CURRENT_TIMESTAMP", "INTERVAL", "YEAR", 
		"MONTH", "DAY", "HOUR", "MINUTE", "SECOND", "MILLISECOND", "POSITION", 
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
		"'FALSE'", "'FROM'", "'IN'", "'IS'", "'LIKE'", "'NOT'", "'NULL'", "'ONLY'", 
		"'OR'", "'SOME'", "'TO'", "'TRUE'", "'VOID'", "'TRIM'", "'LEADING'", "'TRAILING'", 
		"'BOTH'", "'CURRENT_DATE'", "'CURRENT_TIMESTAMP'", "'INTERVAL'", "'YEAR'", 
		"'MONTH'", "'DAY'", "'HOUR'", "'MINUTE'", "'SECOND'", "'MILLISECOND'", 
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
		"NOT", "NULL", "ONLY", "OR", "SOME", "TO", "TRUE", "VOID", "TRIM", "LEADING", 
		"TRAILING", "BOTH", "CURRENT_DATE", "CURRENT_TIMESTAMP", "INTERVAL", "YEAR", 
		"MONTH", "DAY", "HOUR", "MINUTE", "SECOND", "MILLISECOND", "POSITION", 
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
		4,0,103,838,6,-1,2,0,7,0,2,1,7,1,2,2,7,2,2,3,7,3,2,4,7,4,2,5,7,5,2,6,7,
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
		104,2,105,7,105,2,106,7,106,1,0,1,0,1,1,1,1,1,2,1,2,1,2,1,3,1,3,1,3,1,
		4,1,4,1,5,1,5,1,6,1,6,1,7,1,7,1,7,1,7,1,8,1,8,1,9,1,9,1,10,1,10,1,11,1,
		11,1,12,1,12,1,12,1,13,1,13,1,14,1,14,1,15,1,15,1,16,1,16,1,17,1,17,1,
		18,1,18,1,19,1,19,1,19,1,20,1,20,1,21,1,21,1,21,1,22,1,22,1,23,1,23,1,
		23,1,24,1,24,1,24,1,25,1,25,1,25,1,26,1,26,1,26,1,27,1,27,1,27,1,28,1,
		28,1,28,1,28,1,28,1,28,1,28,1,28,1,29,1,29,1,29,1,29,1,29,1,29,1,29,1,
		30,1,30,1,30,1,30,1,30,1,30,1,31,1,31,1,31,1,31,1,31,1,31,1,31,1,31,1,
		31,1,31,1,32,1,32,1,32,1,32,1,32,1,32,1,32,1,32,1,33,1,33,1,33,1,33,1,
		33,1,33,1,33,1,33,1,34,1,34,1,34,1,34,1,34,1,34,1,34,1,35,1,35,1,35,1,
		35,1,36,1,36,1,36,1,36,1,37,1,37,1,37,1,38,1,38,1,38,1,39,1,39,1,39,1,
		39,1,39,1,40,1,40,1,40,1,40,1,40,1,40,1,40,1,40,1,41,1,41,1,41,1,41,1,
		41,1,41,1,41,1,42,1,42,1,42,1,42,1,42,1,42,1,43,1,43,1,43,1,43,1,43,1,
		44,1,44,1,44,1,45,1,45,1,45,1,46,1,46,1,46,1,46,1,46,1,47,1,47,1,47,1,
		47,1,48,1,48,1,48,1,48,1,48,1,49,1,49,1,49,1,49,1,49,1,50,1,50,1,50,1,
		51,1,51,1,51,1,51,1,51,1,52,1,52,1,52,1,53,1,53,1,53,1,53,1,53,1,54,1,
		54,1,54,1,54,1,54,1,55,1,55,1,55,1,55,1,55,1,56,1,56,1,56,1,56,1,56,1,
		56,1,56,1,56,1,57,1,57,1,57,1,57,1,57,1,57,1,57,1,57,1,57,1,58,1,58,1,
		58,1,58,1,58,1,59,1,59,1,59,1,59,1,59,1,59,1,59,1,59,1,59,1,59,1,59,1,
		59,1,59,1,60,1,60,1,60,1,60,1,60,1,60,1,60,1,60,1,60,1,60,1,60,1,60,1,
		60,1,60,1,60,1,60,1,60,1,60,1,61,1,61,1,61,1,61,1,61,1,61,1,61,1,61,1,
		61,1,62,1,62,1,62,1,62,1,62,1,63,1,63,1,63,1,63,1,63,1,63,1,64,1,64,1,
		64,1,64,1,65,1,65,1,65,1,65,1,65,1,66,1,66,1,66,1,66,1,66,1,66,1,66,1,
		67,1,67,1,67,1,67,1,67,1,67,1,67,1,68,1,68,1,68,1,68,1,68,1,68,1,68,1,
		68,1,68,1,68,1,68,1,68,1,69,1,69,1,69,1,69,1,69,1,69,1,69,1,69,1,69,1,
		70,1,70,1,70,1,70,1,70,1,70,1,70,1,70,1,71,1,71,1,71,1,71,1,71,1,72,1,
		72,1,72,1,72,1,73,1,73,1,73,1,73,1,74,1,74,1,74,1,74,1,74,1,74,1,74,1,
		74,1,75,1,75,1,75,1,75,1,75,1,76,1,76,1,76,1,76,1,76,1,76,1,76,1,76,1,
		76,1,77,1,77,1,77,1,77,1,77,1,77,1,78,1,78,1,78,1,78,1,78,1,78,1,79,1,
		79,1,79,1,79,1,79,1,79,1,79,1,80,1,80,1,80,1,80,1,80,1,80,1,81,1,81,1,
		81,1,81,1,81,1,81,1,81,1,82,1,82,1,82,1,82,1,82,1,83,1,83,1,83,1,83,1,
		83,1,83,1,84,1,84,1,84,1,84,1,84,1,85,1,85,1,85,1,85,1,85,1,85,1,85,1,
		86,1,86,1,86,1,86,1,86,1,86,1,87,1,87,1,87,1,87,1,88,1,88,1,88,1,88,1,
		88,1,89,1,89,1,89,1,89,1,89,1,89,1,89,1,90,1,90,1,90,1,90,1,91,1,91,1,
		91,1,91,1,91,1,91,1,92,1,92,1,92,1,92,1,92,1,92,1,93,1,93,1,93,1,93,1,
		93,1,93,1,93,1,93,3,93,695,8,93,1,94,1,94,3,94,699,8,94,1,95,1,95,1,96,
		1,96,5,96,705,8,96,10,96,12,96,708,9,96,1,96,1,96,1,96,1,96,5,96,714,8,
		96,10,96,12,96,717,9,96,1,96,3,96,720,8,96,1,97,1,97,1,98,1,98,1,99,4,
		99,727,8,99,11,99,12,99,728,1,100,4,100,732,8,100,11,100,12,100,733,1,
		100,1,100,5,100,738,8,100,10,100,12,100,741,9,100,3,100,743,8,100,1,100,
		1,100,4,100,747,8,100,11,100,12,100,748,3,100,751,8,100,1,100,1,100,3,
		100,755,8,100,1,100,4,100,758,8,100,11,100,12,100,759,3,100,762,8,100,
		1,100,1,100,1,100,1,100,4,100,768,8,100,11,100,12,100,769,3,100,772,8,
		100,1,101,1,101,1,101,1,102,1,102,1,102,1,102,5,102,781,8,102,10,102,12,
		102,784,9,102,1,102,1,102,1,102,1,102,1,102,5,102,791,8,102,10,102,12,
		102,794,9,102,1,102,3,102,797,8,102,1,103,1,103,3,103,801,8,103,1,104,
		1,104,1,104,1,104,5,104,807,8,104,10,104,12,104,810,9,104,1,104,3,104,
		813,8,104,1,104,1,104,3,104,817,8,104,1,104,1,104,1,105,1,105,1,105,1,
		105,5,105,825,8,105,10,105,12,105,828,9,105,1,105,1,105,1,105,1,105,1,
		105,1,106,1,106,1,106,1,106,1,826,0,107,1,1,3,2,5,3,7,4,9,5,11,6,13,7,
		15,8,17,9,19,10,21,11,23,12,25,13,27,14,29,15,31,16,33,17,35,18,37,19,
		39,20,41,21,43,22,45,23,47,24,49,25,51,26,53,27,55,28,57,29,59,30,61,31,
		63,32,65,33,67,34,69,35,71,36,73,37,75,38,77,39,79,40,81,41,83,42,85,43,
		87,44,89,45,91,46,93,47,95,48,97,49,99,50,101,51,103,52,105,53,107,54,
		109,55,111,56,113,57,115,58,117,59,119,60,121,61,123,62,125,63,127,64,
		129,65,131,66,133,67,135,68,137,69,139,70,141,71,143,72,145,73,147,74,
		149,75,151,76,153,77,155,78,157,79,159,80,161,81,163,82,165,83,167,84,
		169,85,171,86,173,87,175,88,177,89,179,90,181,91,183,92,185,93,187,94,
		189,0,191,0,193,95,195,0,197,0,199,96,201,97,203,98,205,99,207,100,209,
		101,211,102,213,103,1,0,34,2,0,73,73,105,105,2,0,78,78,110,110,2,0,84,
		84,116,116,2,0,69,69,101,101,2,0,71,71,103,103,2,0,82,82,114,114,2,0,83,
		83,115,115,2,0,70,70,102,102,2,0,76,76,108,108,2,0,79,79,111,111,2,0,65,
		65,97,97,2,0,77,77,109,109,2,0,80,80,112,112,2,0,66,66,98,98,2,0,85,85,
		117,117,2,0,67,67,99,99,2,0,74,74,106,106,2,0,89,89,121,121,2,0,68,68,
		100,100,2,0,88,88,120,120,2,0,75,75,107,107,2,0,86,86,118,118,2,0,72,72,
		104,104,2,0,87,87,119,119,5,0,48,57,95,95,183,183,768,879,8255,8256,14,
		0,65,90,95,95,97,122,192,214,224,246,248,767,880,893,895,8191,8204,8205,
		8304,8591,11264,12271,12289,55295,63744,64975,65008,65533,1,0,93,93,3,
		0,48,57,65,70,97,102,1,0,48,57,2,0,43,43,45,45,1,0,39,39,1,0,34,34,2,0,
		10,10,13,13,3,0,9,11,13,13,32,32,866,0,1,1,0,0,0,0,3,1,0,0,0,0,5,1,0,0,
		0,0,7,1,0,0,0,0,9,1,0,0,0,0,11,1,0,0,0,0,13,1,0,0,0,0,15,1,0,0,0,0,17,
		1,0,0,0,0,19,1,0,0,0,0,21,1,0,0,0,0,23,1,0,0,0,0,25,1,0,0,0,0,27,1,0,0,
		0,0,29,1,0,0,0,0,31,1,0,0,0,0,33,1,0,0,0,0,35,1,0,0,0,0,37,1,0,0,0,0,39,
		1,0,0,0,0,41,1,0,0,0,0,43,1,0,0,0,0,45,1,0,0,0,0,47,1,0,0,0,0,49,1,0,0,
		0,0,51,1,0,0,0,0,53,1,0,0,0,0,55,1,0,0,0,0,57,1,0,0,0,0,59,1,0,0,0,0,61,
		1,0,0,0,0,63,1,0,0,0,0,65,1,0,0,0,0,67,1,0,0,0,0,69,1,0,0,0,0,71,1,0,0,
		0,0,73,1,0,0,0,0,75,1,0,0,0,0,77,1,0,0,0,0,79,1,0,0,0,0,81,1,0,0,0,0,83,
		1,0,0,0,0,85,1,0,0,0,0,87,1,0,0,0,0,89,1,0,0,0,0,91,1,0,0,0,0,93,1,0,0,
		0,0,95,1,0,0,0,0,97,1,0,0,0,0,99,1,0,0,0,0,101,1,0,0,0,0,103,1,0,0,0,0,
		105,1,0,0,0,0,107,1,0,0,0,0,109,1,0,0,0,0,111,1,0,0,0,0,113,1,0,0,0,0,
		115,1,0,0,0,0,117,1,0,0,0,0,119,1,0,0,0,0,121,1,0,0,0,0,123,1,0,0,0,0,
		125,1,0,0,0,0,127,1,0,0,0,0,129,1,0,0,0,0,131,1,0,0,0,0,133,1,0,0,0,0,
		135,1,0,0,0,0,137,1,0,0,0,0,139,1,0,0,0,0,141,1,0,0,0,0,143,1,0,0,0,0,
		145,1,0,0,0,0,147,1,0,0,0,0,149,1,0,0,0,0,151,1,0,0,0,0,153,1,0,0,0,0,
		155,1,0,0,0,0,157,1,0,0,0,0,159,1,0,0,0,0,161,1,0,0,0,0,163,1,0,0,0,0,
		165,1,0,0,0,0,167,1,0,0,0,0,169,1,0,0,0,0,171,1,0,0,0,0,173,1,0,0,0,0,
		175,1,0,0,0,0,177,1,0,0,0,0,179,1,0,0,0,0,181,1,0,0,0,0,183,1,0,0,0,0,
		185,1,0,0,0,0,187,1,0,0,0,0,193,1,0,0,0,0,199,1,0,0,0,0,201,1,0,0,0,0,
		203,1,0,0,0,0,205,1,0,0,0,0,207,1,0,0,0,0,209,1,0,0,0,0,211,1,0,0,0,0,
		213,1,0,0,0,1,215,1,0,0,0,3,217,1,0,0,0,5,219,1,0,0,0,7,222,1,0,0,0,9,
		225,1,0,0,0,11,227,1,0,0,0,13,229,1,0,0,0,15,231,1,0,0,0,17,235,1,0,0,
		0,19,237,1,0,0,0,21,239,1,0,0,0,23,241,1,0,0,0,25,243,1,0,0,0,27,246,1,
		0,0,0,29,248,1,0,0,0,31,250,1,0,0,0,33,252,1,0,0,0,35,254,1,0,0,0,37,256,
		1,0,0,0,39,258,1,0,0,0,41,261,1,0,0,0,43,263,1,0,0,0,45,266,1,0,0,0,47,
		268,1,0,0,0,49,271,1,0,0,0,51,274,1,0,0,0,53,277,1,0,0,0,55,280,1,0,0,
		0,57,283,1,0,0,0,59,291,1,0,0,0,61,298,1,0,0,0,63,304,1,0,0,0,65,314,1,
		0,0,0,67,322,1,0,0,0,69,330,1,0,0,0,71,337,1,0,0,0,73,341,1,0,0,0,75,345,
		1,0,0,0,77,348,1,0,0,0,79,351,1,0,0,0,81,356,1,0,0,0,83,364,1,0,0,0,85,
		371,1,0,0,0,87,377,1,0,0,0,89,382,1,0,0,0,91,385,1,0,0,0,93,388,1,0,0,
		0,95,393,1,0,0,0,97,397,1,0,0,0,99,402,1,0,0,0,101,407,1,0,0,0,103,410,
		1,0,0,0,105,415,1,0,0,0,107,418,1,0,0,0,109,423,1,0,0,0,111,428,1,0,0,
		0,113,433,1,0,0,0,115,441,1,0,0,0,117,450,1,0,0,0,119,455,1,0,0,0,121,
		468,1,0,0,0,123,486,1,0,0,0,125,495,1,0,0,0,127,500,1,0,0,0,129,506,1,
		0,0,0,131,510,1,0,0,0,133,515,1,0,0,0,135,522,1,0,0,0,137,529,1,0,0,0,
		139,541,1,0,0,0,141,550,1,0,0,0,143,558,1,0,0,0,145,563,1,0,0,0,147,567,
		1,0,0,0,149,571,1,0,0,0,151,579,1,0,0,0,153,584,1,0,0,0,155,593,1,0,0,
		0,157,599,1,0,0,0,159,605,1,0,0,0,161,612,1,0,0,0,163,618,1,0,0,0,165,
		625,1,0,0,0,167,630,1,0,0,0,169,636,1,0,0,0,171,641,1,0,0,0,173,648,1,
		0,0,0,175,654,1,0,0,0,177,658,1,0,0,0,179,663,1,0,0,0,181,670,1,0,0,0,
		183,674,1,0,0,0,185,680,1,0,0,0,187,694,1,0,0,0,189,698,1,0,0,0,191,700,
		1,0,0,0,193,719,1,0,0,0,195,721,1,0,0,0,197,723,1,0,0,0,199,726,1,0,0,
		0,201,771,1,0,0,0,203,773,1,0,0,0,205,796,1,0,0,0,207,800,1,0,0,0,209,
		802,1,0,0,0,211,820,1,0,0,0,213,834,1,0,0,0,215,216,5,40,0,0,216,2,1,0,
		0,0,217,218,5,41,0,0,218,4,1,0,0,0,219,220,5,58,0,0,220,221,5,61,0,0,221,
		6,1,0,0,0,222,223,5,61,0,0,223,224,5,62,0,0,224,8,1,0,0,0,225,226,5,58,
		0,0,226,10,1,0,0,0,227,228,5,44,0,0,228,12,1,0,0,0,229,230,5,46,0,0,230,
		14,1,0,0,0,231,232,5,46,0,0,232,233,5,46,0,0,233,234,5,46,0,0,234,16,1,
		0,0,0,235,236,5,59,0,0,236,18,1,0,0,0,237,238,5,63,0,0,238,20,1,0,0,0,
		239,240,5,91,0,0,240,22,1,0,0,0,241,242,5,93,0,0,242,24,1,0,0,0,243,244,
		5,91,0,0,244,245,5,93,0,0,245,26,1,0,0,0,246,247,5,43,0,0,247,28,1,0,0,
		0,248,249,5,45,0,0,249,30,1,0,0,0,250,251,5,42,0,0,251,32,1,0,0,0,252,
		253,5,47,0,0,253,34,1,0,0,0,254,255,5,37,0,0,255,36,1,0,0,0,256,257,5,
		61,0,0,257,38,1,0,0,0,258,259,5,60,0,0,259,260,5,62,0,0,260,40,1,0,0,0,
		261,262,5,62,0,0,262,42,1,0,0,0,263,264,5,62,0,0,264,265,5,61,0,0,265,
		44,1,0,0,0,266,267,5,60,0,0,267,46,1,0,0,0,268,269,5,60,0,0,269,270,5,
		61,0,0,270,48,1,0,0,0,271,272,5,124,0,0,272,273,5,124,0,0,273,50,1,0,0,
		0,274,275,5,60,0,0,275,276,5,60,0,0,276,52,1,0,0,0,277,278,5,62,0,0,278,
		279,5,62,0,0,279,54,1,0,0,0,280,281,5,58,0,0,281,282,5,58,0,0,282,56,1,
		0,0,0,283,284,7,0,0,0,284,285,7,1,0,0,285,286,7,2,0,0,286,287,7,3,0,0,
		287,288,7,4,0,0,288,289,7,3,0,0,289,290,7,5,0,0,290,58,1,0,0,0,291,292,
		7,6,0,0,292,293,7,2,0,0,293,294,7,5,0,0,294,295,7,0,0,0,295,296,7,1,0,
		0,296,297,7,4,0,0,297,60,1,0,0,0,298,299,7,7,0,0,299,300,7,8,0,0,300,301,
		7,9,0,0,301,302,7,10,0,0,302,303,7,2,0,0,303,62,1,0,0,0,304,305,7,2,0,
		0,305,306,7,0,0,0,306,307,7,11,0,0,307,308,7,3,0,0,308,309,7,6,0,0,309,
		310,7,2,0,0,310,311,7,10,0,0,311,312,7,11,0,0,312,313,7,12,0,0,313,64,
		1,0,0,0,314,315,7,13,0,0,315,316,7,9,0,0,316,317,7,9,0,0,317,318,7,8,0,
		0,318,319,7,3,0,0,319,320,7,10,0,0,320,321,7,1,0,0,321,66,1,0,0,0,322,
		323,7,1,0,0,323,324,7,14,0,0,324,325,7,11,0,0,325,326,7,3,0,0,326,327,
		7,5,0,0,327,328,7,0,0,0,328,329,7,15,0,0,329,68,1,0,0,0,330,331,7,9,0,
		0,331,332,7,13,0,0,332,333,7,16,0,0,333,334,7,3,0,0,334,335,7,15,0,0,335,
		336,7,2,0,0,336,70,1,0,0,0,337,338,7,10,0,0,338,339,7,1,0,0,339,340,7,
		17,0,0,340,72,1,0,0,0,341,342,7,10,0,0,342,343,7,1,0,0,343,344,7,18,0,
		0,344,74,1,0,0,0,345,346,7,10,0,0,346,347,7,6,0,0,347,76,1,0,0,0,348,349,
		7,13,0,0,349,350,7,17,0,0,350,78,1,0,0,0,351,352,7,15,0,0,352,353,7,10,
		0,0,353,354,7,6,0,0,354,355,7,2,0,0,355,80,1,0,0,0,356,357,7,18,0,0,357,
		358,7,3,0,0,358,359,7,7,0,0,359,360,7,10,0,0,360,361,7,14,0,0,361,362,
		7,8,0,0,362,363,7,2,0,0,363,82,1,0,0,0,364,365,7,3,0,0,365,366,7,19,0,
		0,366,367,7,0,0,0,367,368,7,6,0,0,368,369,7,2,0,0,369,370,7,6,0,0,370,
		84,1,0,0,0,371,372,7,7,0,0,372,373,7,10,0,0,373,374,7,8,0,0,374,375,7,
		6,0,0,375,376,7,3,0,0,376,86,1,0,0,0,377,378,7,7,0,0,378,379,7,5,0,0,379,
		380,7,9,0,0,380,381,7,11,0,0,381,88,1,0,0,0,382,383,7,0,0,0,383,384,7,
		1,0,0,384,90,1,0,0,0,385,386,7,0,0,0,386,387,7,6,0,0,387,92,1,0,0,0,388,
		389,7,8,0,0,389,390,7,0,0,0,390,391,7,20,0,0,391,392,7,3,0,0,392,94,1,
		0,0,0,393,394,7,1,0,0,394,395,7,9,0,0,395,396,7,2,0,0,396,96,1,0,0,0,397,
		398,7,1,0,0,398,399,7,14,0,0,399,400,7,8,0,0,400,401,7,8,0,0,401,98,1,
		0,0,0,402,403,7,9,0,0,403,404,7,1,0,0,404,405,7,8,0,0,405,406,7,17,0,0,
		406,100,1,0,0,0,407,408,7,9,0,0,408,409,7,5,0,0,409,102,1,0,0,0,410,411,
		7,6,0,0,411,412,7,9,0,0,412,413,7,11,0,0,413,414,7,3,0,0,414,104,1,0,0,
		0,415,416,7,2,0,0,416,417,7,9,0,0,417,106,1,0,0,0,418,419,7,2,0,0,419,
		420,7,5,0,0,420,421,7,14,0,0,421,422,7,3,0,0,422,108,1,0,0,0,423,424,7,
		21,0,0,424,425,7,9,0,0,425,426,7,0,0,0,426,427,7,18,0,0,427,110,1,0,0,
		0,428,429,7,2,0,0,429,430,7,5,0,0,430,431,7,0,0,0,431,432,7,11,0,0,432,
		112,1,0,0,0,433,434,7,8,0,0,434,435,7,3,0,0,435,436,7,10,0,0,436,437,7,
		18,0,0,437,438,7,0,0,0,438,439,7,1,0,0,439,440,7,4,0,0,440,114,1,0,0,0,
		441,442,7,2,0,0,442,443,7,5,0,0,443,444,7,10,0,0,444,445,7,0,0,0,445,446,
		7,8,0,0,446,447,7,0,0,0,447,448,7,1,0,0,448,449,7,4,0,0,449,116,1,0,0,
		0,450,451,7,13,0,0,451,452,7,9,0,0,452,453,7,2,0,0,453,454,7,22,0,0,454,
		118,1,0,0,0,455,456,7,15,0,0,456,457,7,14,0,0,457,458,7,5,0,0,458,459,
		7,5,0,0,459,460,7,3,0,0,460,461,7,1,0,0,461,462,7,2,0,0,462,463,5,95,0,
		0,463,464,7,18,0,0,464,465,7,10,0,0,465,466,7,2,0,0,466,467,7,3,0,0,467,
		120,1,0,0,0,468,469,7,15,0,0,469,470,7,14,0,0,470,471,7,5,0,0,471,472,
		7,5,0,0,472,473,7,3,0,0,473,474,7,1,0,0,474,475,7,2,0,0,475,476,5,95,0,
		0,476,477,7,2,0,0,477,478,7,0,0,0,478,479,7,11,0,0,479,480,7,3,0,0,480,
		481,7,6,0,0,481,482,7,2,0,0,482,483,7,10,0,0,483,484,7,11,0,0,484,485,
		7,12,0,0,485,122,1,0,0,0,486,487,7,0,0,0,487,488,7,1,0,0,488,489,7,2,0,
		0,489,490,7,3,0,0,490,491,7,5,0,0,491,492,7,21,0,0,492,493,7,10,0,0,493,
		494,7,8,0,0,494,124,1,0,0,0,495,496,7,17,0,0,496,497,7,3,0,0,497,498,7,
		10,0,0,498,499,7,5,0,0,499,126,1,0,0,0,500,501,7,11,0,0,501,502,7,9,0,
		0,502,503,7,1,0,0,503,504,7,2,0,0,504,505,7,22,0,0,505,128,1,0,0,0,506,
		507,7,18,0,0,507,508,7,10,0,0,508,509,7,17,0,0,509,130,1,0,0,0,510,511,
		7,22,0,0,511,512,7,9,0,0,512,513,7,14,0,0,513,514,7,5,0,0,514,132,1,0,
		0,0,515,516,7,11,0,0,516,517,7,0,0,0,517,518,7,1,0,0,518,519,7,14,0,0,
		519,520,7,2,0,0,520,521,7,3,0,0,521,134,1,0,0,0,522,523,7,6,0,0,523,524,
		7,3,0,0,524,525,7,15,0,0,525,526,7,9,0,0,526,527,7,1,0,0,527,528,7,18,
		0,0,528,136,1,0,0,0,529,530,7,11,0,0,530,531,7,0,0,0,531,532,7,8,0,0,532,
		533,7,8,0,0,533,534,7,0,0,0,534,535,7,6,0,0,535,536,7,3,0,0,536,537,7,
		15,0,0,537,538,7,9,0,0,538,539,7,1,0,0,539,540,7,18,0,0,540,138,1,0,0,
		0,541,542,7,12,0,0,542,543,7,9,0,0,543,544,7,6,0,0,544,545,7,0,0,0,545,
		546,7,2,0,0,546,547,7,0,0,0,547,548,7,9,0,0,548,549,7,1,0,0,549,140,1,
		0,0,0,550,551,7,3,0,0,551,552,7,19,0,0,552,553,7,2,0,0,553,554,7,5,0,0,
		554,555,7,10,0,0,555,556,7,15,0,0,556,557,7,2,0,0,557,142,1,0,0,0,558,
		559,7,3,0,0,559,560,7,15,0,0,560,561,7,22,0,0,561,562,7,9,0,0,562,144,
		1,0,0,0,563,564,7,10,0,0,564,565,7,8,0,0,565,566,7,8,0,0,566,146,1,0,0,
		0,567,568,7,10,0,0,568,569,7,6,0,0,569,570,7,15,0,0,570,148,1,0,0,0,571,
		572,7,13,0,0,572,573,7,3,0,0,573,574,7,2,0,0,574,575,7,23,0,0,575,576,
		7,3,0,0,576,577,7,3,0,0,577,578,7,1,0,0,578,150,1,0,0,0,579,580,7,18,0,
		0,580,581,7,3,0,0,581,582,7,6,0,0,582,583,7,15,0,0,583,152,1,0,0,0,584,
		585,7,18,0,0,585,586,7,0,0,0,586,587,7,6,0,0,587,588,7,2,0,0,588,589,7,
		0,0,0,589,590,7,1,0,0,590,591,7,15,0,0,591,592,7,2,0,0,592,154,1,0,0,0,
		593,594,7,7,0,0,594,595,7,3,0,0,595,596,7,2,0,0,596,597,7,15,0,0,597,598,
		7,22,0,0,598,156,1,0,0,0,599,600,7,7,0,0,600,601,7,0,0,0,601,602,7,5,0,
		0,602,603,7,6,0,0,603,604,7,2,0,0,604,158,1,0,0,0,605,606,7,7,0,0,606,
		607,7,9,0,0,607,608,7,5,0,0,608,609,7,11,0,0,609,610,7,10,0,0,610,611,
		7,2,0,0,611,160,1,0,0,0,612,613,7,4,0,0,613,614,7,5,0,0,614,615,7,9,0,
		0,615,616,7,14,0,0,616,617,7,12,0,0,617,162,1,0,0,0,618,619,7,22,0,0,619,
		620,7,10,0,0,620,621,7,21,0,0,621,622,7,0,0,0,622,623,7,1,0,0,623,624,
		7,4,0,0,624,164,1,0,0,0,625,626,7,0,0,0,626,627,7,1,0,0,627,628,7,2,0,
		0,628,629,7,9,0,0,629,166,1,0,0,0,630,631,7,8,0,0,631,632,7,0,0,0,632,
		633,7,11,0,0,633,634,7,0,0,0,634,635,7,2,0,0,635,168,1,0,0,0,636,637,7,
		1,0,0,637,638,7,3,0,0,638,639,7,19,0,0,639,640,7,2,0,0,640,170,1,0,0,0,
		641,642,7,9,0,0,642,643,7,7,0,0,643,644,7,7,0,0,644,645,7,6,0,0,645,646,
		7,3,0,0,646,647,7,2,0,0,647,172,1,0,0,0,648,649,7,9,0,0,649,650,7,5,0,
		0,650,651,7,18,0,0,651,652,7,3,0,0,652,653,7,5,0,0,653,174,1,0,0,0,654,
		655,7,5,0,0,655,656,7,9,0,0,656,657,7,23,0,0,657,176,1,0,0,0,658,659,7,
		5,0,0,659,660,7,9,0,0,660,661,7,23,0,0,661,662,7,6,0,0,662,178,1,0,0,0,
		663,664,7,6,0,0,664,665,7,3,0,0,665,666,7,8,0,0,666,667,7,3,0,0,667,668,
		7,15,0,0,668,669,7,2,0,0,669,180,1,0,0,0,670,671,7,2,0,0,671,672,7,9,0,
		0,672,673,7,12,0,0,673,182,1,0,0,0,674,675,7,14,0,0,675,676,7,1,0,0,676,
		677,7,0,0,0,677,678,7,9,0,0,678,679,7,1,0,0,679,184,1,0,0,0,680,681,7,
		23,0,0,681,682,7,22,0,0,682,683,7,3,0,0,683,684,7,5,0,0,684,685,7,3,0,
		0,685,186,1,0,0,0,686,695,3,57,28,0,687,695,3,59,29,0,688,695,3,61,30,
		0,689,695,3,63,31,0,690,695,3,65,32,0,691,695,3,67,33,0,692,695,3,69,34,
		0,693,695,3,71,35,0,694,686,1,0,0,0,694,687,1,0,0,0,694,688,1,0,0,0,694,
		689,1,0,0,0,694,690,1,0,0,0,694,691,1,0,0,0,694,692,1,0,0,0,694,693,1,
		0,0,0,695,188,1,0,0,0,696,699,3,191,95,0,697,699,7,24,0,0,698,696,1,0,
		0,0,698,697,1,0,0,0,699,190,1,0,0,0,700,701,7,25,0,0,701,192,1,0,0,0,702,
		706,3,191,95,0,703,705,3,189,94,0,704,703,1,0,0,0,705,708,1,0,0,0,706,
		704,1,0,0,0,706,707,1,0,0,0,707,720,1,0,0,0,708,706,1,0,0,0,709,715,5,
		91,0,0,710,714,8,26,0,0,711,712,5,93,0,0,712,714,5,93,0,0,713,710,1,0,
		0,0,713,711,1,0,0,0,714,717,1,0,0,0,715,713,1,0,0,0,715,716,1,0,0,0,716,
		718,1,0,0,0,717,715,1,0,0,0,718,720,5,93,0,0,719,702,1,0,0,0,719,709,1,
		0,0,0,720,194,1,0,0,0,721,722,7,27,0,0,722,196,1,0,0,0,723,724,7,28,0,
		0,724,198,1,0,0,0,725,727,3,197,98,0,726,725,1,0,0,0,727,728,1,0,0,0,728,
		726,1,0,0,0,728,729,1,0,0,0,729,200,1,0,0,0,730,732,3,197,98,0,731,730,
		1,0,0,0,732,733,1,0,0,0,733,731,1,0,0,0,733,734,1,0,0,0,734,742,1,0,0,
		0,735,739,5,46,0,0,736,738,3,197,98,0,737,736,1,0,0,0,738,741,1,0,0,0,
		739,737,1,0,0,0,739,740,1,0,0,0,740,743,1,0,0,0,741,739,1,0,0,0,742,735,
		1,0,0,0,742,743,1,0,0,0,743,751,1,0,0,0,744,746,5,46,0,0,745,747,3,197,
		98,0,746,745,1,0,0,0,747,748,1,0,0,0,748,746,1,0,0,0,748,749,1,0,0,0,749,
		751,1,0,0,0,750,731,1,0,0,0,750,744,1,0,0,0,751,761,1,0,0,0,752,754,7,
		3,0,0,753,755,7,29,0,0,754,753,1,0,0,0,754,755,1,0,0,0,755,757,1,0,0,0,
		756,758,3,197,98,0,757,756,1,0,0,0,758,759,1,0,0,0,759,757,1,0,0,0,759,
		760,1,0,0,0,760,762,1,0,0,0,761,752,1,0,0,0,761,762,1,0,0,0,762,772,1,
		0,0,0,763,764,5,48,0,0,764,765,7,19,0,0,765,767,1,0,0,0,766,768,3,195,
		97,0,767,766,1,0,0,0,768,769,1,0,0,0,769,767,1,0,0,0,769,770,1,0,0,0,770,
		772,1,0,0,0,771,750,1,0,0,0,771,763,1,0,0,0,772,202,1,0,0,0,773,774,3,
		201,100,0,774,775,7,11,0,0,775,204,1,0,0,0,776,782,5,39,0,0,777,781,8,
		30,0,0,778,779,5,39,0,0,779,781,5,39,0,0,780,777,1,0,0,0,780,778,1,0,0,
		0,781,784,1,0,0,0,782,780,1,0,0,0,782,783,1,0,0,0,783,785,1,0,0,0,784,
		782,1,0,0,0,785,797,5,39,0,0,786,792,5,34,0,0,787,791,8,31,0,0,788,789,
		5,34,0,0,789,791,5,34,0,0,790,787,1,0,0,0,790,788,1,0,0,0,791,794,1,0,
		0,0,792,790,1,0,0,0,792,793,1,0,0,0,793,795,1,0,0,0,794,792,1,0,0,0,795,
		797,5,34,0,0,796,776,1,0,0,0,796,786,1,0,0,0,797,206,1,0,0,0,798,801,3,
		107,53,0,799,801,3,85,42,0,800,798,1,0,0,0,800,799,1,0,0,0,801,208,1,0,
		0,0,802,803,5,45,0,0,803,804,5,45,0,0,804,808,1,0,0,0,805,807,8,32,0,0,
		806,805,1,0,0,0,807,810,1,0,0,0,808,806,1,0,0,0,808,809,1,0,0,0,809,816,
		1,0,0,0,810,808,1,0,0,0,811,813,5,13,0,0,812,811,1,0,0,0,812,813,1,0,0,
		0,813,814,1,0,0,0,814,817,5,10,0,0,815,817,5,0,0,1,816,812,1,0,0,0,816,
		815,1,0,0,0,817,818,1,0,0,0,818,819,6,104,0,0,819,210,1,0,0,0,820,821,
		5,47,0,0,821,822,5,42,0,0,822,826,1,0,0,0,823,825,9,0,0,0,824,823,1,0,
		0,0,825,828,1,0,0,0,826,827,1,0,0,0,826,824,1,0,0,0,827,829,1,0,0,0,828,
		826,1,0,0,0,829,830,5,42,0,0,830,831,5,47,0,0,831,832,1,0,0,0,832,833,
		6,105,0,0,833,212,1,0,0,0,834,835,7,33,0,0,835,836,1,0,0,0,836,837,6,106,
		0,0,837,214,1,0,0,0,28,0,694,698,706,713,715,719,728,733,739,742,748,750,
		754,759,761,769,771,780,782,790,792,796,800,808,812,816,826,1,0,1,0
	};

	public static readonly ATN _ATN =
		new ATNDeserializer().Deserialize(_serializedATN);


}
} // namespace QueryCat.Backend.Parser
