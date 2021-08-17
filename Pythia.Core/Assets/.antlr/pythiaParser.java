// Generated from c:\Projects\Core20\Pythia\Pythia.Core\Assets\pythia.g4 by ANTLR 4.8
import org.antlr.v4.runtime.atn.*;
import org.antlr.v4.runtime.dfa.DFA;
import org.antlr.v4.runtime.*;
import org.antlr.v4.runtime.misc.*;
import org.antlr.v4.runtime.tree.*;
import java.util.List;
import java.util.Iterator;
import java.util.ArrayList;

@SuppressWarnings({"all", "warnings", "unchecked", "unused", "cast"})
public class pythiaParser extends Parser {
	static { RuntimeMetaData.checkVersion("4.8", RuntimeMetaData.VERSION); }

	protected static final DFA[] _decisionToDFA;
	protected static final PredictionContextCache _sharedContextCache =
		new PredictionContextCache();
	public static final int
		CSET1C=1, CSET1D=2, CSET0=3, NOTINSIDE=4, NOTBEFORE=5, NOTAFTER=6, NOTNEAR=7, 
		NOTOVERLAPS=8, NOTLALIGN=9, NOTRALIGN=10, ANDNOT=11, ORNOT=12, INSIDE=13, 
		BEFORE=14, AFTER=15, NEAR=16, OVERLAPS=17, LALIGN=18, RALIGN=19, AND=20, 
		OR=21, QVALUE=22, CONTAINS=23, STARTSWITH=24, ENDSWITH=25, REGEXP=26, 
		WILDCARDS=27, SIMILAR=28, LTEQ=29, GTEQ=30, EQ=31, EQN=32, NEQ=33, NEQN=34, 
		LT=35, GT=36, ID=37, SID=38, POSARG=39, LPAREN=40, RPAREN=41, LSQUARE=42, 
		RSQUARE=43, WS=44;
	public static final int
		RULE_query = 0, RULE_corSet = 1, RULE_docSet = 2, RULE_docExpr = 3, RULE_txtExpr = 4, 
		RULE_pair = 5, RULE_spair = 6, RULE_tpair = 7, RULE_locop = 8, RULE_locExpr = 9;
	private static String[] makeRuleNames() {
		return new String[] {
			"query", "corSet", "docSet", "docExpr", "txtExpr", "pair", "spair", "tpair", 
			"locop", "locExpr"
		};
	}
	public static final String[] ruleNames = makeRuleNames();

	private static String[] makeLiteralNames() {
		return new String[] {
			null, "'@@'", "'@'", "';'", null, null, null, null, null, null, null, 
			null, null, "'INSIDE'", "'BEFORE'", "'AFTER'", "'NEAR'", "'OVERLAPS'", 
			"'LALIGN'", "'RALIGN'", "'AND'", "'OR'", null, "'*='", "'^='", "'$='", 
			"'~='", "'?='", "'%='", "'<='", "'>='", "'='", "'=='", "'<>'", "'!='", 
			"'<'", "'>'", null, null, null, "'('", "')'", "'['", "']'"
		};
	}
	private static final String[] _LITERAL_NAMES = makeLiteralNames();
	private static String[] makeSymbolicNames() {
		return new String[] {
			null, "CSET1C", "CSET1D", "CSET0", "NOTINSIDE", "NOTBEFORE", "NOTAFTER", 
			"NOTNEAR", "NOTOVERLAPS", "NOTLALIGN", "NOTRALIGN", "ANDNOT", "ORNOT", 
			"INSIDE", "BEFORE", "AFTER", "NEAR", "OVERLAPS", "LALIGN", "RALIGN", 
			"AND", "OR", "QVALUE", "CONTAINS", "STARTSWITH", "ENDSWITH", "REGEXP", 
			"WILDCARDS", "SIMILAR", "LTEQ", "GTEQ", "EQ", "EQN", "NEQ", "NEQN", "LT", 
			"GT", "ID", "SID", "POSARG", "LPAREN", "RPAREN", "LSQUARE", "RSQUARE", 
			"WS"
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
	public String getGrammarFileName() { return "pythia.g4"; }

	@Override
	public String[] getRuleNames() { return ruleNames; }

	@Override
	public String getSerializedATN() { return _serializedATN; }

	@Override
	public ATN getATN() { return _ATN; }

	public pythiaParser(TokenStream input) {
		super(input);
		_interp = new ParserATNSimulator(this,_ATN,_decisionToDFA,_sharedContextCache);
	}

	public static class QueryContext extends ParserRuleContext {
		public TxtExprContext txtExpr() {
			return getRuleContext(TxtExprContext.class,0);
		}
		public CorSetContext corSet() {
			return getRuleContext(CorSetContext.class,0);
		}
		public DocSetContext docSet() {
			return getRuleContext(DocSetContext.class,0);
		}
		public QueryContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_query; }
	}

	public final QueryContext query() throws RecognitionException {
		QueryContext _localctx = new QueryContext(_ctx, getState());
		enterRule(_localctx, 0, RULE_query);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(21);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==CSET1C) {
				{
				setState(20);
				corSet();
				}
			}

			setState(24);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==CSET1D) {
				{
				setState(23);
				docSet();
				}
			}

