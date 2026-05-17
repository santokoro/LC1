grammar KotlinConst;

program
    : declaration* EOF
    ;

declaration
    : CONST VAL IDENT COLON DOUBLE ASSIGN expr SEMICOLON
    ;

expr
    : expr op=(PLUS | MINUS) term   # addSub
    | term                          # exprTerm
    ;

term
    : term op=(STAR | DIV) factor   # mulDiv
    | factor                        # termFactor
    ;

factor
    : LPAREN expr RPAREN            # paren
    | MINUS factor                  # unaryMinus
    | number                        # factorNumber
    ;

number
    : INT
    | REAL
    ;

CONST      : 'const';
VAL        : 'val';
DOUBLE     : 'Double';
COLON      : ':';
ASSIGN     : '=';
SEMICOLON  : ';';
PLUS       : '+';
MINUS      : '-';
STAR       : '*';
DIV        : '/';
LPAREN     : '(';
RPAREN     : ')';

IDENT      : [a-zA-Z_] [a-zA-Z0-9_]*;
INT        : [0-9]+;
REAL       : [0-9]+ '.' [0-9]+;

WS         : [ \t\r\n]+ -> skip;

ERROR_CHAR : . ;
