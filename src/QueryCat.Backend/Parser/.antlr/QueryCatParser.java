// Generated from /mnt/data/work/querycat/src/QueryCat.Backend/Ast/Parser/QueryCatParser.g4 by ANTLR 4.9.2
 #pragma warning disable 3021 
import org.antlr.v4.runtime.atn.*;
import org.antlr.v4.runtime.dfa.DFA;
import org.antlr.v4.runtime.*;
import org.antlr.v4.runtime.misc.*;
import org.antlr.v4.runtime.tree.*;
import java.util.List;
import java.util.Iterator;
import java.util.ArrayList;

@SuppressWarnings({"all", "warnings", "unchecked", "unused", "cast"})
public class QueryCatParser extends Parser {
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
	public static final int
		RULE_program = 0, RULE_statement = 1, RULE_expression = 2, RULE_literal = 3;
	private static String[] makeRuleNames() {
		return new String[] {
			"program", "statement", "expression", "literal"
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

	@Override
	public String getGrammarFileName() { return "QueryCatParser.g4"; }

	@Override
	public String[] getRuleNames() { return ruleNames; }

	@Override
	public String getSerializedATN() { return _serializedATN; }

	@Override
	public ATN getATN() { return _ATN; }

	public QueryCatParser(TokenStream input) {
		super(input);
		_interp = new ParserATNSimulator(this,_ATN,_decisionToDFA,_sharedContextCache);
	}

	public static class ProgramContext extends ParserRuleContext {
		public List<StatementContext> statement() {
			return getRuleContexts(StatementContext.class);
		}
		public StatementContext statement(int i) {
			return getRuleContext(StatementContext.class,i);
		}
		public TerminalNode EOF() { return getToken(QueryCatParser.EOF, 0); }
		public List<TerminalNode> SEMICOLON() { return getTokens(QueryCatParser.SEMICOLON); }
		public TerminalNode SEMICOLON(int i) {
			return getToken(QueryCatParser.SEMICOLON, i);
		}
		public ProgramContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_program; }
	}

	public final ProgramContext program() throws RecognitionException {
		ProgramContext _localctx = new ProgramContext(_ctx, getState());
		enterRule(_localctx, 0, RULE_program);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(8);
			statement();
			setState(13);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==SEMICOLON) {
				{
				{
				setState(9);
				match(SEMICOLON);
				setState(10);
				statement();
				}
				}
				setState(15);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(16);
			match(EOF);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class StatementContext extends ParserRuleContext {
		public ExpressionContext expression() {
			return getRuleContext(ExpressionContext.class,0);
		}
		public TerminalNode ECHO() { return getToken(QueryCatParser.ECHO, 0); }
		public StatementContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_statement; }
	}

	public final StatementContext statement() throws RecognitionException {
		StatementContext _localctx = new StatementContext(_ctx, getState());
		enterRule(_localctx, 2, RULE_statement);
		try {
			setState(21);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case INTEGER_LITERAL:
			case FLOAT_LITERAL:
			case NUMERIC_LITERAL:
			case STRING_LITERAL:
			case BOOLEAN_LITERAL:
				enterOuterAlt(_localctx, 1);
				{
				setState(18);
				expression(0);
				}
				break;
			case ECHO:
				enterOuterAlt(_localctx, 2);
				{
				setState(19);
				match(ECHO);
				setState(20);
				expression(0);
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class ExpressionContext extends ParserRuleContext {
		public ExpressionContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_expression; }
	 
		public ExpressionContext() { }
		public void copyFrom(ExpressionContext ctx) {
			super.copyFrom(ctx);
		}
	}
	public static class ExpressionPlusMinusContext extends ExpressionContext {
		public List<ExpressionContext> expression() {
			return getRuleContexts(ExpressionContext.class);
		}
		public ExpressionContext expression(int i) {
			return getRuleContext(ExpressionContext.class,i);
		}
		public TerminalNode PLUS() { return getToken(QueryCatParser.PLUS, 0); }
		public TerminalNode MINUS() { return getToken(QueryCatParser.MINUS, 0); }
		public ExpressionPlusMinusContext(ExpressionContext ctx) { copyFrom(ctx); }
	}
	public static class ExpressionMulDivModContext extends ExpressionContext {
		public List<ExpressionContext> expression() {
			return getRuleContexts(ExpressionContext.class);
		}
		public ExpressionContext expression(int i) {
			return getRuleContext(ExpressionContext.class,i);
		}
		public TerminalNode STAR() { return getToken(QueryCatParser.STAR, 0); }
		public TerminalNode DIV() { return getToken(QueryCatParser.DIV, 0); }
		public TerminalNode MOD() { return getToken(QueryCatParser.MOD, 0); }
		public ExpressionMulDivModContext(ExpressionContext ctx) { copyFrom(ctx); }
	}
	public static class ExpressionLiteralContext extends ExpressionContext {
		public LiteralContext literal() {
			return getRuleContext(LiteralContext.class,0);
		}
		public ExpressionLiteralContext(ExpressionContext ctx) { copyFrom(ctx); }
	}

	public final ExpressionContext expression() throws RecognitionException {
		return expression(0);
	}

	private ExpressionContext expression(int _p) throws RecognitionException {
		ParserRuleContext _parentctx = _ctx;
		int _parentState = getState();
		ExpressionContext _localctx = new ExpressionContext(_ctx, _parentState);
		ExpressionContext _prevctx = _localctx;
		int _startState = 4;
		enterRecursionRule(_localctx, 4, RULE_expression, _p);
		int _la;
		try {
			int _alt;
			enterOuterAlt(_localctx, 1);
			{
			{
			_localctx = new ExpressionLiteralContext(_localctx);
			_ctx = _localctx;
			_prevctx = _localctx;

			setState(24);
			literal();
			}
			_ctx.stop = _input.LT(-1);
			setState(34);
			_errHandler.sync(this);
			_alt = getInterpreter().adaptivePredict(_input,3,_ctx);
			while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					if ( _parseListeners!=null ) triggerExitRuleEvent();
					_prevctx = _localctx;
					{
					setState(32);
					_errHandler.sync(this);
					switch ( getInterpreter().adaptivePredict(_input,2,_ctx) ) {
					case 1:
						{
						_localctx = new ExpressionMulDivModContext(new ExpressionContext(_parentctx, _parentState));
						pushNewRecursionContext(_localctx, _startState, RULE_expression);
						setState(26);
						if (!(precpred(_ctx, 2))) throw new FailedPredicateException(this, "precpred(_ctx, 2)");
						setState(27);
						_la = _input.LA(1);
						if ( !((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << STAR) | (1L << DIV) | (1L << MOD))) != 0)) ) {
						_errHandler.recoverInline(this);
						}
						else {
							if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
							_errHandler.reportMatch(this);
							consume();
						}
						setState(28);
						expression(3);
						}
						break;
					case 2:
						{
						_localctx = new ExpressionPlusMinusContext(new ExpressionContext(_parentctx, _parentState));
						pushNewRecursionContext(_localctx, _startState, RULE_expression);
						setState(29);
						if (!(precpred(_ctx, 1))) throw new FailedPredicateException(this, "precpred(_ctx, 1)");
						setState(30);
						_la = _input.LA(1);
						if ( !(_la==PLUS || _la==MINUS) ) {
						_errHandler.recoverInline(this);
						}
						else {
							if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
							_errHandler.reportMatch(this);
							consume();
						}
						setState(31);
						expression(2);
						}
						break;
					}
					} 
				}
				setState(36);
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,3,_ctx);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			unrollRecursionContexts(_parentctx);
		}
		return _localctx;
	}

	public static class LiteralContext extends ParserRuleContext {
		public TerminalNode INTEGER_LITERAL() { return getToken(QueryCatParser.INTEGER_LITERAL, 0); }
		public TerminalNode FLOAT_LITERAL() { return getToken(QueryCatParser.FLOAT_LITERAL, 0); }
		public TerminalNode NUMERIC_LITERAL() { return getToken(QueryCatParser.NUMERIC_LITERAL, 0); }
		public TerminalNode STRING_LITERAL() { return getToken(QueryCatParser.STRING_LITERAL, 0); }
		public TerminalNode BOOLEAN_LITERAL() { return getToken(QueryCatParser.BOOLEAN_LITERAL, 0); }
		public LiteralContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_literal; }
	}

	public final LiteralContext literal() throws RecognitionException {
		LiteralContext _localctx = new LiteralContext(_ctx, getState());
		enterRule(_localctx, 6, RULE_literal);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(37);
			_la = _input.LA(1);
			if ( !(((((_la - 72)) & ~0x3f) == 0 && ((1L << (_la - 72)) & ((1L << (INTEGER_LITERAL - 72)) | (1L << (FLOAT_LITERAL - 72)) | (1L << (NUMERIC_LITERAL - 72)) | (1L << (STRING_LITERAL - 72)) | (1L << (BOOLEAN_LITERAL - 72)))) != 0)) ) {
			_errHandler.recoverInline(this);
			}
			else {
				if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
				_errHandler.reportMatch(this);
				consume();
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public boolean sempred(RuleContext _localctx, int ruleIndex, int predIndex) {
		switch (ruleIndex) {
		case 2:
			return expression_sempred((ExpressionContext)_localctx, predIndex);
		}
		return true;
	}
	private boolean expression_sempred(ExpressionContext _localctx, int predIndex) {
		switch (predIndex) {
		case 0:
			return precpred(_ctx, 2);
		case 1:
			return precpred(_ctx, 1);
		}
		return true;
	}

	public static final String _serializedATN =
		"\3\u608b\ua72a\u8133\ub9ed\u417c\u3be7\u7786\u5964\3Q*\4\2\t\2\4\3\t\3"+
		"\4\4\t\4\4\5\t\5\3\2\3\2\3\2\7\2\16\n\2\f\2\16\2\21\13\2\3\2\3\2\3\3\3"+
		"\3\3\3\5\3\30\n\3\3\4\3\4\3\4\3\4\3\4\3\4\3\4\3\4\3\4\7\4#\n\4\f\4\16"+
		"\4&\13\4\3\5\3\5\3\5\2\3\6\6\2\4\6\b\2\5\3\2\21\23\3\2\17\20\3\2JN\2)"+
		"\2\n\3\2\2\2\4\27\3\2\2\2\6\31\3\2\2\2\b\'\3\2\2\2\n\17\5\4\3\2\13\f\7"+
		"\13\2\2\f\16\5\4\3\2\r\13\3\2\2\2\16\21\3\2\2\2\17\r\3\2\2\2\17\20\3\2"+
		"\2\2\20\22\3\2\2\2\21\17\3\2\2\2\22\23\7\2\2\3\23\3\3\2\2\2\24\30\5\6"+
		"\4\2\25\26\7\63\2\2\26\30\5\6\4\2\27\24\3\2\2\2\27\25\3\2\2\2\30\5\3\2"+
		"\2\2\31\32\b\4\1\2\32\33\5\b\5\2\33$\3\2\2\2\34\35\f\4\2\2\35\36\t\2\2"+
		"\2\36#\5\6\4\5\37 \f\3\2\2 !\t\3\2\2!#\5\6\4\4\"\34\3\2\2\2\"\37\3\2\2"+
		"\2#&\3\2\2\2$\"\3\2\2\2$%\3\2\2\2%\7\3\2\2\2&$\3\2\2\2\'(\t\4\2\2(\t\3"+
		"\2\2\2\6\17\27\"$";
	public static final ATN _ATN =
		new ATNDeserializer().deserialize(_serializedATN.toCharArray());
	static {
		_decisionToDFA = new DFA[_ATN.getNumberOfDecisions()];
		for (int i = 0; i < _ATN.getNumberOfDecisions(); i++) {
			_decisionToDFA[i] = new DFA(_ATN.getDecisionState(i), i);
		}
	}
}