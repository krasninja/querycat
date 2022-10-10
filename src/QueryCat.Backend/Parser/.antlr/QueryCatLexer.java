// Generated from /mnt/data/work/querycat/src/QueryCat.Backend/Ast/Parser/QueryCatLexer.g4 by ANTLR 4.9.2
 #pragma warning disable 3021 
import org.antlr.v4.runtime.Lexer;
import org.antlr.v4.runtime.CharStream;
import org.antlr.v4.runtime.Token;
import org.antlr.v4.runtime.TokenStream;
import org.antlr.v4.runtime.*;
import org.antlr.v4.runtime.atn.*;
import org.antlr.v4.runtime.dfa.DFA;
import org.antlr.v4.runtime.misc.*;

@SuppressWarnings({"all", "warnings", "unchecked", "unused", "cast"})
public class QueryCatLexer extends Lexer {
	static { RuntimeMetaData.checkVersion("4.9.2", RuntimeMetaData.VERSION); }

	protected static final DFA[] _decisionToDFA;
	protected static final PredictionContextCache _sharedContextCache =
		new PredictionContextCache();
	public static final int
		LEFT_PAREN=1, RIGHT_PAREN=2, ASSIGN=3, ASSOCIATION=4, COLON=5, COMMA=6, 
		PERIOD=7, ELLIPSIS=8, SEMICOLON=9, QUESTION=10, LEFT_BRACKET=11, RIGHT_BRACKET=12, 
		PLUS=13, MINUS=14, STAR=15, DIV=16, MOD=17, EQUALS=18, NOT_EQUALS=19, 
		GREATER=20, GREATER_OR_EQUALS=21, LESS=22, LESS_OR_EQUALS=23, CONCAT=24, 
		INTEGER=25, STRING=26, FLOAT=27, TIMESTAMP=28, BOOLEAN=29, NUMERIC=30, 
		OBJECT=31, ANY=32, TRUE=33, FALSE=34, NULL=35, AND=36, OR=37, IN=38, IS=39, 
		NOT=40, LIKE=41, VOID=42, AS=43, TO=44, BY=45, ONLY=46, DEFAULT=47, CAST=48, 
		ECHO=49, BETWEEN=50, OFFSET=51, ROW=52, ROWS=53, FETCH=54, FIRST=55, NEXT=56, 
		ORDER=57, ASC=58, DESC=59, HAVING=60, WHERE=61, UNION=62, GROUP=63, INTO=64, 
		SELECT=65, FROM=66, DISTINCT=67, ALL=68, FORMAT=69, TYPE=70, IDENTIFIER=71, 
		INTEGER_LITERAL=72, FLOAT_LITERAL=73, NUMERIC_LITERAL=74, STRING_LITERAL=75, 
		BOOLEAN_LITERAL=76, SINGLE_LINE_COMMENT=77, MULTILINE_COMMENT=78, SPACES=79;
	public static String[] channelNames = {
		"DEFAULT_TOKEN_CHANNEL", "HIDDEN"
	};

	public static String[] modeNames = {
		"DEFAULT_MODE"
	};

	private static String[] makeRuleNames() {
		return new String[] {
			"LEFT_PAREN", "RIGHT_PAREN", "ASSIGN", "ASSOCIATION", "COLON", "COMMA", 
			"PERIOD", "ELLIPSIS", "SEMICOLON", "QUESTION", "LEFT_BRACKET", "RIGHT_BRACKET", 
			"PLUS", "MINUS", "STAR", "DIV", "MOD", "EQUALS", "NOT_EQUALS", "GREATER", 
			"GREATER_OR_EQUALS", "LESS", "LESS_OR_EQUALS", "CONCAT", "INTEGER", "STRING", 
			"FLOAT", "TIMESTAMP", "BOOLEAN", "NUMERIC", "OBJECT", "ANY", "TRUE", 
			"FALSE", "NULL", "AND", "OR", "IN", "IS", "NOT", "LIKE", "VOID", "AS", 
			"TO", "BY", "ONLY", "DEFAULT", "CAST", "ECHO", "BETWEEN", "OFFSET", "ROW", 
			"ROWS", "FETCH", "FIRST", "NEXT", "ORDER", "ASC", "DESC", "HAVING", "WHERE", 
			"UNION", "GROUP", "INTO", "SELECT", "FROM", "DISTINCT", "ALL", "FORMAT", 
			"TYPE", "NameChar", "NameStartChar", "IDENTIFIER", "HEX_DIGIT", "DIGIT", 
			"INTEGER_LITERAL", "FLOAT_LITERAL", "NUMERIC_LITERAL", "STRING_LITERAL", 
			"BOOLEAN_LITERAL", "SINGLE_LINE_COMMENT", "MULTILINE_COMMENT", "SPACES"
		};
	}
	public static final String[] ruleNames = makeRuleNames();