			setState(26);
			txtExpr(0);
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

	public static class CorSetContext extends ParserRuleContext {
		public Token delim;
		public TerminalNode CSET1C() { return getToken(pythiaParser.CSET1C, 0); }
		public TerminalNode CSET0() { return getToken(pythiaParser.CSET0, 0); }
		public List<TerminalNode> ID() { return getTokens(pythiaParser.ID); }
		public TerminalNode ID(int i) {
			return getToken(pythiaParser.ID, i);
		}
		public CorSetContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_corSet; }
	}

	public final CorSetContext corSet() throws RecognitionException {
		CorSetContext _localctx = new CorSetContext(_ctx, getState());
		enterRule(_localctx, 2, RULE_corSet);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(28);
			((CorSetContext)_localctx).delim = match(CSET1C);
			setState(30); 
			_errHandler.sync(this);
			_la = _input.LA(1);
			do {
				{
				{
				setState(29);
				match(ID);
				}
				}
				setState(32); 
				_errHandler.sync(this);
				_la = _input.LA(1);
			} while ( _la==ID );
			setState(34);
			((CorSetContext)_localctx).delim = match(CSET0);
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

	public static class DocSetContext extends ParserRuleContext {
		public Token delim;
		public DocExprContext docExpr() {
			return getRuleContext(DocExprContext.class,0);
		}
		public TerminalNode CSET1D() { return getToken(pythiaParser.CSET1D, 0); }
		public TerminalNode CSET0() { return getToken(pythiaParser.CSET0, 0); }
		public DocSetContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_docSet; }
	}

	public final DocSetContext docSet() throws RecognitionException {
		DocSetContext _localctx = new DocSetContext(_ctx, getState());
		enterRule(_localctx, 4, RULE_docSet);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(36);
			((DocSetContext)_localctx).delim = match(CSET1D);
			setState(37);
			docExpr(0);
			setState(38);
			((DocSetContext)_localctx).delim = match(CSET0);
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

	public static class DocExprContext extends ParserRuleContext {
		public TerminalNode LPAREN() { return getToken(pythiaParser.LPAREN, 0); }
		public List<DocExprContext> docExpr() {
			return getRuleContexts(DocExprContext.class);
		}
		public DocExprContext docExpr(int i) {
			return getRuleContext(DocExprContext.class,i);
		}
		public TerminalNode RPAREN() { return getToken(pythiaParser.RPAREN, 0); }
		public TerminalNode LSQUARE() { return getToken(pythiaParser.LSQUARE, 0); }
		public TpairContext tpair() {
			return getRuleContext(TpairContext.class,0);
		}
		public TerminalNode RSQUARE() { return getToken(pythiaParser.RSQUARE, 0); }
		public TerminalNode AND() { return getToken(pythiaParser.AND, 0); }
		public TerminalNode OR() { return getToken(pythiaParser.OR, 0); }
		public TerminalNode ANDNOT() { return getToken(pythiaParser.ANDNOT, 0); }
		public TerminalNode ORNOT() { return getToken(pythiaParser.ORNOT, 0); }
		public DocExprContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_docExpr; }
	}

	public final DocExprContext docExpr() throws RecognitionException {
		return docExpr(0);
	}

	private DocExprContext docExpr(int _p) throws RecognitionException {
		ParserRuleContext _parentctx = _ctx;
		int _parentState = getState();
		DocExprContext _localctx = new DocExprContext(_ctx, _parentState);
		DocExprContext _prevctx = _localctx;
		int _startState = 6;
		enterRecursionRule(_localctx, 6, RULE_docExpr, _p);
		int _la;
		try {
			int _alt;
			enterOuterAlt(_localctx, 1);
			{
			setState(49);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case LPAREN:
				{
				setState(41);
				match(LPAREN);
				setState(42);
				docExpr(0);
				setState(43);
				match(RPAREN);
				}
				break;
			case LSQUARE:
				{
				setState(45);
				match(LSQUARE);
				setState(46);
				tpair();
				setState(47);
				match(RSQUARE);
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
			_ctx.stop = _input.LT(-1);
			setState(56);
			_errHandler.sync(this);
			_alt = getInterpreter().adaptivePredict(_input,4,_ctx);
			while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					if ( _parseListeners!=null ) triggerExitRuleEvent();
					_prevctx = _localctx;
					{
					{
					_localctx = new DocExprContext(_parentctx, _parentState);
					pushNewRecursionContext(_localctx, _startState, RULE_docExpr);
					setState(51);
					if (!(precpred(_ctx, 3))) throw new FailedPredicateException(this, "precpred(_ctx, 3)");
					setState(52);
					_la = _input.LA(1);
					if ( !((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << ANDNOT) | (1L << ORNOT) | (1L << AND) | (1L << OR))) != 0)) ) {
					_errHandler.recoverInline(this);
					}
					else {
						if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
						_errHandler.reportMatch(this);
						consume();
					}
					setState(53);
					docExpr(4);
					}
					} 
				}
				setState(58);
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,4,_ctx);
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

	public static class TxtExprContext extends ParserRuleContext {
		public TerminalNode LPAREN() { return getToken(pythiaParser.LPAREN, 0); }
		public List<TxtExprContext> txtExpr() {
			return getRuleContexts(TxtExprContext.class);
		}
		public TxtExprContext txtExpr(int i) {
			return getRuleContext(TxtExprContext.class,i);
		}
		public TerminalNode RPAREN() { return getToken(pythiaParser.RPAREN, 0); }
		public LocExprContext locExpr() {
			return getRuleContext(LocExprContext.class,0);
		}
		public PairContext pair() {
			return getRuleContext(PairContext.class,0);
		}
		public TerminalNode OR() { return getToken(pythiaParser.OR, 0); }
		public TerminalNode ORNOT() { return getToken(pythiaParser.ORNOT, 0); }
		public TxtExprContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_txtExpr; }
	}

	public final TxtExprContext txtExpr() throws RecognitionException {
		return txtExpr(0);
	}

	private TxtExprContext txtExpr(int _p) throws RecognitionException {
		ParserRuleContext _parentctx = _ctx;
		int _parentState = getState();
		TxtExprContext _localctx = new TxtExprContext(_ctx, _parentState);
		TxtExprContext _prevctx = _localctx;
		int _startState = 8;
		enterRecursionRule(_localctx, 8, RULE_txtExpr, _p);
		int _la;
		try {
			int _alt;
			enterOuterAlt(_localctx, 1);
			{
			setState(66);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,5,_ctx) ) {
			case 1:
				{
				setState(60);
				match(LPAREN);
				setState(61);
				txtExpr(0);
				setState(62);
				match(RPAREN);
				}
				break;
			case 2:
				{
				setState(64);
				locExpr();
				}
				break;
			case 3:
				{
				setState(65);
				pair();
				}
				break;
			}
			_ctx.stop = _input.LT(-1);
			setState(73);
			_errHandler.sync(this);
			_alt = getInterpreter().adaptivePredict(_input,6,_ctx);
			while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					if ( _parseListeners!=null ) triggerExitRuleEvent();
					_prevctx = _localctx;
					{
					{
					_localctx = new TxtExprContext(_parentctx, _parentState);
					pushNewRecursionContext(_localctx, _startState, RULE_txtExpr);
					setState(68);
					if (!(precpred(_ctx, 4))) throw new FailedPredicateException(this, "precpred(_ctx, 4)");
					setState(69);
					_la = _input.LA(1);
					if ( !(_la==ORNOT || _la==OR) ) {
					_errHandler.recoverInline(this);
					}
					else {
						if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
						_errHandler.reportMatch(this);
						consume();
					}
					setState(70);
					txtExpr(5);
					}
					} 
				}
				setState(75);
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,6,_ctx);
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

	public static class PairContext extends ParserRuleContext {
		public TerminalNode LSQUARE() { return getToken(pythiaParser.LSQUARE, 0); }
		public TerminalNode RSQUARE() { return getToken(pythiaParser.RSQUARE, 0); }
		public TpairContext tpair() {
			return getRuleContext(TpairContext.class,0);
		}
		public SpairContext spair() {
			return getRuleContext(SpairContext.class,0);
		}
		public PairContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_pair; }
	}

	public final PairContext pair() throws RecognitionException {
		PairContext _localctx = new PairContext(_ctx, getState());
		enterRule(_localctx, 10, RULE_pair);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(76);
			match(LSQUARE);
			setState(79);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case ID:
				{
				setState(77);
				tpair();
				}
				break;
			case SID:
				{
				setState(78);
				spair();
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
			setState(81);
			match(RSQUARE);
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

	public static class SpairContext extends ParserRuleContext {
		public Token name;
		public Token operator;
		public Token value;
		public TerminalNode SID() { return getToken(pythiaParser.SID, 0); }
		public TerminalNode QVALUE() { return getToken(pythiaParser.QVALUE, 0); }
		public TerminalNode EQ() { return getToken(pythiaParser.EQ, 0); }
		public TerminalNode NEQ() { return getToken(pythiaParser.NEQ, 0); }
		public TerminalNode CONTAINS() { return getToken(pythiaParser.CONTAINS, 0); }
		public TerminalNode STARTSWITH() { return getToken(pythiaParser.STARTSWITH, 0); }
		public TerminalNode ENDSWITH() { return getToken(pythiaParser.ENDSWITH, 0); }
		public TerminalNode REGEXP() { return getToken(pythiaParser.REGEXP, 0); }
		public TerminalNode WILDCARDS() { return getToken(pythiaParser.WILDCARDS, 0); }
		public TerminalNode SIMILAR() { return getToken(pythiaParser.SIMILAR, 0); }
		public TerminalNode EQN() { return getToken(pythiaParser.EQN, 0); }
		public TerminalNode NEQN() { return getToken(pythiaParser.NEQN, 0); }
		public TerminalNode LT() { return getToken(pythiaParser.LT, 0); }
		public TerminalNode LTEQ() { return getToken(pythiaParser.LTEQ, 0); }
		public TerminalNode GT() { return getToken(pythiaParser.GT, 0); }
		public TerminalNode GTEQ() { return getToken(pythiaParser.GTEQ, 0); }
		public SpairContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_spair; }
	}

	public final SpairContext spair() throws RecognitionException {
		SpairContext _localctx = new SpairContext(_ctx, getState());
		enterRule(_localctx, 12, RULE_spair);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(83);
			((SpairContext)_localctx).name = match(SID);
			setState(86);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if ((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << CONTAINS) | (1L << STARTSWITH) | (1L << ENDSWITH) | (1L << REGEXP) | (1L << WILDCARDS) | (1L << SIMILAR) | (1L << LTEQ) | (1L << GTEQ) | (1L << EQ) | (1L << EQN) | (1L << NEQ) | (1L << NEQN) | (1L << LT) | (1L << GT))) != 0)) {
				{
				setState(84);
				((SpairContext)_localctx).operator = _input.LT(1);
				_la = _input.LA(1);
				if ( !((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << CONTAINS) | (1L << STARTSWITH) | (1L << ENDSWITH) | (1L << REGEXP) | (1L << WILDCARDS) | (1L << SIMILAR) | (1L << LTEQ) | (1L << GTEQ) | (1L << EQ) | (1L << EQN) | (1L << NEQ) | (1L << NEQN) | (1L << LT) | (1L << GT))) != 0)) ) {
					((SpairContext)_localctx).operator = (Token)_errHandler.recoverInline(this);
				}
				else {
					if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
					_errHandler.reportMatch(this);
					consume();
				}
				setState(85);
				((SpairContext)_localctx).value = match(QVALUE);
				}
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

	public static class TpairContext extends ParserRuleContext {
		public Token name;
		public Token operator;
		public Token value;
		public TerminalNode ID() { return getToken(pythiaParser.ID, 0); }
		public TerminalNode QVALUE() { return getToken(pythiaParser.QVALUE, 0); }
		public TerminalNode EQ() { return getToken(pythiaParser.EQ, 0); }
		public TerminalNode NEQ() { return getToken(pythiaParser.NEQ, 0); }
		public TerminalNode CONTAINS() { return getToken(pythiaParser.CONTAINS, 0); }
		public TerminalNode STARTSWITH() { return getToken(pythiaParser.STARTSWITH, 0); }
		public TerminalNode ENDSWITH() { return getToken(pythiaParser.ENDSWITH, 0); }
		public TerminalNode REGEXP() { return getToken(pythiaParser.REGEXP, 0); }
		public TerminalNode WILDCARDS() { return getToken(pythiaParser.WILDCARDS, 0); }
		public TerminalNode SIMILAR() { return getToken(pythiaParser.SIMILAR, 0); }
		public TerminalNode EQN() { return getToken(pythiaParser.EQN, 0); }
		public TerminalNode NEQN() { return getToken(pythiaParser.NEQN, 0); }
		public TerminalNode LT() { return getToken(pythiaParser.LT, 0); }
		public TerminalNode LTEQ() { return getToken(pythiaParser.LTEQ, 0); }
		public TerminalNode GT() { return getToken(pythiaParser.GT, 0); }
		public TerminalNode GTEQ() { return getToken(pythiaParser.GTEQ, 0); }
		public TpairContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_tpair; }
	}

	public final TpairContext tpair() throws RecognitionException {
		TpairContext _localctx = new TpairContext(_ctx, getState());
		enterRule(_localctx, 14, RULE_tpair);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(88);
			((TpairContext)_localctx).name = match(ID);
			setState(91);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if ((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << CONTAINS) | (1L << STARTSWITH) | (1L << ENDSWITH) | (1L << REGEXP) | (1L << WILDCARDS) | (1L << SIMILAR) | (1L << LTEQ) | (1L << GTEQ) | (1L << EQ) | (1L << EQN) | (1L << NEQ) | (1L << NEQN) | (1L << LT) | (1L << GT))) != 0)) {
				{
				setState(89);
				((TpairContext)_localctx).operator = _input.LT(1);
				_la = _input.LA(1);
				if ( !((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << CONTAINS) | (1L << STARTSWITH) | (1L << ENDSWITH) | (1L << REGEXP) | (1L << WILDCARDS) | (1L << SIMILAR) | (1L << LTEQ) | (1L << GTEQ) | (1L << EQ) | (1L << EQN) | (1L << NEQ) | (1L << NEQN) | (1L << LT) | (1L << GT))) != 0)) ) {
					((TpairContext)_localctx).operator = (Token)_errHandler.recoverInline(this);
				}
				else {
					if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
					_errHandler.reportMatch(this);
					consume();
				}
				setState(90);
				((TpairContext)_localctx).value = match(QVALUE);
				}
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

	public static class LocopContext extends ParserRuleContext {
		public Token operator;
		public Token arg;
		public TerminalNode LPAREN() { return getToken(pythiaParser.LPAREN, 0); }
		public TerminalNode RPAREN() { return getToken(pythiaParser.RPAREN, 0); }
		public TerminalNode NEAR() { return getToken(pythiaParser.NEAR, 0); }
		public TerminalNode BEFORE() { return getToken(pythiaParser.BEFORE, 0); }
		public TerminalNode AFTER() { return getToken(pythiaParser.AFTER, 0); }
		public TerminalNode INSIDE() { return getToken(pythiaParser.INSIDE, 0); }
		public TerminalNode OVERLAPS() { return getToken(pythiaParser.OVERLAPS, 0); }
		public TerminalNode LALIGN() { return getToken(pythiaParser.LALIGN, 0); }
		public TerminalNode RALIGN() { return getToken(pythiaParser.RALIGN, 0); }
		public TerminalNode NOTNEAR() { return getToken(pythiaParser.NOTNEAR, 0); }
		public TerminalNode NOTBEFORE() { return getToken(pythiaParser.NOTBEFORE, 0); }
		public TerminalNode NOTAFTER() { return getToken(pythiaParser.NOTAFTER, 0); }
		public TerminalNode NOTINSIDE() { return getToken(pythiaParser.NOTINSIDE, 0); }
		public TerminalNode NOTOVERLAPS() { return getToken(pythiaParser.NOTOVERLAPS, 0); }
		public TerminalNode NOTLALIGN() { return getToken(pythiaParser.NOTLALIGN, 0); }
		public TerminalNode NOTRALIGN() { return getToken(pythiaParser.NOTRALIGN, 0); }
		public List<TerminalNode> POSARG() { return getTokens(pythiaParser.POSARG); }
		public TerminalNode POSARG(int i) {
			return getToken(pythiaParser.POSARG, i);
		}
		public LocopContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_locop; }
	}

	public final LocopContext locop() throws RecognitionException {
		LocopContext _localctx = new LocopContext(_ctx, getState());
		enterRule(_localctx, 16, RULE_locop);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			{
			setState(93);
			((LocopContext)_localctx).operator = _input.LT(1);
			_la = _input.LA(1);
			if ( !((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << NOTINSIDE) | (1L << NOTBEFORE) | (1L << NOTAFTER) | (1L << NOTNEAR) | (1L << NOTOVERLAPS) | (1L << NOTLALIGN) | (1L << NOTRALIGN) | (1L << INSIDE) | (1L << BEFORE) | (1L << AFTER) | (1L << NEAR) | (1L << OVERLAPS) | (1L << LALIGN) | (1L << RALIGN))) != 0)) ) {
				((LocopContext)_localctx).operator = (Token)_errHandler.recoverInline(this);
			}
			else {
				if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
				_errHandler.reportMatch(this);
				consume();
			}
			setState(94);
			match(LPAREN);
			setState(96); 
			_errHandler.sync(this);
			_la = _input.LA(1);
			do {
				{
				{
				setState(95);
				((LocopContext)_localctx).arg = match(POSARG);
				}
				}
				setState(98); 
				_errHandler.sync(this);
				_la = _input.LA(1);
			} while ( _la==POSARG );
			setState(100);
			match(RPAREN);
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

	public static class LocExprContext extends ParserRuleContext {
		public List<PairContext> pair() {
			return getRuleContexts(PairContext.class);
		}
		public PairContext pair(int i) {
			return getRuleContext(PairContext.class,i);
		}
		public List<LocopContext> locop() {
			return getRuleContexts(LocopContext.class);
		}
		public LocopContext locop(int i) {
			return getRuleContext(LocopContext.class,i);
		}
		public LocExprContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_locExpr; }
	}

	public final LocExprContext locExpr() throws RecognitionException {
		LocExprContext _localctx = new LocExprContext(_ctx, getState());
		enterRule(_localctx, 18, RULE_locExpr);
		try {
			int _alt;
			enterOuterAlt(_localctx, 1);
			{
			setState(102);
			pair();
			setState(106); 
			_errHandler.sync(this);
			_alt = 1;
			do {
				switch (_alt) {
				case 1:
					{
					{
					setState(103);
					locop();
					setState(104);
					pair();
					}
					}
					break;
				default:
					throw new NoViableAltException(this);
				}
				setState(108); 
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,11,_ctx);
			} while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER );
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
		case 3:
			return docExpr_sempred((DocExprContext)_localctx, predIndex);
		case 4:
			return txtExpr_sempred((TxtExprContext)_localctx, predIndex);
		}
		return true;
	}
	private boolean docExpr_sempred(DocExprContext _localctx, int predIndex) {
		switch (predIndex) {
		case 0:
			return precpred(_ctx, 3);
		}
		return true;
	}
	private boolean txtExpr_sempred(TxtExprContext _localctx, int predIndex) {
		switch (predIndex) {
		case 1:
			return precpred(_ctx, 4);
		}
		return true;
	}

	public static final String _serializedATN =
		"\3\u608b\ua72a\u8133\ub9ed\u417c\u3be7\u7786\u5964\3.q\4\2\t\2\4\3\t\3"+
		"\4\4\t\4\4\5\t\5\4\6\t\6\4\7\t\7\4\b\t\b\4\t\t\t\4\n\t\n\4\13\t\13\3\2"+
		"\5\2\30\n\2\3\2\5\2\33\n\2\3\2\3\2\3\3\3\3\6\3!\n\3\r\3\16\3\"\3\3\3\3"+
		"\3\4\3\4\3\4\3\4\3\5\3\5\3\5\3\5\3\5\3\5\3\5\3\5\3\5\5\5\64\n\5\3\5\3"+
		"\5\3\5\7\59\n\5\f\5\16\5<\13\5\3\6\3\6\3\6\3\6\3\6\3\6\3\6\5\6E\n\6\3"+
		"\6\3\6\3\6\7\6J\n\6\f\6\16\6M\13\6\3\7\3\7\3\7\5\7R\n\7\3\7\3\7\3\b\3"+
		"\b\3\b\5\bY\n\b\3\t\3\t\3\t\5\t^\n\t\3\n\3\n\3\n\6\nc\n\n\r\n\16\nd\3"+
		"\n\3\n\3\13\3\13\3\13\3\13\6\13m\n\13\r\13\16\13n\3\13\2\4\b\n\f\2\4\6"+
		"\b\n\f\16\20\22\24\2\6\4\2\r\16\26\27\4\2\16\16\27\27\3\2\31&\4\2\6\f"+
		"\17\25\2s\2\27\3\2\2\2\4\36\3\2\2\2\6&\3\2\2\2\b\63\3\2\2\2\nD\3\2\2\2"+
		"\fN\3\2\2\2\16U\3\2\2\2\20Z\3\2\2\2\22_\3\2\2\2\24h\3\2\2\2\26\30\5\4"+
		"\3\2\27\26\3\2\2\2\27\30\3\2\2\2\30\32\3\2\2\2\31\33\5\6\4\2\32\31\3\2"+
		"\2\2\32\33\3\2\2\2\33\34\3\2\2\2\34\35\5\n\6\2\35\3\3\2\2\2\36 \7\3\2"+
		"\2\37!\7\'\2\2 \37\3\2\2\2!\"\3\2\2\2\" \3\2\2\2\"#\3\2\2\2#$\3\2\2\2"+
		"$%\7\5\2\2%\5\3\2\2\2&\'\7\4\2\2\'(\5\b\5\2()\7\5\2\2)\7\3\2\2\2*+\b\5"+
		"\1\2+,\7*\2\2,-\5\b\5\2-.\7+\2\2.\64\3\2\2\2/\60\7,\2\2\60\61\5\20\t\2"+
		"\61\62\7-\2\2\62\64\3\2\2\2\63*\3\2\2\2\63/\3\2\2\2\64:\3\2\2\2\65\66"+
		"\f\5\2\2\66\67\t\2\2\2\679\5\b\5\68\65\3\2\2\29<\3\2\2\2:8\3\2\2\2:;\3"+
		"\2\2\2;\t\3\2\2\2<:\3\2\2\2=>\b\6\1\2>?\7*\2\2?@\5\n\6\2@A\7+\2\2AE\3"+
		"\2\2\2BE\5\24\13\2CE\5\f\7\2D=\3\2\2\2DB\3\2\2\2DC\3\2\2\2EK\3\2\2\2F"+
		"G\f\6\2\2GH\t\3\2\2HJ\5\n\6\7IF\3\2\2\2JM\3\2\2\2KI\3\2\2\2KL\3\2\2\2"+
		"L\13\3\2\2\2MK\3\2\2\2NQ\7,\2\2OR\5\20\t\2PR\5\16\b\2QO\3\2\2\2QP\3\2"+
		"\2\2RS\3\2\2\2ST\7-\2\2T\r\3\2\2\2UX\7(\2\2VW\t\4\2\2WY\7\30\2\2XV\3\2"+
		"\2\2XY\3\2\2\2Y\17\3\2\2\2Z]\7\'\2\2[\\\t\4\2\2\\^\7\30\2\2][\3\2\2\2"+
		"]^\3\2\2\2^\21\3\2\2\2_`\t\5\2\2`b\7*\2\2ac\7)\2\2ba\3\2\2\2cd\3\2\2\2"+
		"db\3\2\2\2de\3\2\2\2ef\3\2\2\2fg\7+\2\2g\23\3\2\2\2hl\5\f\7\2ij\5\22\n"+
		"\2jk\5\f\7\2km\3\2\2\2li\3\2\2\2mn\3\2\2\2nl\3\2\2\2no\3\2\2\2o\25\3\2"+
		"\2\2\16\27\32\"\63:DKQX]dn";
	public static final ATN _ATN =
		new ATNDeserializer().deserialize(_serializedATN.toCharArray());
	static {
		_decisionToDFA = new DFA[_ATN.getNumberOfDecisions()];
		for (int i = 0; i < _ATN.getNumberOfDecisions(); i++) {
			_decisionToDFA[i] = new DFA(_ATN.getDecisionState(i), i);
		}
	}
}