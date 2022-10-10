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

TRUE:               'TRUE';
FALSE:              'FALSE';
NULL:               'NULL';
AND:                'AND';
OR:                 'OR';
IN:                 'IN';
IS:                 'IS';
NOT:                'NOT';
LIKE:               'LIKE';
VOID:               'VOID';
AS:                 'AS';
TO:                 'TO';
BY:                 'BY';
ONLY:               'ONLY';
DEFAULT:            'DEFAULT';
CAST:               'CAST';

// ECHO command.

ECHO:               'ECHO';

// SELECT command.

BETWEEN:            'BETWEEN';
OFFSET:             'OFFSET';
ROW:                'ROW';
ROWS:               'ROWS';
FETCH:              'FETCH';
FIRST:              'FIRST';
NEXT:               'NEXT';
ORDER:              'ORDER';
ASC:                'ASC';
DESC:               'DESC';
HAVING:             'HAVING';
WHERE:              'WHERE';
UNION:              'UNION';
GROUP:              'GROUP';
INTO:               'INTO';
SELECT:             'SELECT';
FROM:               'FROM';
DISTINCT:           'DISTINCT';
ALL:                'ALL';
FORMAT:             'FORMAT';

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
STRING_LITERAL: '\'' ( ~'\'' | '\'\'')* '\'';
BOOLEAN_LITERAL: TRUE | FALSE;

// Comments.

SINGLE_LINE_COMMENT: '--' ~[\r\n]* (('\r'? '\n') | EOF) -> channel(HIDDEN);
MULTILINE_COMMENT: '/*' .*? '*/' -> channel(HIDDEN);
SPACES: [ \u000B\t\r\n] -> channel(HIDDEN);