	private static String[] makeLiteralNames() {
		return new String[] {
			null, "'('", "')'", "':='", "'=>'", "':'", "','", "'.'", "'...'", "';'", 
			"'?'", "'['", "']'", "'+'", "'-'", "'*'", "'/'", "'%'", "'='", "'<>'", 
			"'>'", "'>='", "'<'", "'<='", "'||'", "'INTEGER'", "'STRING'", "'FLOAT'", 
			"'TIMESTAMP'", "'BOOLEAN'", "'NUMERIC'", "'OBJECT'", "'ANY'", "'TRUE'", 
			"'FALSE'", "'NULL'", "'AND'", "'OR'", "'IN'", "'IS'", "'NOT'", "'LIKE'", 
			"'VOID'", "'AS'", "'TO'", "'BY'", "'ONLY'", "'DEFAULT'", "'CAST'", "'ECHO'", 
			"'BETWEEN'", "'OFFSET'", "'ROW'", "'ROWS'", "'FETCH'", "'FIRST'", "'NEXT'", 
			"'ORDER'", "'ASC'", "'DESC'", "'HAVING'", "'WHERE'", "'UNION'", "'GROUP'", 
			"'INTO'", "'SELECT'", "'FROM'", "'DISTINCT'", "'ALL'", "'FORMAT'"
		};
	}
	private static final String[] _LITERAL_NAMES = makeLiteralNames();
	private static String[] makeSymbolicNames() {
		return new String[] {
			null, "LEFT_PAREN", "RIGHT_PAREN", "ASSIGN", "ASSOCIATION", "COLON", 
			"COMMA", "PERIOD", "ELLIPSIS", "SEMICOLON", "QUESTION", "LEFT_BRACKET", 
			"RIGHT_BRACKET", "PLUS", "MINUS", "STAR", "DIV", "MOD", "EQUALS", "NOT_EQUALS", 
			"GREATER", "GREATER_OR_EQUALS", "LESS", "LESS_OR_EQUALS", "CONCAT", "INTEGER", 
			"STRING", "FLOAT", "TIMESTAMP", "BOOLEAN", "NUMERIC", "OBJECT", "ANY", 
			"TRUE", "FALSE", "NULL", "AND", "OR", "IN", "IS", "NOT", "LIKE", "VOID", 
			"AS", "TO", "BY", "ONLY", "DEFAULT", "CAST", "ECHO", "BETWEEN", "OFFSET", 
			"ROW", "ROWS", "FETCH", "FIRST", "NEXT", "ORDER", "ASC", "DESC", "HAVING", 
			"WHERE", "UNION", "GROUP", "INTO", "SELECT", "FROM", "DISTINCT", "ALL", 
			"FORMAT", "TYPE", "IDENTIFIER", "INTEGER_LITERAL", "FLOAT_LITERAL", "NUMERIC_LITERAL", 
			"STRING_LITERAL", "BOOLEAN_LITERAL", "SINGLE_LINE_COMMENT", "MULTILINE_COMMENT", 
			"SPACES"
		};
	}
	private static final String[] _SYMBOLIC_NAMES = makeSymbolicNames();
	public static final Vocabulary VOCABULARY = new VocabularyImpl(_LITERAL_NAMES, _SYMBOLIC_NAMES);

	/**
	 * @deprecated Use {@link #VOCABULARY} instead.
	 */
	@Deprecated
	public static final String[] tokenNames;
	static {
		tokenNames = new String[_SYMBOLIC_NAMES.length];
		for (int i = 0; i < tokenNames.length; i++) {
			tokenNames[i] = VOCABULARY.getLiteralName(i);
			if (tokenNames[i] == null) {
				tokenNames[i] = VOCABULARY.getSymbolicName(i);
			}

			if (tokenNames[i] == null) {
				tokenNames[i] = "<INVALID>";
			}
		}
	}

	@Override
	@Deprecated
	public String[] getTokenNames() {
		return tokenNames;
	}

	@Override

	public Vocabulary getVocabulary() {
		return VOCABULARY;
	}


	public QueryCatLexer(CharStream input) {
		super(input);
		_interp = new LexerATNSimulator(this,_ATN,_decisionToDFA,_sharedContextCache);
	}

	@Override
	public String getGrammarFileName() { return "QueryCatLexer.g4"; }

	@Override
	public String[] getRuleNames() { return ruleNames; }

	@Override
	public String getSerializedATN() { return _serializedATN; }

	@Override
	public String[] getChannelNames() { return channelNames; }

	@Override
	public String[] getModeNames() { return modeNames; }

	@Override
	public ATN getATN() { return _ATN; }

