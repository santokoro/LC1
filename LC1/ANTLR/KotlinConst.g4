grammar KotlinConst;

@lexer::header {
using Antlr4.Runtime;
}

@lexer::members {
    public override void NotifyErrorListeners(string msg) {
        base.NotifyErrorListeners(msg);
    }
}

program
    : declaration* EOF
    ;

declaration
    : CONST VAL IDENT COLON DOUBLE ASSIGN number SEMICOLON
    ;

number
    : MINUS? (INT | REAL)
    ;

CONST      : 'const';
VAL        : 'val';
DOUBLE     : 'Double';
COLON      : ':';
ASSIGN     : '=';
SEMICOLON  : ';';
MINUS      : '-';

IDENT      : [a-zA-Z_] [a-zA-Z0-9_]*;
INT        : [0-9]+;
REAL       : [0-9]+ '.' [0-9]+;

WS         : [ \t\r\n]+ -> skip;

ERROR_CHAR
    : .
      {
          NotifyErrorListeners("Недопустимый символ: " + Text);
      }
    ;
