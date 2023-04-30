parser grammar QueryCatParser;

@parser::header { #pragma warning disable 3021 } // Disable StyleCop warning CS3021 re CLSCompliant attribute in generated files.
@lexer::header { #pragma warning disable 3021 } // Disable StyleCop warning CS3021 re CLSCompliant attribute in generated files.

options {
    tokenVocab = QueryCatLexer;
}

/*
 * Program statements.
 */

program: SEMICOLON* statement (SEMICOLON statement)* SEMICOLON* EOF;

statement
    : functionCall # StatementFunctionCall
    | selectStatement # StatementSelectExpression
    | updateStatement # StatementUpdateExpression
    | insertStatement # StatementInsertExpression
    | echoStatement # StatementEcho
    | declareVariable # StatementDeclareVariable
    | setVariable # StatementSetVariable
    | expression # StatementExpression
    ;

/*
 * ===============
 * FUNCTION command.
 * ===============
 */

functionSignature: name=IDENTIFIER '(' (functionArg (COMMA functionArg)*)? ')' (COLON functionType)? EOF;
functionType: type ('<' IDENTIFIER '>')?;
functionArg: variadic=ELLIPSIS? IDENTIFIER optional=QUESTION? COLON functionType isArray=LEFT_RIGHT_BRACKET?
    (('=' | ':=' | DEFAULT) default=literal)?;

functionCall
    : IDENTIFIER '(' ( functionCallArg (COMMA functionCallArg)* )? ')'
    | IDENTIFIER '(' '*' ')' // Special case for COUNT(*).
    ;
functionCallArg: (IDENTIFIER ASSOCIATION)? expression;

/*
 * ===============
 * DECLARE/SET command.
 * ===============
 */

declareVariable: DECLARE IDENTIFIER type (':=' statement)?;
setVariable: SET IDENTIFIER ':=' statement;

/*
 * ===============
 * SELECT command.
 * ===============
 */

selectStatement: selectQueryExpression;

// Order.
selectOrderByClause: ORDER BY selectSortSpecification (COMMA selectSortSpecification)*;
selectSortSpecification: expression (ASC | DESC)? ((NULLS FIRST) | (NULLS LAST))?;

// Select query.
selectAlias: AS? (name=(IDENTIFIER | STRING_LITERAL));
selectQueryExpression
    : selectWithClause?
      SELECT
      selectTopClause?
      selectDistinctClause?
      selectList
      selectTarget?
      selectFromClause
      selectWindow?
      selectOrderByClause?
      selectLimitClause?
      selectOffsetClause?
      selectFetchFirstClause? # SelectQueryExpressionSimple
    | selectWithClause?
      selectQueryExpressionBody
      selectOrderByClause?
      selectLimitClause?
      selectOffsetClause?
      selectFetchFirstClause? # SelectQueryExpressionFull
    ;
selectQueryExpressionBody
    : left=selectQueryPrimary # SelectQueryExpressionBodyPrimary
    | left=selectQueryExpressionBody INTERSECT (DISTINCT | ALL)? right=selectQueryExpressionBody # SelectQueryExpressionBodyIntersect
    | left=selectQueryExpressionBody (UNION | EXCEPT) (DISTINCT | ALL)? right=selectQueryExpressionBody # SelectQueryExpressionBodyUnionExcept
    ;
selectQueryPrimary
    : selectQuerySpecification # SelectQueryPrimaryNoParens
    | '(' selectQueryExpression ')' # SelectQueryPrimaryParens
    ;
selectQuerySpecification
    : selectWithClause?
      SELECT
      selectTopClause?
      selectDistinctClause?
      selectList
      selectTarget?
      selectFromClause
      selectWindow? # SelectQuerySpecificationFull
    | SELECT selectSublist (COMMA selectSublist)* selectTarget? # SelectQuerySpecificationSingle
    ;
selectList: selectSublist (COMMA selectSublist)*;
selectDistinctClause: ALL | DISTINCT | selectDistinctOnClause;
selectDistinctOnClause: DISTINCT ON '(' simpleExpression (COMMA simpleExpression)* ')';

// With.
selectWithClause: WITH RECURSIVE? selectWithElement (COMMA selectWithElement)*;
selectWithElement: name=IDENTIFIER ('(' selectWithColumnList ')')? AS '(' query=selectQueryExpression ')';
selectWithColumnList: name=identifierChain (COMMA name=identifierChain)*;

// Columns.
selectSublist
    : STAR # SelectSublistAll
    | functionCall OVER (windowName=IDENTIFIER | selectWindowSpecification) selectAlias? # SelectSublistWindow
    | expression selectAlias? # SelectSublistExpression
    ;

// Into.
selectTarget: INTO (functionCall | uri=STRING_LITERAL);

// From.
selectFromClause:
    selectTableReferenceList
    selectSearchCondition?
    selectGroupBy?
    selectHaving?;
selectTableReferenceList:
    FROM selectTableReference (COMMA selectTableReference)*;
selectTableReference: selectTablePrimary selectTableJoined*;
selectTableRow: '(' simpleExpression (COMMA simpleExpression)* ')';
selectTable: VALUES selectTableRow (COMMA selectTableRow)*;
selectTablePrimary
    : functionCall selectAlias? # SelectTablePrimaryNoFormat
    | '-' selectAlias? # SelectTablePrimaryStdin
    | uri=STRING_LITERAL (FORMAT functionCall)? selectAlias? # SelectTablePrimaryWithFormat
    | '(' selectQueryExpression ')' selectAlias? # SelectTablePrimarySubquery
    | name=IDENTIFIER selectAlias? # SelectTablePrimaryIdentifier
    | '(' selectTable ')' selectAlias? # SelectTablePrimaryTable
    ;
selectTableJoined
    : selectJoinType? JOIN right=selectTablePrimary ON condition=expression # SelectTableJoinedOn
    | selectJoinType? JOIN right=selectTablePrimary USING '(' IDENTIFIER (COMMA IDENTIFIER)* ')' # SelectTableJoinedUsing
    ;
selectJoinType: INNER | (LEFT | RIGHT | FULL) OUTER?;

// Group, Having.
selectGroupBy: GROUP BY expression (COMMA expression)*;
selectHaving: HAVING expression;

// Where.
selectSearchCondition: WHERE expression;

// Window.
selectWindowSpecification: '(' existingWindowName=IDENTIFIER? selectWindowPartitionClause?
    selectWindowOrderClause? ')';
selectWindowPartitionClause: PARTITION BY expression (COMMA expression)*;
selectWindowOrderClause: ORDER BY selectSortSpecification (COMMA selectSortSpecification)*;
selectWindow: WINDOW selectWindowDefinitionList (COMMA selectWindowDefinitionList)*;
selectWindowDefinitionList: name=IDENTIFIER AS selectWindowSpecification;

// Limit, offset.
selectOffsetClause: OFFSET (offset=expression) (ROW | ROWS)?;
selectFetchFirstClause: (FETCH | LIMIT) (FIRST | NEXT)? (limit=expression) (ROW | ROWS)? (ONLY | ONLY)?;
selectTopClause: TOP limit=INTEGER_LITERAL;
selectLimitClause: LIMIT limit=expression;

/*
 * ===============
 * UPDATE command.
 * ===============
 */

updateStatement:
    UPDATE
    updateSource
    SET
    updateSetClause (',' updateSetClause)*
    selectSearchCondition?;
updateSource
    : functionCall selectAlias? # UpdateNoFormat
    | uri=STRING_LITERAL (FORMAT functionCall)? # UpdateWithFormat
    | name=identifierChain # UpdateFromVariable
    ;
updateSetClause: source=identifierChain '=' target=expression;

/*
 * ===============
 * INSERT command.
 * ===============
 */

insertStatement:
    INSERT INTO
    insertToSource
    insertColumnsList?
    insertFromSource;
insertToSource
    : functionCall # InsertNoFormat
    | uri=STRING_LITERAL (FORMAT functionCall)? # InsertWithFormat
    | name=identifierChain # InsertFromVariable
    ;
insertColumnsList: '(' name=identifierChain (',' name=identifierChain)* ')';
insertFromSource
    : selectQueryExpression # InsertSourceQuery
    | selectTable # InsertSourceTable
    ;

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

identifierChain
    : source=IDENTIFIER PERIOD name=IDENTIFIER? # identifierChainFull
    | name=IDENTIFIER # identifierChainSimple
    ;
array: '(' expression (',' expression)* ')';
intervalLiteral: INTERVAL interval=STRING_LITERAL;

castOperand: CAST '(' value=simpleExpression AS type ')';
atTimeZone: AT (LOCAL | TIME ZONE tz=simpleExpression);
caseExpression: CASE arg=simpleExpression? caseWhen* (ELSE default=expression)? END;
caseWhen: WHEN condition=expression THEN result=expression;

// SQL functions.
standardFunction
    : CURRENT_DATE # standardFunctionCurrentDate
    | CURRENT_TIMESTAMP # standardFunctionCurrentTimestamp
    | TRIM '(' spec=(LEADING | TRAILING | BOTH)? characters=STRING_LITERAL? FROM? target=simpleExpression ')' # standardFunctionTrim
    | POSITION '(' substring=STRING_LITERAL IN string=simpleExpression ')' # standardFunctionPosition
    | EXTRACT '(' extractField=dateTimeField FROM source=simpleExpression ')' # standardFunctionExtract
    | COALESCE '(' expression (COMMA expression)* ')' # standardFunctionCoalesce
    ;

dateTimeField
    : YEAR
    | DOY
    | DAYOFYEAR
    | MONTH
    | DOW
    | WEEKDAY
    | DAY
    | HOUR
    | MINUTE
    | SECOND
    | MILLISECOND
    ;

type
    : INTEGER
    | STRING
    | FLOAT
    | TIMESTAMP
    | INTERVAL
    | BOOLEAN
    | NUMERIC
    | OBJECT
    | ANY
    | VOID
    ;

// For reference: https://www.postgresql.org/docs/current/sql-syntax-lexical.html#SQL-PRECEDENCE
expression
    : literal # ExpressionLiteral
    | castOperand # ExpressionCast
    | left=expression atTimeZone # ExpressionAtTimeZone
    | standardFunction # ExpressionStandardFunctionCall
    | functionCall # ExpressionFunctionCall
    | caseExpression # ExpressionCase
    | identifierChain # ExpressionIdentifier
    | '(' expression ')' # ExpressionInParens
    | '(' selectQueryExpression ')' # ExpressionSelect
    | left=expression op=CONCAT right=expression # ExpressionBinary
    | right=expression TYPECAST type # ExpressionBinaryCast
    | op=(PLUS | MINUS) right=expression # ExpressionUnary
    | left=expression op=(LESS_LESS | GREATER_GREATER) right=expression # ExpressionBinary
    | left=expression op=(STAR | DIV | MOD) right=expression # ExpressionBinary
    | left=expression op=(PLUS | MINUS) right=expression # ExpressionBinary
    | left=expression op=(EQUALS | NOT_EQUALS | GREATER | GREATER_OR_EQUALS | LESS | LESS_OR_EQUALS) right=expression # ExpressionBinary
    | left=expression NOT? op=LIKE right=expression # ExpressionBinary
    | left=expression NOT? op=SIMILAR TO right=expression # ExpressionBinary
    | left=expression NOT? op=IN right=array # ExpressionBinaryIn
    | expr=expression NOT? op=BETWEEN left=simpleExpression AND right=expression # ExpressionBetween
    | EXISTS '(' selectQueryExpression ')' # ExpressionExists
    | left=simpleExpression op=(EQUALS | NOT_EQUALS | GREATER | GREATER_OR_EQUALS | LESS | LESS_OR_EQUALS)
        condition=(ANY | SOME | ALL) '(' selectQueryExpression ')' # ExpressionSubquery
    | left=expression op=AND right=expression # ExpressionBinary
    | left=expression op=OR right=expression # ExpressionBinary
    | right=expression op=IS NOT? NULL # ExpressionUnary
    | op=NOT right=expression # ExpressionUnary
    ;

// Simple expression is subset of "expressons" to be used in clauses like BETWEEN.
// Because (BETWEEN x AND y) conflicts with plain (a AND b).
simpleExpression
    : literal # SimpleExpressionLiteral
    | castOperand # SimpleExpressionCast
    | atTimeZone # SimpleExpressionAtTimeZone
    | standardFunction # SimpleExpressionStandardFunctionCall
    | functionCall # SimpleExpressionFunctionCall
    | caseExpression # SimpleExpressionCase
    | identifierChain # SimpleExpressionIdentifier
    | '(' simpleExpression ')' # SimpleExpressionInParens
    | op=(PLUS | MINUS) right=expression # SimpleExpressionUnary
    | left=simpleExpression op=CONCAT right=simpleExpression # SimpleExpressionBinary
    | right=simpleExpression TYPECAST type # SimpleExpressionBinaryCast
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
