# QueryCat

QueryCat is the command line tool that provides the universal access to a text-based data like CSV, log files. It also provides the way to query this data using SQL (Structured Query Language). The general idea is to bring well-known SQL expressive power and provide easy access to various kind of information.

- QueryCat is designed to use pipeline mode to be able to process large amount of data.
- INTO clause allows to write transformed data to popular formats.
- The single binary file doesn't require any dependencies to be installed and simplifies installation (download and use).

## Information

- Home: [GitHub](https://github.com/krasninja/querycat)
- Tutorial: [ReadTheDocs](https://querycat.readthedocs.io/en/latest/tutorial/)
- Documentation: [ReadTheDocs](https://querycat.readthedocs.io/)

## Limitations

- Not whole SQL standard is implemented.
- No data modification (UPDATE, INSERT, UPSERT) statements are implemented.
