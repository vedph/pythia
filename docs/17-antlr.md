# ANTLR

Search queries are built by an ANTLR language defined in `pythia.g4` grammar. VSCode has a nice extension for ANTLR syntax support: `ANTLR4 grammar syntax support`.

You can also test online by pasting the Pythia g4 grammar into `Parser` and clearing `Lexer` in the [ANTLR Online Lab](http://lab.antlr.org).

Useful links:

- <https://github.com/antlr/antlr4/tree/master/runtime/CSharp>
- <https://github.com/tunnelvisionlabs/antlr4cs>
- <https://www.antlr3.org/works/> for AntlrWorks GUI
- <https://tomassetti.me/getting-started-with-antlr-in-csharp/>
- <https://levlaz.org/setting-up-antlr4-on-windows/>

## Setup

Setup in Windows is a bit more involved:

1. install Java runtime environment (v8 or higher).

2. download the ANTLR *complete* JAR from the ANTLR org website (<https://www.antlr.org/download.html>). The ANTLR tool converts grammars into programs that recognize sentences in the language described by the grammar. Place the JAR into some folder, e.g. `C:\Javalib` (or whatever else you like).

3. add the `antlr-...complete.jar` to `CLASSPATH`, either permanently (environment variables: create or append to CLASSPATH variable = `.;C:\Javalib\antlr-4.9.2-complete.jar`; note the trailing dot) or temporarily, at command line: `SET CLASSPATH=.;C:\Javalib\antlr-4.10.1-complete.jar;%CLASSPATH%`.

4. you can use these batches:

- compile: `antlr.bat`:

```bat
java org.antlr.v4.Tool %*
```

- test: `grun.bat`:

```bat
java org.antlr.v4.gui.TestRig %*
```

This batch starts from the grammar (`.g4`) file to build Java classes and compile, build C# classes, and run test on some expression stored in a file:

```bat
call C:\Javalib\antlr pythia.g4 -visitor
call C:\Javalib\antlr pythia.g4 -visitor -Dlanguage=CSharp -o .\cs\
javac *.java
call C:\Javalib\grun pythia query -gui .\grunsample.txt
```

To interactively invoke a test, run `grun.bat` (e.g. `.\grun.bat pythia query -gui`), then type and close the input stream by pressing CTRL+Z.
