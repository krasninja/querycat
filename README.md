# QueryCat

QueryCat is the command line tool that provides the universal access to a text-based data like CSV, JSON, XML and log files. It also provides the way to query this data using SQL (Structured Query Language). The general idea is to bring well-known SQL expressive power and provide easy access to various kind of information.

- QueryCat is designed to use pipeline mode to be able to process large amount of data.
- INTO clause allows to write transformed data to popular formats.
- The single binary file doesn't require any dependencies to be installed and simplifies installation (download and use).

## Features

1. Can query CSV, TSV, JSON, XML, log files and other input sources.
2. Has simple SQL-engine that supports select, filter, [group](https://querycat.readthedocs.io/en/latest/functions/aggregate/), joins, order, limit, offset, union, subqueries, CTE (recursive CTE), window functions.
3. Primitive [Web UI](https://querycat.readthedocs.io/en/latest/features/web-server/) and [REST API](https://querycat.readthedocs.io/en/latest/features/web-server/) are available.
4. Can be extended using [plugins](https://querycat.readthedocs.io/en/latest/plugins/).
5. [Variables](https://querycat.readthedocs.io/en/latest/commands/declare/) support.

## Information

- Home: [GitHub](https://github.com/krasninja/querycat)
- Tutorial: [ReadTheDocs](https://querycat.readthedocs.io/en/latest/tutorial/)
- Documentation: [ReadTheDocs](https://querycat.readthedocs.io/)

## Limitations

- Not whole SQL standard is implemented. It is huge.
- No data modification (UPDATE, INSERT, UPSERT) statements are implemented. But you can use INTO clause in some cases.
