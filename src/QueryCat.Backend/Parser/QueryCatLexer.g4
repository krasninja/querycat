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

INTEGER:            'INTEGER';
STRING:             'STRING';
FLOAT:              'FLOAT';
TIMESTAMP:          'TIMESTAMP';
BOOLEAN:            'BOOLEAN';
NUMERIC:            'NUMERIC';
OBJECT:             'OBJECT';
ANY:                'ANY';

// General grammar keywords.

AND:                'AND';
AS:                 'AS';
BY:                 'BY';
CAST:               'CAST';
DEFAULT:            'DEFAULT';
ELSE:               'ELSE';
END:                'END';
EXISTS:             'EXISTS';
FALSE:              'FALSE';
FROM:               'FROM';
IF:                 'IF';
IN:                 'IN';
IS:                 'IS';
LIKE:               'LIKE';
NOT:                'NOT';
NULL:               'NULL';
ON:                 'ON';
ONLY:               'ONLY';
OR:                 'OR';
SOME:               'SOME';
THEN:               'THEN';
TO:                 'TO';
TRUE:               'TRUE';
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
MONTH:              'MONTH';
DAY:                'DAY';
HOUR:               'HOUR';
MINUTE:             'MINUTE';
SECOND:             'SECOND';
MILLISECOND:        'MILLISECOND';

// Other functions.

CASE:               'CASE';
COALESCE:           'COALESCE';
EXTRACT:            'EXTRACT';
POSITION:           'POSITION';
WHEN:               'WHEN';

// ECHO command.

ECHO:               'ECHO';

// SELECT command.

ALL:                'ALL';
ASC:                'ASC';
BETWEEN:            'BETWEEN';
DESC:               'DESC';
DISTINCT:           'DISTINCT';
FETCH:              'FETCH';
FIRST:              'FIRST';
FORMAT:             'FORMAT';
FULL:               'FULL';
GROUP:              'GROUP';
HAVING:             'HAVING';
INNER:              'INNER';
INTO:               'INTO';
JOIN:               'JOIN';
LEFT:               'LEFT';
LIMIT:              'LIMIT';
NEXT:               'NEXT';
OFFSET:             'OFFSET';
ORDER:              'ORDER';
OUTER:              'OUTER';
RECURSIVE:          'RECURSIVE';
RIGHT:              'RIGHT';
ROW:                'ROW';
ROWS:               'ROWS';
SELECT:             'SELECT';
TOP:                'TOP';
UNION:              'UNION';
WHERE:              'WHERE';
WITH:               'WITH';

TYPE: INTEGER | STRING | FLOAT | TIMESTAMP | BOOLEAN | NUMERIC | OBJECT | ANY;

// https://github.com/antlr/antlr4/blob/master/doc/lexicon.md#identifiers

fragment NameChar
   : NameStartChar
   | '0'..'9'
   | '_'
   | '\u00B7'
   | '\u0300'..'\u036F'
   | '\u203F'..'\u2040'
   ;
fragment NameStartChar
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

IDENTIFIER
    : NameStartChar NameChar*
    | '[' (~']' | ']' ']')* ']'
    ;

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
    | '"' ( ~'"' | '""')* '"'
    ;
BOOLEAN_LITERAL: TRUE | FALSE;

// Comments.

SINGLE_LINE_COMMENT: '--' ~[\r\n]* (('\r'? '\n') | EOF) -> channel(HIDDEN);
MULTILINE_COMMENT: '/*' .*? '*/' -> channel(HIDDEN);
SPACES: [ \u000B\t\r\n] -> channel(HIDDEN);
