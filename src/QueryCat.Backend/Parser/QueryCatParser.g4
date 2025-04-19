parser grammar QueryCatParser;

@parser::header { #pragma warning disable 3021 } // Disable StyleCop warning CS3021 re CLSCompliant attribute in generated files.
@lexer::header { #pragma warning disable 3021 } // Disable StyleCop warning CS3021 re CLSCompliant attribute in generated files.

options {
    tokenVocab = QueryCatLexer;
}

/*
 * Program statements.
 */

program: programBody EOF;
programBody: SEMICOLON* statement (SEMICOLON statement)* SEMICOLON*;

statement
    : functionCall # StatementFunctionCall
    | declareVariable # StatementDeclareVariable
    | setVariable # StatementSetVariable
    | selectStatement # StatementSelectExpression
    | updateStatement # StatementUpdateExpression
    | insertStatement # StatementInsertExpression
    | deleteStatement # StatementDeleteExpression
    | echoStatement # StatementEcho
    | callStatement # StatementCall
    | ifStatement # StatementIf
    | whileStatement # StatementWhile
    | forStatement # StatementFor
    | breakStatement # StatementBreak
    | continueStatement # StatementContinue
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
      selectExcept?
      selectTarget?
      selectFromClause?
      selectWindow? # SelectQuerySpecificationFull
    ;
selectList: selectSublist (COMMA selectSublist)*;
selectExcept: EXCEPT identifier (COMMA identifier)*;
selectDistinctClause: ALL | DISTINCT | selectDistinctOnClause;
selectDistinctOnClause: DISTINCT ON '(' simpleExpression (COMMA simpleExpression)* ')';

// With.
selectWithClause: WITH RECURSIVE? selectWithElement (COMMA selectWithElement)*;
selectWithElement: name=identifierSimple ('(' selectWithColumnList ')')? AS '(' query=selectQueryExpression ')';
selectWithColumnList: name=identifier (COMMA name=identifier)*;

// Columns.
selectSublist
    : (identifierSimple '.')? STAR # SelectSublistAll
    | functionCall OVER (windowName=identifierSimple | selectWindowSpecification) selectAlias? # SelectSublistWindow
    | expression selectAlias? # SelectSublistExpression
    ;

// Into.
selectTarget: INTO (into=functionCall | uri=STRING_LITERAL | dash='-') (FORMAT format=functionCall)?;

// From.
selectFromClause:
    selectTableReferenceList
    selectSearchCondition?
    selectGroupBy?
    selectHaving?;
selectTableReferenceList:
    FROM selectTableReference (COMMA selectTableReference)*;
selectTableReference: selectTablePrimary selectTableJoined*;
selectTableValuesRow: '(' simpleExpression (COMMA simpleExpression)* ')';
selectTableValues: VALUES selectTableValuesRow (COMMA selectTableValuesRow)*;
selectTablePrimary
    : func=functionCall (FORMAT format=functionCall)? selectAlias? # SelectTablePrimaryNoFormat
    | '-' (FORMAT format=functionCall)? selectAlias? # SelectTablePrimaryStdin
    | uri=STRING_LITERAL (FORMAT format=functionCall)? selectAlias? # SelectTablePrimaryWithFormat
    | '(' selectQueryExpression ')' selectAlias? # SelectTablePrimarySubquery
    | name=identifier (FORMAT format=functionCall)? selectAlias? # SelectTablePrimaryIdentifier
    | '(' selectTableValues ')' selectAlias? # SelectTablePrimaryTableValues
    | simpleExpression (FORMAT format=functionCall)? selectAlias? # SelectTablePrimaryExpression
    ;
selectTableJoined
    : selectJoinType? JOIN right=selectTablePrimary ON condition=expression # SelectTableJoinedOn
    | selectJoinType? JOIN right=selectTablePrimary USING '(' identifier (COMMA identifier)* ')' # SelectTableJoinedUsing
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
    | uri=STRING_LITERAL (FORMAT functionCall)? selectAlias? # UpdateWithFormat
    | name=identifier selectAlias? # UpdateFromVariable
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
    : functionCall selectAlias? # InsertNoFormat
    | uri=STRING_LITERAL (FORMAT functionCall)? selectAlias? # InsertWithFormat
    | '-' (FORMAT format=functionCall)? selectAlias? # InsertStdout
    | name=identifier selectAlias? # InsertFromVariable
    ;
insertColumnsList: '(' name=identifier (',' name=identifier)* ')';
insertFromSource
    : selectQueryExpression # InsertSourceQuery
    | selectTableValues # InsertSourceTable
    ;

/*
 * ===============
 * DELETE command.
 * ===============
 */

deleteStatement:
    DELETE FROM
    deleteFromSource
    selectSearchCondition?;
deleteFromSource
    : functionCall selectAlias? # DeleteNoFormat
    | uri=STRING_LITERAL (FORMAT functionCall)? selectAlias? # DeleteWithFormat
    | name=identifier selectAlias? # DeleteFromVariable
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
 * ===============
 * IF command.
 * ===============
 */

ifStatement:
    IF mainIf=ifCondition
    (ELSEIF elseIf=ifCondition)*
    (ELSE elseBlock=blockExpression)?;
ifCondition: condition=expression THEN block=blockExpression;

/*
 * ===============
 * WHILE command.
 * ===============
 */

whileStatement:
    WHILE expression LOOP
        programBody
    END LOOP;

/*
 * ===============
 * FOR command.
 * ===============
 */

forStatement:
    FOR target=identifierSimple IN query=expression LOOP
        programBody
    END LOOP;

/*
 * ===============
 * BREAK command.
 * ===============
 */

breakStatement: BREAK;

/*
 * ===============
 * CONTINUE command.
 * ===============
 */

continueStatement: CONTINUE;

/*
 * ===============
 * General.
 * ===============
 */

identifierSimple
    : NO_QUOTES_IDENTIFIER # IdentifierSimpleNoQuotes
    | QUOTES_IDENTIFIER # IdentifierSimpleQuotes
    | '@' # IdentifierSimpleCurrent
    ;
identifier
    : name=identifierSimple identifierSelector* # IdentifierWithSelector
    | name=identifierSimple # IdentifierWithoutSource
    ;
identifierSelector
    : '.' name=identifierSimple # IdentifierSelectorProperty
    | '[' simpleExpression (',' simpleExpression)* ']' # IdentifierSelectorIndex
    | '[' '?' simpleExpression ']' # IdentifierSelectorFilterExpression
    ;
array: '(' expression (',' expression)* ')';
intervalLiteral: INTERVAL interval=STRING_LITERAL;

blockExpression : BEGIN SEMICOLON* statement (SEMICOLON statement)* SEMICOLON* END;

castOperand: CAST '(' value=simpleExpression AS type ')';
atTimeZone: AT (LOCAL | TIME ZONE tz=simpleExpression);
caseExpression: CASE arg=simpleExpression? caseWhen* (ELSE default=expression)? END;
caseWhen: WHEN condition=expression THEN result=expression;

// SQL functions.
standardFunction
    : CURRENT_DATE # StandardFunctionCurrentDate
    | CURRENT_TIMESTAMP # StandardFunctionCurrentTimestamp
    | TRIM '(' spec=(LEADING | TRAILING | BOTH)? characters=STRING_LITERAL? FROM? target=simpleExpression ')' # StandardFunctionTrim
    | POSITION '(' substring=STRING_LITERAL IN string=simpleExpression ')' # StandardFunctionPosition
    | EXTRACT '(' extractField=dateTimeField FROM source=simpleExpression ')' # StandardFunctionExtract
    | COALESCE '(' expression (COMMA expression)* ')' # StandardFunctionCoalesce
    | OCCURRENCES_REGEX '(' pattern=STRING_LITERAL IN string=simpleExpression ')' # StandardOccurrencesRegex
    | SUBSTRING_REGEX '(' pattern=STRING_LITERAL IN string=simpleExpression ')' # StandardSubstringRegex
    | POSITION_REGEX '(' pattern=STRING_LITERAL IN string=simpleExpression ')' # StandardPositionRegex
    | TRANSLATE_REGEX '(' pattern=STRING_LITERAL IN string=simpleExpression WITH replacement=STRING_LITERAL ')' # StandardTranslateRegex
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
    | standardFunction # ExpressionStandardFunctionCall
    | functionCall # ExpressionFunctionCall
    | identifier # ExpressionIdentifier
    | '(' expression ')' # ExpressionInParens
    | op=(PLUS | MINUS | NOT) right=expression # ExpressionUnary
    | left=expression op=(LESS_LESS | GREATER_GREATER) right=expression # ExpressionBinary
    | left=expression op=(STAR | DIV | MOD) right=expression # ExpressionBinary
    | left=expression op=(CONCAT | PLUS | MINUS) right=expression # ExpressionBinary
    | left=expression op=(EQUALS | NOT_EQUALS | GREATER | GREATER_OR_EQUALS | LESS | LESS_OR_EQUALS) right=expression # ExpressionBinary
    | left=expression NOT? op=LIKE right=expression # ExpressionBinary
    | left=expression NOT? op=SIMILAR TO right=expression # ExpressionBinary
    | left=expression NOT? op=IN right=array # ExpressionBinaryInArray
    | left=expression NOT? op=IN right=identifierSimple # ExpressionBinaryInIdentifier
    | left=expression NOT? op=IN right=selectQueryExpression # ExpressionBinaryInSubquery
    | expr=expression NOT? op=BETWEEN left=simpleExpression AND right=expression # ExpressionBetween
    | left=expression op=(EQUALS | NOT_EQUALS | GREATER | GREATER_OR_EQUALS | LESS | LESS_OR_EQUALS)
        condition=(ANY | SOME | ALL) '(' selectQueryExpression ')' # ExpressionSubquery
    | EXISTS '(' selectQueryExpression ')' # ExpressionExists
    | '(' selectQueryExpression ')' # ExpressionSelect
    | left=expression op=AND right=expression # ExpressionBinary
    | left=expression op=OR right=expression # ExpressionBinary
    | right=expression op=IS NOT? NULL # ExpressionUnary
    | left=expression atTimeZone # ExpressionAtTimeZone
    | caseExpression # ExpressionCase
    | blockExpression # ExpressionBlock
    ;

// Simple expression is subset of "expressons" to be used in clauses like BETWEEN.
// Because (BETWEEN x AND y) conflicts with plain (a AND b).
simpleExpression
    : literal # SimpleExpressionLiteral
    | castOperand # SimpleExpressionCast
    | right=simpleExpression TYPECAST type # SimpleExpressionBinaryCast
    | standardFunction # SimpleExpressionStandardFunctionCall
    | functionCall # SimpleExpressionFunctionCall
    | identifier # SimpleExpressionIdentifier
    | '(' simpleExpression ')' # SimpleExpressionInParens
    | op=(PLUS | MINUS | NOT) right=simpleExpression # SimpleExpressionUnary
    | left=simpleExpression op=(STAR | DIV | MOD) right=simpleExpression # SimpleExpressionBinary
    | left=simpleExpression op=(CONCAT | PLUS | MINUS) right=simpleExpression # SimpleExpressionBinary
    | left=simpleExpression op=(EQUALS | NOT_EQUALS | GREATER | GREATER_OR_EQUALS | LESS | LESS_OR_EQUALS) right=simpleExpression # SimpleExpressionBinary
    | caseExpression # SimpleExpressionCase
    | atTimeZone # SimpleExpressionAtTimeZone
    ;

literal
    : INTEGER_LITERAL # LiteralPlain
    | STRING_LITERAL # LiteralPlain
    | FLOAT_LITERAL # LiteralPlain
    | NUMERIC_LITERAL # LiteralPlain
    | TRUE # LiteralPlain
    | FALSE # LiteralPlain
    | NULL # LiteralPlain
    | intervalLiteral # LiteralInterval
    ;