	public static final String _serializedATN =
		"\3\u608b\ua72a\u8133\ub9ed\u417c\u3be7\u7786\u5964\2Q\u025b\b\1\4\2\t"+
		"\2\4\3\t\3\4\4\t\4\4\5\t\5\4\6\t\6\4\7\t\7\4\b\t\b\4\t\t\t\4\n\t\n\4\13"+
		"\t\13\4\f\t\f\4\r\t\r\4\16\t\16\4\17\t\17\4\20\t\20\4\21\t\21\4\22\t\22"+
		"\4\23\t\23\4\24\t\24\4\25\t\25\4\26\t\26\4\27\t\27\4\30\t\30\4\31\t\31"+
		"\4\32\t\32\4\33\t\33\4\34\t\34\4\35\t\35\4\36\t\36\4\37\t\37\4 \t \4!"+
		"\t!\4\"\t\"\4#\t#\4$\t$\4%\t%\4&\t&\4\'\t\'\4(\t(\4)\t)\4*\t*\4+\t+\4"+
		",\t,\4-\t-\4.\t.\4/\t/\4\60\t\60\4\61\t\61\4\62\t\62\4\63\t\63\4\64\t"+
		"\64\4\65\t\65\4\66\t\66\4\67\t\67\48\t8\49\t9\4:\t:\4;\t;\4<\t<\4=\t="+
		"\4>\t>\4?\t?\4@\t@\4A\tA\4B\tB\4C\tC\4D\tD\4E\tE\4F\tF\4G\tG\4H\tH\4I"+
		"\tI\4J\tJ\4K\tK\4L\tL\4M\tM\4N\tN\4O\tO\4P\tP\4Q\tQ\4R\tR\4S\tS\4T\tT"+
		"\3\2\3\2\3\3\3\3\3\4\3\4\3\4\3\5\3\5\3\5\3\6\3\6\3\7\3\7\3\b\3\b\3\t\3"+
		"\t\3\t\3\t\3\n\3\n\3\13\3\13\3\f\3\f\3\r\3\r\3\16\3\16\3\17\3\17\3\20"+
		"\3\20\3\21\3\21\3\22\3\22\3\23\3\23\3\24\3\24\3\24\3\25\3\25\3\26\3\26"+
		"\3\26\3\27\3\27\3\30\3\30\3\30\3\31\3\31\3\31\3\32\3\32\3\32\3\32\3\32"+
		"\3\32\3\32\3\32\3\33\3\33\3\33\3\33\3\33\3\33\3\33\3\34\3\34\3\34\3\34"+
		"\3\34\3\34\3\35\3\35\3\35\3\35\3\35\3\35\3\35\3\35\3\35\3\35\3\36\3\36"+
		"\3\36\3\36\3\36\3\36\3\36\3\36\3\37\3\37\3\37\3\37\3\37\3\37\3\37\3\37"+
		"\3 \3 \3 \3 \3 \3 \3 \3!\3!\3!\3!\3\"\3\"\3\"\3\"\3\"\3#\3#\3#\3#\3#\3"+
		"#\3$\3$\3$\3$\3$\3%\3%\3%\3%\3&\3&\3&\3\'\3\'\3\'\3(\3(\3(\3)\3)\3)\3"+
		")\3*\3*\3*\3*\3*\3+\3+\3+\3+\3+\3,\3,\3,\3-\3-\3-\3.\3.\3.\3/\3/\3/\3"+
		"/\3/\3\60\3\60\3\60\3\60\3\60\3\60\3\60\3\60\3\61\3\61\3\61\3\61\3\61"+
		"\3\62\3\62\3\62\3\62\3\62\3\63\3\63\3\63\3\63\3\63\3\63\3\63\3\63\3\64"+
		"\3\64\3\64\3\64\3\64\3\64\3\64\3\65\3\65\3\65\3\65\3\66\3\66\3\66\3\66"+
		"\3\66\3\67\3\67\3\67\3\67\3\67\3\67\38\38\38\38\38\38\39\39\39\39\39\3"+
		":\3:\3:\3:\3:\3:\3;\3;\3;\3;\3<\3<\3<\3<\3<\3=\3=\3=\3=\3=\3=\3=\3>\3"+
		">\3>\3>\3>\3>\3?\3?\3?\3?\3?\3?\3@\3@\3@\3@\3@\3@\3A\3A\3A\3A\3A\3B\3"+
		"B\3B\3B\3B\3B\3B\3C\3C\3C\3C\3C\3D\3D\3D\3D\3D\3D\3D\3D\3D\3E\3E\3E\3"+
		"E\3F\3F\3F\3F\3F\3F\3F\3G\3G\3G\3G\3G\3G\3G\3G\5G\u01e5\nG\3H\3H\5H\u01e9"+
		"\nH\3I\3I\3J\3J\7J\u01ef\nJ\fJ\16J\u01f2\13J\3K\3K\3L\3L\3M\6M\u01f9\n"+
		"M\rM\16M\u01fa\3N\6N\u01fe\nN\rN\16N\u01ff\3N\3N\7N\u0204\nN\fN\16N\u0207"+
		"\13N\5N\u0209\nN\3N\3N\6N\u020d\nN\rN\16N\u020e\5N\u0211\nN\3N\3N\5N\u0215"+
		"\nN\3N\6N\u0218\nN\rN\16N\u0219\5N\u021c\nN\3N\3N\3N\3N\6N\u0222\nN\r"+
		"N\16N\u0223\5N\u0226\nN\3O\3O\3O\3P\3P\3P\3P\7P\u022f\nP\fP\16P\u0232"+
		"\13P\3P\3P\3Q\3Q\5Q\u0238\nQ\3R\3R\7R\u023c\nR\fR\16R\u023f\13R\3R\5R"+
		"\u0242\nR\3R\3R\5R\u0246\nR\3R\3R\3S\3S\3S\3S\7S\u024e\nS\fS\16S\u0251"+
		"\13S\3S\3S\3S\3S\3S\3T\3T\3T\3T\3\u024f\2U\3\3\5\4\7\5\t\6\13\7\r\b\17"+
		"\t\21\n\23\13\25\f\27\r\31\16\33\17\35\20\37\21!\22#\23%\24\'\25)\26+"+
		"\27-\30/\31\61\32\63\33\65\34\67\359\36;\37= ?!A\"C#E$G%I&K\'M(O)Q*S+"+
		"U,W-Y.[/]\60_\61a\62c\63e\64g\65i\66k\67m8o9q:s;u<w=y>{?}@\177A\u0081"+
		"B\u0083C\u0085D\u0087E\u0089F\u008bG\u008dH\u008f\2\u0091\2\u0093I\u0095"+
		"\2\u0097\2\u0099J\u009bK\u009dL\u009fM\u00a1N\u00a3O\u00a5P\u00a7Q\3\2"+
		"\n\7\2\62;aa\u00b9\u00b9\u0302\u0371\u2041\u2042\17\2C\\c|\u00c2\u00d8"+
		"\u00da\u00f8\u00fa\u0301\u0372\u037f\u0381\u2001\u200e\u200f\u2072\u2191"+
		"\u2c02\u2ff1\u3003\ud801\uf902\ufdd1\ufdf2\uffff\4\2\62;CH\3\2\62;\4\2"+
		"--//\3\2))\4\2\f\f\17\17\5\2\13\r\17\17\"\"\2\u0271\2\3\3\2\2\2\2\5\3"+
		"\2\2\2\2\7\3\2\2\2\2\t\3\2\2\2\2\13\3\2\2\2\2\r\3\2\2\2\2\17\3\2\2\2\2"+
		"\21\3\2\2\2\2\23\3\2\2\2\2\25\3\2\2\2\2\27\3\2\2\2\2\31\3\2\2\2\2\33\3"+
		"\2\2\2\2\35\3\2\2\2\2\37\3\2\2\2\2!\3\2\2\2\2#\3\2\2\2\2%\3\2\2\2\2\'"+
		"\3\2\2\2\2)\3\2\2\2\2+\3\2\2\2\2-\3\2\2\2\2/\3\2\2\2\2\61\3\2\2\2\2\63"+
		"\3\2\2\2\2\65\3\2\2\2\2\67\3\2\2\2\29\3\2\2\2\2;\3\2\2\2\2=\3\2\2\2\2"+
		"?\3\2\2\2\2A\3\2\2\2\2C\3\2\2\2\2E\3\2\2\2\2G\3\2\2\2\2I\3\2\2\2\2K\3"+
		"\2\2\2\2M\3\2\2\2\2O\3\2\2\2\2Q\3\2\2\2\2S\3\2\2\2\2U\3\2\2\2\2W\3\2\2"+
		"\2\2Y\3\2\2\2\2[\3\2\2\2\2]\3\2\2\2\2_\3\2\2\2\2a\3\2\2\2\2c\3\2\2\2\2"+
		"e\3\2\2\2\2g\3\2\2\2\2i\3\2\2\2\2k\3\2\2\2\2m\3\2\2\2\2o\3\2\2\2\2q\3"+
		"\2\2\2\2s\3\2\2\2\2u\3\2\2\2\2w\3\2\2\2\2y\3\2\2\2\2{\3\2\2\2\2}\3\2\2"+
		"\2\2\177\3\2\2\2\2\u0081\3\2\2\2\2\u0083\3\2\2\2\2\u0085\3\2\2\2\2\u0087"+
		"\3\2\2\2\2\u0089\3\2\2\2\2\u008b\3\2\2\2\2\u008d\3\2\2\2\2\u0093\3\2\2"+
		"\2\2\u0099\3\2\2\2\2\u009b\3\2\2\2\2\u009d\3\2\2\2\2\u009f\3\2\2\2\2\u00a1"+
		"\3\2\2\2\2\u00a3\3\2\2\2\2\u00a5\3\2\2\2\2\u00a7\3\2\2\2\3\u00a9\3\2\2"+
		"\2\5\u00ab\3\2\2\2\7\u00ad\3\2\2\2\t\u00b0\3\2\2\2\13\u00b3\3\2\2\2\r"+
		"\u00b5\3\2\2\2\17\u00b7\3\2\2\2\21\u00b9\3\2\2\2\23\u00bd\3\2\2\2\25\u00bf"+
		"\3\2\2\2\27\u00c1\3\2\2\2\31\u00c3\3\2\2\2\33\u00c5\3\2\2\2\35\u00c7\3"+
		"\2\2\2\37\u00c9\3\2\2\2!\u00cb\3\2\2\2#\u00cd\3\2\2\2%\u00cf\3\2\2\2\'"+
		"\u00d1\3\2\2\2)\u00d4\3\2\2\2+\u00d6\3\2\2\2-\u00d9\3\2\2\2/\u00db\3\2"+
		"\2\2\61\u00de\3\2\2\2\63\u00e1\3\2\2\2\65\u00e9\3\2\2\2\67\u00f0\3\2\2"+
		"\29\u00f6\3\2\2\2;\u0100\3\2\2\2=\u0108\3\2\2\2?\u0110\3\2\2\2A\u0117"+
		"\3\2\2\2C\u011b\3\2\2\2E\u0120\3\2\2\2G\u0126\3\2\2\2I\u012b\3\2\2\2K"+
		"\u012f\3\2\2\2M\u0132\3\2\2\2O\u0135\3\2\2\2Q\u0138\3\2\2\2S\u013c\3\2"+
		"\2\2U\u0141\3\2\2\2W\u0146\3\2\2\2Y\u0149\3\2\2\2[\u014c\3\2\2\2]\u014f"+
		"\3\2\2\2_\u0154\3\2\2\2a\u015c\3\2\2\2c\u0161\3\2\2\2e\u0166\3\2\2\2g"+
		"\u016e\3\2\2\2i\u0175\3\2\2\2k\u0179\3\2\2\2m\u017e\3\2\2\2o\u0184\3\2"+
		"\2\2q\u018a\3\2\2\2s\u018f\3\2\2\2u\u0195\3\2\2\2w\u0199\3\2\2\2y\u019e"+
		"\3\2\2\2{\u01a5\3\2\2\2}\u01ab\3\2\2\2\177\u01b1\3\2\2\2\u0081\u01b7\3"+
		"\2\2\2\u0083\u01bc\3\2\2\2\u0085\u01c3\3\2\2\2\u0087\u01c8\3\2\2\2\u0089"+
		"\u01d1\3\2\2\2\u008b\u01d5\3\2\2\2\u008d\u01e4\3\2\2\2\u008f\u01e8\3\2"+
		"\2\2\u0091\u01ea\3\2\2\2\u0093\u01ec\3\2\2\2\u0095\u01f3\3\2\2\2\u0097"+
		"\u01f5\3\2\2\2\u0099\u01f8\3\2\2\2\u009b\u0225\3\2\2\2\u009d\u0227\3\2"+
		"\2\2\u009f\u022a\3\2\2\2\u00a1\u0237\3\2\2\2\u00a3\u0239\3\2\2\2\u00a5"+
		"\u0249\3\2\2\2\u00a7\u0257\3\2\2\2\u00a9\u00aa\7*\2\2\u00aa\4\3\2\2\2"+
		"\u00ab\u00ac\7+\2\2\u00ac\6\3\2\2\2\u00ad\u00ae\7<\2\2\u00ae\u00af\7?"+
		"\2\2\u00af\b\3\2\2\2\u00b0\u00b1\7?\2\2\u00b1\u00b2\7@\2\2\u00b2\n\3\2"+
		"\2\2\u00b3\u00b4\7<\2\2\u00b4\f\3\2\2\2\u00b5\u00b6\7.\2\2\u00b6\16\3"+
		"\2\2\2\u00b7\u00b8\7\60\2\2\u00b8\20\3\2\2\2\u00b9\u00ba\7\60\2\2\u00ba"+
		"\u00bb\7\60\2\2\u00bb\u00bc\7\60\2\2\u00bc\22\3\2\2\2\u00bd\u00be\7=\2"+
		"\2\u00be\24\3\2\2\2\u00bf\u00c0\7A\2\2\u00c0\26\3\2\2\2\u00c1\u00c2\7"+
		"]\2\2\u00c2\30\3\2\2\2\u00c3\u00c4\7_\2\2\u00c4\32\3\2\2\2\u00c5\u00c6"+
		"\7-\2\2\u00c6\34\3\2\2\2\u00c7\u00c8\7/\2\2\u00c8\36\3\2\2\2\u00c9\u00ca"+
		"\7,\2\2\u00ca \3\2\2\2\u00cb\u00cc\7\61\2\2\u00cc\"\3\2\2\2\u00cd\u00ce"+
		"\7\'\2\2\u00ce$\3\2\2\2\u00cf\u00d0\7?\2\2\u00d0&\3\2\2\2\u00d1\u00d2"+
		"\7>\2\2\u00d2\u00d3\7@\2\2\u00d3(\3\2\2\2\u00d4\u00d5\7@\2\2\u00d5*\3"+
		"\2\2\2\u00d6\u00d7\7@\2\2\u00d7\u00d8\7?\2\2\u00d8,\3\2\2\2\u00d9\u00da"+
		"\7>\2\2\u00da.\3\2\2\2\u00db\u00dc\7>\2\2\u00dc\u00dd\7?\2\2\u00dd\60"+
		"\3\2\2\2\u00de\u00df\7~\2\2\u00df\u00e0\7~\2\2\u00e0\62\3\2\2\2\u00e1"+
		"\u00e2\7K\2\2\u00e2\u00e3\7P\2\2\u00e3\u00e4\7V\2\2\u00e4\u00e5\7G\2\2"+
		"\u00e5\u00e6\7I\2\2\u00e6\u00e7\7G\2\2\u00e7\u00e8\7T\2\2\u00e8\64\3\2"+
		"\2\2\u00e9\u00ea\7U\2\2\u00ea\u00eb\7V\2\2\u00eb\u00ec\7T\2\2\u00ec\u00ed"+
		"\7K\2\2\u00ed\u00ee\7P\2\2\u00ee\u00ef\7I\2\2\u00ef\66\3\2\2\2\u00f0\u00f1"+
		"\7H\2\2\u00f1\u00f2\7N\2\2\u00f2\u00f3\7Q\2\2\u00f3\u00f4\7C\2\2\u00f4"+
		"\u00f5\7V\2\2\u00f58\3\2\2\2\u00f6\u00f7\7V\2\2\u00f7\u00f8\7K\2\2\u00f8"+
		"\u00f9\7O\2\2\u00f9\u00fa\7G\2\2\u00fa\u00fb\7U\2\2\u00fb\u00fc\7V\2\2"+
		"\u00fc\u00fd\7C\2\2\u00fd\u00fe\7O\2\2\u00fe\u00ff\7R\2\2\u00ff:\3\2\2"+
		"\2\u0100\u0101\7D\2\2\u0101\u0102\7Q\2\2\u0102\u0103\7Q\2\2\u0103\u0104"+
		"\7N\2\2\u0104\u0105\7G\2\2\u0105\u0106\7C\2\2\u0106\u0107\7P\2\2\u0107"+
		"<\3\2\2\2\u0108\u0109\7P\2\2\u0109\u010a\7W\2\2\u010a\u010b\7O\2\2\u010b"+
		"\u010c\7G\2\2\u010c\u010d\7T\2\2\u010d\u010e\7K\2\2\u010e\u010f\7E\2\2"+
		"\u010f>\3\2\2\2\u0110\u0111\7Q\2\2\u0111\u0112\7D\2\2\u0112\u0113\7L\2"+
		"\2\u0113\u0114\7G\2\2\u0114\u0115\7E\2\2\u0115\u0116\7V\2\2\u0116@\3\2"+
		"\2\2\u0117\u0118\7C\2\2\u0118\u0119\7P\2\2\u0119\u011a\7[\2\2\u011aB\3"+
		"\2\2\2\u011b\u011c\7V\2\2\u011c\u011d\7T\2\2\u011d\u011e\7W\2\2\u011e"+
		"\u011f\7G\2\2\u011fD\3\2\2\2\u0120\u0121\7H\2\2\u0121\u0122\7C\2\2\u0122"+
		"\u0123\7N\2\2\u0123\u0124\7U\2\2\u0124\u0125\7G\2\2\u0125F\3\2\2\2\u0126"+
		"\u0127\7P\2\2\u0127\u0128\7W\2\2\u0128\u0129\7N\2\2\u0129\u012a\7N\2\2"+
		"\u012aH\3\2\2\2\u012b\u012c\7C\2\2\u012c\u012d\7P\2\2\u012d\u012e\7F\2"+
		"\2\u012eJ\3\2\2\2\u012f\u0130\7Q\2\2\u0130\u0131\7T\2\2\u0131L\3\2\2\2"+
		"\u0132\u0133\7K\2\2\u0133\u0134\7P\2\2\u0134N\3\2\2\2\u0135\u0136\7K\2"+
		"\2\u0136\u0137\7U\2\2\u0137P\3\2\2\2\u0138\u0139\7P\2\2\u0139\u013a\7"+
		"Q\2\2\u013a\u013b\7V\2\2\u013bR\3\2\2\2\u013c\u013d\7N\2\2\u013d\u013e"+
		"\7K\2\2\u013e\u013f\7M\2\2\u013f\u0140\7G\2\2\u0140T\3\2\2\2\u0141\u0142"+
		"\7X\2\2\u0142\u0143\7Q\2\2\u0143\u0144\7K\2\2\u0144\u0145\7F\2\2\u0145"+
		"V\3\2\2\2\u0146\u0147\7C\2\2\u0147\u0148\7U\2\2\u0148X\3\2\2\2\u0149\u014a"+
		"\7V\2\2\u014a\u014b\7Q\2\2\u014bZ\3\2\2\2\u014c\u014d\7D\2\2\u014d\u014e"+
		"\7[\2\2\u014e\\\3\2\2\2\u014f\u0150\7Q\2\2\u0150\u0151\7P\2\2\u0151\u0152"+
		"\7N\2\2\u0152\u0153\7[\2\2\u0153^\3\2\2\2\u0154\u0155\7F\2\2\u0155\u0156"+
		"\7G\2\2\u0156\u0157\7H\2\2\u0157\u0158\7C\2\2\u0158\u0159\7W\2\2\u0159"+
		"\u015a\7N\2\2\u015a\u015b\7V\2\2\u015b`\3\2\2\2\u015c\u015d\7E\2\2\u015d"+
		"\u015e\7C\2\2\u015e\u015f\7U\2\2\u015f\u0160\7V\2\2\u0160b\3\2\2\2\u0161"+
		"\u0162\7G\2\2\u0162\u0163\7E\2\2\u0163\u0164\7J\2\2\u0164\u0165\7Q\2\2"+
		"\u0165d\3\2\2\2\u0166\u0167\7D\2\2\u0167\u0168\7G\2\2\u0168\u0169\7V\2"+
		"\2\u0169\u016a\7Y\2\2\u016a\u016b\7G\2\2\u016b\u016c\7G\2\2\u016c\u016d"+
		"\7P\2\2\u016df\3\2\2\2\u016e\u016f\7Q\2\2\u016f\u0170\7H\2\2\u0170\u0171"+
		"\7H\2\2\u0171\u0172\7U\2\2\u0172\u0173\7G\2\2\u0173\u0174\7V\2\2\u0174"+
		"h\3\2\2\2\u0175\u0176\7T\2\2\u0176\u0177\7Q\2\2\u0177\u0178\7Y\2\2\u0178"+
		"j\3\2\2\2\u0179\u017a\7T\2\2\u017a\u017b\7Q\2\2\u017b\u017c\7Y\2\2\u017c"+
		"\u017d\7U\2\2\u017dl\3\2\2\2\u017e\u017f\7H\2\2\u017f\u0180\7G\2\2\u0180"+
		"\u0181\7V\2\2\u0181\u0182\7E\2\2\u0182\u0183\7J\2\2\u0183n\3\2\2\2\u0184"+
		"\u0185\7H\2\2\u0185\u0186\7K\2\2\u0186\u0187\7T\2\2\u0187\u0188\7U\2\2"+
		"\u0188\u0189\7V\2\2\u0189p\3\2\2\2\u018a\u018b\7P\2\2\u018b\u018c\7G\2"+
		"\2\u018c\u018d\7Z\2\2\u018d\u018e\7V\2\2\u018er\3\2\2\2\u018f\u0190\7"+
		"Q\2\2\u0190\u0191\7T\2\2\u0191\u0192\7F\2\2\u0192\u0193\7G\2\2\u0193\u0194"+
		"\7T\2\2\u0194t\3\2\2\2\u0195\u0196\7C\2\2\u0196\u0197\7U\2\2\u0197\u0198"+
		"\7E\2\2\u0198v\3\2\2\2\u0199\u019a\7F\2\2\u019a\u019b\7G\2\2\u019b\u019c"+
		"\7U\2\2\u019c\u019d\7E\2\2\u019dx\3\2\2\2\u019e\u019f\7J\2\2\u019f\u01a0"+
		"\7C\2\2\u01a0\u01a1\7X\2\2\u01a1\u01a2\7K\2\2\u01a2\u01a3\7P\2\2\u01a3"+
		"\u01a4\7I\2\2\u01a4z\3\2\2\2\u01a5\u01a6\7Y\2\2\u01a6\u01a7\7J\2\2\u01a7"+
		"\u01a8\7G\2\2\u01a8\u01a9\7T\2\2\u01a9\u01aa\7G\2\2\u01aa|\3\2\2\2\u01ab"+
		"\u01ac\7W\2\2\u01ac\u01ad\7P\2\2\u01ad\u01ae\7K\2\2\u01ae\u01af\7Q\2\2"+
		"\u01af\u01b0\7P\2\2\u01b0~\3\2\2\2\u01b1\u01b2\7I\2\2\u01b2\u01b3\7T\2"+
		"\2\u01b3\u01b4\7Q\2\2\u01b4\u01b5\7W\2\2\u01b5\u01b6\7R\2\2\u01b6\u0080"+
		"\3\2\2\2\u01b7\u01b8\7K\2\2\u01b8\u01b9\7P\2\2\u01b9\u01ba\7V\2\2\u01ba"+
		"\u01bb\7Q\2\2\u01bb\u0082\3\2\2\2\u01bc\u01bd\7U\2\2\u01bd\u01be\7G\2"+
		"\2\u01be\u01bf\7N\2\2\u01bf\u01c0\7G\2\2\u01c0\u01c1\7E\2\2\u01c1\u01c2"+
		"\7V\2\2\u01c2\u0084\3\2\2\2\u01c3\u01c4\7H\2\2\u01c4\u01c5\7T\2\2\u01c5"+
		"\u01c6\7Q\2\2\u01c6\u01c7\7O\2\2\u01c7\u0086\3\2\2\2\u01c8\u01c9\7F\2"+
		"\2\u01c9\u01ca\7K\2\2\u01ca\u01cb\7U\2\2\u01cb\u01cc\7V\2\2\u01cc\u01cd"+
		"\7K\2\2\u01cd\u01ce\7P\2\2\u01ce\u01cf\7E\2\2\u01cf\u01d0\7V\2\2\u01d0"+
		"\u0088\3\2\2\2\u01d1\u01d2\7C\2\2\u01d2\u01d3\7N\2\2\u01d3\u01d4\7N\2"+
		"\2\u01d4\u008a\3\2\2\2\u01d5\u01d6\7H\2\2\u01d6\u01d7\7Q\2\2\u01d7\u01d8"+
		"\7T\2\2\u01d8\u01d9\7O\2\2\u01d9\u01da\7C\2\2\u01da\u01db\7V\2\2\u01db"+
		"\u008c\3\2\2\2\u01dc\u01e5\5\63\32\2\u01dd\u01e5\5\65\33\2\u01de\u01e5"+
		"\5\67\34\2\u01df\u01e5\59\35\2\u01e0\u01e5\5;\36\2\u01e1\u01e5\5=\37\2"+
		"\u01e2\u01e5\5? \2\u01e3\u01e5\5A!\2\u01e4\u01dc\3\2\2\2\u01e4\u01dd\3"+
		"\2\2\2\u01e4\u01de\3\2\2\2\u01e4\u01df\3\2\2\2\u01e4\u01e0\3\2\2\2\u01e4"+
		"\u01e1\3\2\2\2\u01e4\u01e2\3\2\2\2\u01e4\u01e3\3\2\2\2\u01e5\u008e\3\2"+
		"\2\2\u01e6\u01e9\5\u0091I\2\u01e7\u01e9\t\2\2\2\u01e8\u01e6\3\2\2\2\u01e8"+
		"\u01e7\3\2\2\2\u01e9\u0090\3\2\2\2\u01ea\u01eb\t\3\2\2\u01eb\u0092\3\2"+
		"\2\2\u01ec\u01f0\5\u0091I\2\u01ed\u01ef\5\u008fH\2\u01ee\u01ed\3\2\2\2"+
		"\u01ef\u01f2\3\2\2\2\u01f0\u01ee\3\2\2\2\u01f0\u01f1\3\2\2\2\u01f1\u0094"+
		"\3\2\2\2\u01f2\u01f0\3\2\2\2\u01f3\u01f4\t\4\2\2\u01f4\u0096\3\2\2\2\u01f5"+
		"\u01f6\t\5\2\2\u01f6\u0098\3\2\2\2\u01f7\u01f9\5\u0097L\2\u01f8\u01f7"+
		"\3\2\2\2\u01f9\u01fa\3\2\2\2\u01fa\u01f8\3\2\2\2\u01fa\u01fb\3\2\2\2\u01fb"+
		"\u009a\3\2\2\2\u01fc\u01fe\5\u0097L\2\u01fd\u01fc\3\2\2\2\u01fe\u01ff"+
		"\3\2\2\2\u01ff\u01fd\3\2\2\2\u01ff\u0200\3\2\2\2\u0200\u0208\3\2\2\2\u0201"+
		"\u0205\7\60\2\2\u0202\u0204\5\u0097L\2\u0203\u0202\3\2\2\2\u0204\u0207"+
		"\3\2\2\2\u0205\u0203\3\2\2\2\u0205\u0206\3\2\2\2\u0206\u0209\3\2\2\2\u0207"+
		"\u0205\3\2\2\2\u0208\u0201\3\2\2\2\u0208\u0209\3\2\2\2\u0209\u0211\3\2"+
		"\2\2\u020a\u020c\7\60\2\2\u020b\u020d\5\u0097L\2\u020c\u020b\3\2\2\2\u020d"+
		"\u020e\3\2\2\2\u020e\u020c\3\2\2\2\u020e\u020f\3\2\2\2\u020f\u0211\3\2"+
		"\2\2\u0210\u01fd\3\2\2\2\u0210\u020a\3\2\2\2\u0211\u021b\3\2\2\2\u0212"+
		"\u0214\7G\2\2\u0213\u0215\t\6\2\2\u0214\u0213\3\2\2\2\u0214\u0215\3\2"+
		"\2\2\u0215\u0217\3\2\2\2\u0216\u0218\5\u0097L\2\u0217\u0216\3\2\2\2\u0218"+
		"\u0219\3\2\2\2\u0219\u0217\3\2\2\2\u0219\u021a\3\2\2\2\u021a\u021c\3\2"+
		"\2\2\u021b\u0212\3\2\2\2\u021b\u021c\3\2\2\2\u021c\u0226\3\2\2\2\u021d"+
		"\u021e\7\62\2\2\u021e\u021f\7z\2\2\u021f\u0221\3\2\2\2\u0220\u0222\5\u0095"+
		"K\2\u0221\u0220\3\2\2\2\u0222\u0223\3\2\2\2\u0223\u0221\3\2\2\2\u0223"+
		"\u0224\3\2\2\2\u0224\u0226\3\2\2\2\u0225\u0210\3\2\2\2\u0225\u021d\3\2"+
		"\2\2\u0226\u009c\3\2\2\2\u0227\u0228\5\u009bN\2\u0228\u0229\7O\2\2\u0229"+
		"\u009e\3\2\2\2\u022a\u0230\7)\2\2\u022b\u022f\n\7\2\2\u022c\u022d\7)\2"+
		"\2\u022d\u022f\7)\2\2\u022e\u022b\3\2\2\2\u022e\u022c\3\2\2\2\u022f\u0232"+
		"\3\2\2\2\u0230\u022e\3\2\2\2\u0230\u0231\3\2\2\2\u0231\u0233\3\2\2\2\u0232"+
		"\u0230\3\2\2\2\u0233\u0234\7)\2\2\u0234\u00a0\3\2\2\2\u0235\u0238\5C\""+
		"\2\u0236\u0238\5E#\2\u0237\u0235\3\2\2\2\u0237\u0236\3\2\2\2\u0238\u00a2"+
		"\3\2\2\2\u0239\u023d\7%\2\2\u023a\u023c\n\b\2\2\u023b\u023a\3\2\2\2\u023c"+
		"\u023f\3\2\2\2\u023d\u023b\3\2\2\2\u023d\u023e\3\2\2\2\u023e\u0245\3\2"+
		"\2\2\u023f\u023d\3\2\2\2\u0240\u0242\7\17\2\2\u0241\u0240\3\2\2\2\u0241"+
		"\u0242\3\2\2\2\u0242\u0243\3\2\2\2\u0243\u0246\7\f\2\2\u0244\u0246\7\2"+
		"\2\3\u0245\u0241\3\2\2\2\u0245\u0244\3\2\2\2\u0246\u0247\3\2\2\2\u0247"+
		"\u0248\bR\2\2\u0248\u00a4\3\2\2\2\u0249\u024a\7\61\2\2\u024a\u024b\7,"+
		"\2\2\u024b\u024f\3\2\2\2\u024c\u024e\13\2\2\2\u024d\u024c\3\2\2\2\u024e"+
		"\u0251\3\2\2\2\u024f\u0250\3\2\2\2\u024f\u024d\3\2\2\2\u0250\u0252\3\2"+
		"\2\2\u0251\u024f\3\2\2\2\u0252\u0253\7,\2\2\u0253\u0254\7\61\2\2\u0254"+
		"\u0255\3\2\2\2\u0255\u0256\bS\2\2\u0256\u00a6\3\2\2\2\u0257\u0258\t\t"+
		"\2\2\u0258\u0259\3\2\2\2\u0259\u025a\bT\2\2\u025a\u00a8\3\2\2\2\30\2\u01e4"+
		"\u01e8\u01f0\u01fa\u01ff\u0205\u0208\u020e\u0210\u0214\u0219\u021b\u0223"+
		"\u0225\u022e\u0230\u0237\u023d\u0241\u0245\u024f\3\2\3\2";
	public static final ATN _ATN =
		new ATNDeserializer().deserialize(_serializedATN.toCharArray());
	static {
		_decisionToDFA = new DFA[_ATN.getNumberOfDecisions()];
		for (int i = 0; i < _ATN.getNumberOfDecisions(); i++) {
			_decisionToDFA[i] = new DFA(_ATN.getDecisionState(i), i);
		}
	}
}