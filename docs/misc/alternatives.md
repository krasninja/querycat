# Alternatives

There are alternative and similar tools:

- [Steampipe](https://steampipe.io/). The powerful tool that can query data using SQL from cloud providers (AWS, Azure). There are a lot of plugguble data providers. Internally, it uses Postgres FDW to query various data sources.

- [CloudQuery](https://www.cloudquery.io/). The open-source cloud asset inventory powered by SQL.

- [osquery](https://osquery.io/). Uses SQL to provide various device info: disks, processes, apps, users and lot more. Written in C++.

- [LNAV](https://github.com/tstack/lnav). The Logfile Navigator. It is a terminal application that can understand your log files and make it easy for you to find problems with little to no setup.

- [csvq](https://mithrandie.github.io/csvq/). Can query CSV files. It can run SELECT, INSERT, UPDATE, DELETE queries. Written in Go.

- [csvkit](https://github.com/wireservice/csvkit/). This is a suite of command-line tools for converting to and working with CSV, the king of tabular file formats. Written in Python.

- [Musoq](https://github.com/Puchaczov/Musoq). Very similar tool written in C#. The tool allows to use SQL on different data source (CSV, JSON, XML, Git, Websites and a lot more!).

- [RBQL](https://rbql.org). RBQL is a technology that provides SQL-like language for data-transformation and data-analysis queries for structured data (e.g. CSV files, log files, Python lists, JS arrays). RBQL evaluates input query using one of the available general-purpose "backend" languages (currently Python or JavaScript).

- [NCalc](https://github.com/ncalc/ncalc). NCalc is a mathematical expression evaluator in .NET. NCalc can parse any expression and evaluate the result, including static or dynamic parameters and custom functions.

- [NFun](https://github.com/tmteam/NFun). This is an expression evaluator or a mini-script language for .NET . It supports working with mathematical expressions as well as with collections, strings, hi-order functions and structures. NFun is quite similar to NCalc but with a rich type system and linq support.

- [MathEvaluator](https://github.com/AntonovAnton/math.evaluation). MathEvaluator is a .NET library that allows you to evaluate any mathematical expressions from a string dynamically.

- [DuckDB](https://duckdb.org). The DuckDB has extensible engine and can select structured data from CSV or log files. [The Data Engineer's Guide to Efficient Log Parsing with DuckDB/MotherDuck](https://motherduck.com/blog/json-log-analysis-duckdb-motherduck/). Has great performance.

- [zsv](https://github.com/liquidaty/zsv). Fast CSV parser.

- [Sep](https://github.com/nietras/Sep). CSV parser NuGet package. Really fast.

No active support:

- [Log Parser](https://www.microsoft.com/en-us/download/details.aspx?id=24659). The is the command line tool to query various logs files (CSV, IIS logs, event logs, XML, etc). It can run aggregate and filter queries. Only for Windows. Not supported anymore. [Documentation](https://documentation.help/Log-Parser/index.htm).

- [logdissect](https://github.com/dogoncouch/logdissect/). Logdissect is a CLI utility and Python library for analyzing log files and other data. It can parse, merge, filter, and export data (to log files, or JSON). Last commit more than 3 years ago.

- [xsv](https://github.com/BurntSushi/xsv/). Command line programs for indexing, slicing, analyzing, splitting and joining CSV files. Written in Rust. Seems it is not supported anymore.

- [q](https://harelba.github.io/q/). Run SQL directly on CSV or TSV files. Written in Python. Seems no active support.

- [TextQL](https://github.com/dinedal/textql). Executes SQL against CSV or TSV. Golang.

- [dsq](https://github.com/multiprocessio/dsq). Command line tool for running SQL queries against JSON, CSV, Excel, Parquet, and more. Not under active development: "While development may continue in the future with a different architecture, for the moment you should probably instead use DuckDB, ClickHouse-local, or GlareDB (based on DataFusion)."
https://github.com/cube2222/octosql

- [OctoSQL](https://github.com/cube2222/octosql). OctoSQL is a query tool that allows you to join, analyse and transform data from multiple databases and file formats using SQL.
