# Alternatives

There are alternative and similar tools:

- [Steampipe](https://steampipe.io/). The powerful tool that can query data using SQL from cloud providers (AWS, Azure). There are a lot of plugguble data providers. Internally, it uses Postgres FDW to query various data sources.

- [CloudQuery](https://www.cloudquery.io/). The open-source cloud asset inventory powered by SQL.

- [osquery](https://osquery.io/). Uses SQL to provide various device info: disks, processes, apps, users and lot more. Written in C++.

- [csvq](https://mithrandie.github.io/csvq/). Can query CSV files. It can run SELECT, INSERT, UPDATE, DELETE queries. Written in Go.

- [csvkit](https://github.com/wireservice/csvkit/). This is a suite of command-line tools for converting to and working with CSV, the king of tabular file formats. Written in Python.

- [Musoq](https://github.com/Puchaczov/Musoq) Very similar tool written in C#. The tool allows to use SQL on different data source (CSV, JSON, XML, Git, Websites and a lot more!).

No active support:

- [Log Parser](https://www.microsoft.com/en-us/download/details.aspx?id=24659). The is the command line tool to query various logs files (CSV, IIS logs, event logs, XML, etc). It can run aggregate and filter queries. Only for Windows. Not supported anymore. [Documentation](https://documentation.help/Log-Parser/index.htm).

- [logdissect](https://github.com/dogoncouch/logdissect/). Logdissect is a CLI utility and Python library for analyzing log files and other data. It can parse, merge, filter, and export data (to log files, or JSON). Last commit more than 3 years ago.

- [xsv](https://github.com/BurntSushi/xsv/). Command line programs for indexing, slicing, analyzing, splitting and joining CSV files. Written in Rust. Seems it is not supported anymore.

- [q](https://harelba.github.io/q/). Run SQL directly on CSV or TSV files. Written in Python. Seems no active support.
