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
    | callStatement # StatementCall
    | declareVariable # StatementDeclareVariable
    | setVariable # StatementSetVariable
    | expression # StatementExpression
    ;

/*
 * ===============
 * FUNCTION command.
 * ===============
 */

functionSignature: name=identifierSimple '(' (functionArg (COMMA functionArg)*)? ')' (COLON functionType)? EOF;
functionType: type ('<' identifierSimple '>')?;
functionArg: variadic=ELLIPSIS? identifierSimple optional=QUESTION? COLON functionType isArray=LEFT_RIGHT_BRACKET?
    (('=' | ':=' | DEFAULT) default=literal)?;

functionCall
    : identifierSimple '(' ( functionCallArg (COMMA functionCallArg)* )? ')'
    | identifierSimple '(' '*' ')' // Special case for COUNT(*).
    ;
functionCallArg: (identifierSimple ASSOCIATION)? expression;

/*
 * ===============
 * DECLARE/SET command.
 * ===============
 */

declareVariable: DECLARE identifierSimple type (':=' statement)?;
setVariable: SET identifier ':=' statement;

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
selectAlias: AS? (identifierSimple | STRING_LITERAL);
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
selectWithElement: name=identifierSimple ('(' selectWithColumnList ')')? AS '(' query=selectQueryExpression ')';
selectWithColumnList: name=identifier (COMMA name=identifier)*;

// Columns.
selectSublist
    : STAR # SelectSublistAll
    | functionCall OVER (windowName=identifierSimple | selectWindowSpecification) selectAlias? # SelectSublistWindow
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
    : func=functionCall (FORMAT format=functionCall)? selectAlias? # SelectTablePrimaryNoFormat
    | '-' (FORMAT format=functionCall)? selectAlias? # SelectTablePrimaryStdin
    | uri=STRING_LITERAL (FORMAT format=functionCall)? selectAlias? # SelectTablePrimaryWithFormat
    | '(' selectQueryExpression ')' selectAlias? # SelectTablePrimarySubquery
    | name=identifierSimple (FORMAT format=functionCall)? selectAlias? # SelectTablePrimaryIdentifier
    | '(' selectTable ')' selectAlias? # SelectTablePrimaryTable
    ;
selectTableJoined
    : selectJoinType? JOIN right=selectTablePrimary ON condition=expression # SelectTableJoinedOn
    | selectJoinType? JOIN right=selectTablePrimary USING '(' identifierSimple (COMMA identifierSimple)* ')' # SelectTableJoinedUsing
    ;
selectJoinType: INNER | (LEFT | RIGHT | FULL) OUTER?;

// Group, Having.
selectGroupBy: GROUP BY expression (COMMA expression)*;
selectHaving: HAVING expression;

// Where.
selectSearchCondition: WHERE expression;

// Window.
selectWindowSpecification: '(' existingWindowName=identifierSimple? selectWindowPartitionClause?
    selectWindowOrderClause? ')';
selectWindowPartitionClause: PARTITION BY expression (COMMA expression)*;
selectWindowOrderClause: ORDER BY selectSortSpecification (COMMA selectSortSpecification)*;
selectWindow: WINDOW selectWindowDefinitionList (COMMA selectWindowDefinitionList)*;
selectWindowDefinitionList: name=identifier AS selectWindowSpecification;

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
    | name=identifier # UpdateFromVariable
    ;
updateSetClause: source=identifier '=' target=expression;

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
    | name=identifier # InsertFromVariable
    ;
insertColumnsList: '(' name=identifier (',' name=identifier)* ')';
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
 * =============
 * CALL command.
 * =============
 */

callStatement: CALL functionCall;

/*
 * ========
 * General.
 * ========
 */

identifierSimple
    : NO_QUOTES_IDENTIFIER
    | QUOTES_IDENTIFIER
    ;
identifier
    : name=identifierSimple identifierSelector* # IdentifierWithSelector
    | name=identifierSimple # IdentifierWithoutSource
    ;
identifierSelector
    : '.' name=identifierSimple # IdentifierSelectorProperty
    | '[' simpleExpression (',' simpleExpression)* ']' # IdentifierSelectorIndex
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
    | OCCURRENCES_REGEX '(' pattern=STRING_LITERAL IN string=simpleExpression ')' # standardOccurrencesRegex
    | SUBSTRING_REGEX '(' pattern=STRING_LITERAL IN string=simpleExpression ')' # standardSubstringRegex
    | POSITION_REGEX '(' pattern=STRING_LITERAL IN string=simpleExpression ')' # standardPositionRegex
    | TRANSLATE_REGEX '(' pattern=STRING_LITERAL IN string=simpleExpression WITH replacement=STRING_LITERAL ')' # standardTranslateRegex
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
    : INTEGER | INT | INT8
    | STRING | TEXT
    | FLOAT | REAL
    | TIMESTAMP
    | INTERVAL
    | BLOB
    | BOOLEAN | BOOL
    | NUMERIC | DECIMAL
    | OBJECT
    | ANY
    | VOID
    ;

// For reference: https://www.postgresql.org/docs/current/sql-syntax-lexical.html#SQL-PRECEDENCE
expression
    : literal # ExpressionLiteral
    | castOperand # ExpressionCast
    | right=expression TYPECAST type # ExpressionBinaryCast
    | left=expression atTimeZone # ExpressionAtTimeZone
    | standardFunction # ExpressionStandardFunctionCall
    | functionCall # ExpressionFunctionCall
    | caseExpression # ExpressionCase
    | identifier # ExpressionIdentifier
    | '(' expression ')' # ExpressionInParens
    | '(' selectQueryExpression ')' # ExpressionSelect
    | left=expression op=CONCAT right=expression # ExpressionBinary
    | op=(PLUS | MINUS) right=expression # ExpressionUnary
    | left=expression op=(LESS_LESS | GREATER_GREATER) right=expression # ExpressionBinary
    | left=expression op=(STAR | DIV | MOD) right=expression # ExpressionBinary
    | left=expression op=(PLUS | MINUS) right=expression # ExpressionBinary
    | left=expression op=(EQUALS | NOT_EQUALS | GREATER | GREATER_OR_EQUALS | LESS | LESS_OR_EQUALS) right=expression # ExpressionBinary
    | left=expression NOT? op=LIKE right=expression # ExpressionBinary
    | left=expression NOT? op=SIMILAR TO right=expression # ExpressionBinary
    | left=expression NOT? op=IN right=array # ExpressionBinaryInArray
    | left=expression NOT? op=IN right=selectQueryExpression # ExpressionBinaryInSubquery
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
    | right=simpleExpression TYPECAST type # SimpleExpressionBinaryCast
    | atTimeZone # SimpleExpressionAtTimeZone
    | standardFunction # SimpleExpressionStandardFunctionCall
    | functionCall # SimpleExpressionFunctionCall
    | caseExpression # SimpleExpressionCase
    | identifier # SimpleExpressionIdentifier
    | '(' simpleExpression ')' # SimpleExpressionInParens
    | op=(PLUS | MINUS) right=expression # SimpleExpressionUnary
    | left=simpleExpression op=CONCAT right=simpleExpression # SimpleExpressionBinary
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
