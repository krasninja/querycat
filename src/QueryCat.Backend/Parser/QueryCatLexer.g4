lexer grammar QueryCatLexer;

@parser::header { #pragma warning disable 3021 } // Disable StyleCop warning CS3021 re CLSCompliant attribute in generated files.
@lexer::header { #pragma warning disable 3021 } // Disable StyleCop warning CS3021 re CLSCompliant attribute in generated files.

options { caseInsensitive = true; }

// General tokens.

LEFT_PAREN:         '(';
RIGHT_PAREN:        ')';
ASSIGN:             ':=';
ASSOCIATION:        '=>';
COLON:              ':';
COMMA:              ',';
PERIOD:             '.';
ELLIPSIS:           '...';
SEMICOLON:          ';';
QUESTION:           '?';
LEFT_BRACKET:       '[';
RIGHT_BRACKET:      ']';
LEFT_RIGHT_BRACKET: '[]';
PIPE:               '&>';
AT_SIGN:            '@';
EXCLAMATION_SIGN:   '!';
DOLLAR_SIGN:        '$';

// Math operations.

PLUS:               '+';
MINUS:              '-';
STAR:               '*';
DIV:                '/';
MOD:                '%';
EQUALS:             '=';
NOT_EQUALS:         '<>';
GREATER:            '>';
GREATER_OR_EQUALS:  '>=';
LESS:               '<';
LESS_OR_EQUALS:     '<=';
CONCAT:             '||';
LESS_LESS:          '<<';
GREATER_GREATER:    '>>';
TYPECAST:           '::';

// Types.

ANY:                'ANY';
BLOB:               'BLOB';
BOOL:               'BOOL';
BOOLEAN:            'BOOLEAN';
DECIMAL:            'DECIMAL';
FLOAT:              'FLOAT';
INT:                'INT';
INT8:               'INT8';
INTEGER:            'INTEGER';
NUMERIC:            'NUMERIC';
OBJECT:             'OBJECT';
REAL:               'REAL';
STRING:             'STRING';
TEXT:               'TEXT';
TIMESTAMP:          'TIMESTAMP';

// General grammar keywords.

AND:                'AND';
AS:                 'AS';
AT:                 'AT';
BEGIN:              'BEGIN';
BY:                 'BY';
CAST:               'CAST';
DEFAULT:            'DEFAULT';
END:                'END';
EXISTS:             'EXISTS';
FALSE:              'FALSE';
FROM:               'FROM';
IN:                 'IN';
IS:                 'IS';
LIKE:               'LIKE';
LIKE_REGEX:         'LIKE_REGEX';
NOT:                'NOT';
NULL:               'NULL';
ON:                 'ON';
ONLY:               'ONLY';
OR:                 'OR';
SOME:               'SOME';
TO:                 'TO';
TRUE:               'TRUE';
USING:              'USING';
VOID:               'VOID';

// Trim function.

TRIM:               'TRIM';
LEADING:            'LEADING';
TRAILING:           'TRAILING';
BOTH:               'BOTH';

// Datetime.

CURRENT_DATE:       'CURRENT_DATE';
CURRENT_TIMESTAMP:  'CURRENT_TIMESTAMP';
INTERVAL:           'INTERVAL';
YEAR:               'YEAR';
DOY:                'DOY';
DAYOFYEAR:          'DAYOFYEAR';
MONTH:              'MONTH';
DOW:                'DOW';
WEEKDAY:            'WEEKDAY';
DAY:                'DAY';
HOUR:               'HOUR';
MINUTE:             'MINUTE';
SECOND:             'SECOND';
MILLISECOND:        'MILLISECOND';
LOCAL:              'LOCAL';
TIME:               'TIME';
ZONE:               'ZONE';

// Other functions.

CASE:               'CASE';
COALESCE:           'COALESCE';
EXTRACT:            'EXTRACT';
POSITION:           'POSITION';
WHEN:               'WHEN';
OCCURRENCES_REGEX:  'OCCURRENCES_REGEX';
SUBSTRING_REGEX:    'SUBSTRING_REGEX';
POSITION_REGEX:     'POSITION_REGEX';
TRANSLATE_REGEX:    'TRANSLATE_REGEX';

// ECHO command.

ECHO:               'ECHO';

// SELECT command.

ALL:                'ALL';
ASC:                'ASC';
BETWEEN:            'BETWEEN';
CURRENT:            'CURRENT';
DESC:               'DESC';
DISTINCT:           'DISTINCT';
EXCEPT:             'EXCEPT';
FETCH:              'FETCH';
FIRST:              'FIRST';
FOLLOWING:          'FOLLOWING';
FORMAT:             'FORMAT';
FULL:               'FULL';
GROUP:              'GROUP';
HAVING:             'HAVING';
INNER:              'INNER';
INTERSECT:          'INTERSECT';
INTO:               'INTO';
JOIN:               'JOIN';
LAST:               'LAST';
LEFT:               'LEFT';
LIMIT:              'LIMIT';
NEXT:               'NEXT';
NULLS:              'NULLS';
OFFSET:             'OFFSET';
ORDER:              'ORDER';
OUTER:              'OUTER';
OVER:               'OVER';
PARTITION:          'PARTITION';
PRECEDING:          'PRECEDING';
RECURSIVE:          'RECURSIVE';
RIGHT:              'RIGHT';
ROW:                'ROW';
ROWS:               'ROWS';
SELECT:             'SELECT';
SIMILAR:            'SIMILAR';
TOP:                'TOP';
UNBOUNDED:          'UNBOUNDED';
UNION:              'UNION';
VALUES:             'VALUES';
WHERE:              'WHERE';
WINDOW:             'WINDOW';
WITH:               'WITH';

// UPDATE command.

UPDATE:             'UPDATE';

// INSERT command.

INSERT:             'INSERT';

// DECLARE/SET command.

DECLARE:            'DECLARE';
SET:                'SET';

// CALL command.

CALL:               'CALL';

// IF command.

IF:                 'IF';
THEN:               'THEN';
ELSE:               'ELSE';
ELSEIF:             'ELSEIF';

// WHILE command.

WHILE:              'WHILE';
BREAK:              'BREAK';
CONTINUE:           'CONTINUE';

// FOR command.

FOR:                'FOR';

TYPE: ANY | BLOB | BOOL | BOOLEAN | DECIMAL | FLOAT | INT | INT8 | INTEGER | NUMERIC | OBJECT | REAL | STRING | TEXT
    | TIMESTAMP;

// https://github.com/antlr/antlr4/blob/master/doc/lexicon.md#identifiers

fragment NAME_CHAR
    : NAME_START_CHAR
    | '0'..'9'
    | '_'
    | '\u00B7'
    | '\u0300'..'\u036F'
    | '\u203F'..'\u2040'
    ;
fragment NAME_START_CHAR
    : 'A'..'Z'
    | '_'
    | '\u00C0'..'\u00D6'
    | '\u00F8'..'\u02FF'
    | '\u0370'..'\u037D'
    | '\u037F'..'\u1FFF'
    | '\u200C'..'\u200D'
    | '\u2070'..'\u218F'
    | '\u2C00'..'\u2FEF'
    | '\u3001'..'\uD7FF'
    | '\uF900'..'\uFDCF'
    | '\uFDF0'..'\uFFFD'
    ;

NO_QUOTES_IDENTIFIER: NAME_START_CHAR NAME_CHAR*;
QUOTES_IDENTIFIER: '"' ( ~'"' | '""')* '"';

fragment HEX_DIGIT: [0-9A-F];
fragment DIGIT: [0-9];

INTEGER_LITERAL: DIGIT+;
FLOAT_LITERAL
    : ((DIGIT+ ('.' DIGIT*)?)
    | ('.' DIGIT+)) ('E' [-+]? DIGIT+)?
    | '0x' HEX_DIGIT+
    ;
NUMERIC_LITERAL: FLOAT_LITERAL'M';
STRING_LITERAL
    : '\'' ( ~'\'' | '\'\'')* '\''
    | ('E\'' | 'e\'') ( ~'\'' | '\'\'')* '\''
    ;
BOOLEAN_LITERAL: TRUE | FALSE;

// Comments.

SINGLE_LINE_COMMENT: ('--' | '#!') ~[\r\n]* (('\r'? '\n') | EOF) -> channel(HIDDEN);
MULTILINE_COMMENT: '/*' .*? '*/' -> channel(HIDDEN);
SPACES: [ \t\r\n] -> channel(HIDDEN);
