parser grammar QueryCatParser;

@parser::header { #pragma warning disable 3021 } // Disable StyleCop warning CS3021 re CLSCompliant attribute in generated files.
@lexer::header { #pragma warning disable 3021 } // Disable StyleCop warning CS3021 re CLSCompliant attribute in generated files.

options {
    tokenVocab = QueryCatLexer;
}

program: statement (SEMICOLON statement)* SEMICOLON? EOF;

statement
    : selectStatement # StatementSelectExpression
    | functionCall # StatementFunctionCall
    | echoStatement # StatementEcho
    | expression # StatementExpression
    ;

/*
 * FUNCTION command.
 */

functionSignature: IDENTIFIER '(' (functionArg (COMMA functionArg)*)? ')' (COLON functionType)? EOF;
functionType: type ('<' IDENTIFIER '>')?;
functionArg: variadic=ELLIPSIS? IDENTIFIER optional=QUESTION? COLON functionType isArray=LEFT_RIGHT_BRACKET? ('=' default=literal)?;

/*
 * ===============
 * SELECT command.
 * ===============
 */

selectStatement: selectExpression;
selectExpression: selectQuery (UNION selectQuery)*;
selectAlias: AS (name=(IDENTIFIER | STRING_LITERAL));
selectQuery
    : SELECT
        selectSetQuantifier?
        selectList
        selectTarget?
        selectFromClause
        selectOrderByClause?
        selectOffsetClause?
        selectFetchFirstClause? # SelectQueryFull
    | SELECT selectSublist (COMMA selectSublist)* selectTarget? # SelectQuerySingle
    ;
selectList: selectSublist (COMMA selectSublist)*;
selectSetQuantifier: ALL | DISTINCT;

// Columns.
selectSublist
    : STAR # SelectSublistAll
    | expression selectAlias? # SelectSublistExpression
    | IDENTIFIER (PERIOD IDENTIFIER)? selectAlias? # SelectSublistIdentifier
    ;

// Into.
selectTarget: INTO functionCall;

// From.
selectFromClause:
    selectTableReferenceList
    selectSearchCondition?
    selectGroupBy?
    selectHaving?;
selectTableReferenceList:
    FROM selectTableReference (COMMA selectTableReference)*;
selectTableReference
    : functionCall selectAlias? # SelectTableReferenceNoFormat
    | STRING_LITERAL (FORMAT functionCall)? selectAlias? # SelectTableReferenceWithFormat
    | '(' selectExpression ')' selectAlias? # SelectTableReferenceSubquery
    ;

// Group, Having.
selectGroupBy: GROUP BY expression (COMMA expression)*;
selectHaving: HAVING expression;

// Where.
selectSearchCondition: WHERE expression;

// Order.
selectOrderByClause: ORDER BY selectSortSpecification (COMMA selectSortSpecification)*;
selectSortSpecification: expression (ASC | DESC)?;

// Limit, offset.
selectOffsetClause: OFFSET (offset=expression) (ROW | ROWS)?;
selectFetchFirstClause: FETCH (FIRST | NEXT)? (limit=expression) (ROW | ROWS)? (ONLY | ONLY)?;

/*
 * =============
 * ECHO command.
 * =============
 */

echoStatement: ECHO expression;

/*
 * ========
 * General.
 * ========
 */

functionCall
    : IDENTIFIER '(' ( functionCallArg (COMMA functionCallArg)* )? ')'
    | IDENTIFIER '(' '*' ')' // Special case for COUNT(*).
    ;
functionCallArg: (IDENTIFIER ASSOCIATION)? expression;

// SQL functions.
standardFunction
    : CURRENT_DATE # standardFunctionCurrentDate
    | CURRENT_TIMESTAMP # standardFunctionCurrentTimestamp
    ;

type
    : INTEGER
    | STRING
    | FLOAT
    | TIMESTAMP
    | BOOLEAN
    | NUMERIC
    | OBJECT
    | ANY
    ;

// For reference: https://www.postgresql.org/docs/current/sql-syntax-lexical.html#SQL-PRECEDENCE
expression
    : literal # ExpressionLiteral
    | standardFunction # ExpressionStandardFunctionCall
    | functionCall # ExpressionFunctionCall
    | IDENTIFIER # ExpressionIdentifier
    | '(' expression ')' # ExpressionInParens
    | '(' selectExpression ')' # ExpressionSelect
    | left=expression op=CONCAT right=expression # ExpressionBinary
    | op=(PLUS | MINUS) right=expression # ExpressionUnary
    | left=expression op=(STAR | DIV | MOD) right=expression # ExpressionBinary
    | left=expression op=(PLUS | MINUS) right=expression # ExpressionBinary
    | left=expression op=(EQUALS | NOT_EQUALS | GREATER | GREATER_OR_EQUALS | LESS | LESS_OR_EQUALS) right=expression # ExpressionBinary
    | left=expression NOT? op=LIKE right=expression # ExpressionBinary
    | left=expression NOT? op=IN right=array # ExpressionBinaryIn
    | expr=expression NOT? op=BETWEEN left=simpleExpression AND right=expression # ExpressionBetween
    | left=expression op=AND right=expression # ExpressionBinary
    | left=expression op=OR right=expression # ExpressionBinary
    | right=expression op=IS NOT? NULL # ExpressionUnary
    | op=NOT right=expression # ExpressionUnary
    ;

array: '(' expression (',' expression)* ')';
intervalLiteral: INTERVAL interval=STRING_LITERAL;

// Simple expression is subset of "expressons" to be used in clauses like BETWEEN.
// Because (BETWEEN x AND y) conflicts with plain (a AND b).
simpleExpression
    : literal # SimpleExpressionLiteral
    | left=simpleExpression op=(STAR | DIV | MOD) right=simpleExpression # SimpleExpressionBinary
    | left=simpleExpression op=(PLUS | MINUS) right=simpleExpression # SimpleExpressionBinary
    | left=simpleExpression op=(EQUALS | NOT_EQUALS | GREATER | GREATER_OR_EQUALS | LESS | LESS_OR_EQUALS) right=simpleExpression # SimpleExpressionBinary
    ;

literal
    : INTEGER_LITERAL # literalPlain
    | FLOAT_LITERAL # literalPlain
    | NUMERIC_LITERAL # literalPlain
    | BOOLEAN_LITERAL # literalPlain
    | STRING_LITERAL # literalPlain
    | TRUE # literalPlain
    | FALSE # literalPlain
    | NULL # literalPlain
    | intervalLiteral # literalInterval
    ;
