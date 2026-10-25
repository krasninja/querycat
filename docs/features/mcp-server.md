# MCP Server

QueryCat can run as a [Model Context Protocol](https://modelcontextprotocol.io) (MCP) server, so AI agents can search and call QueryCat functions (including functions from loaded plugins) and run SQL queries.

```
$ qcat mcp
```

Options:

- `--transport`. `http` (default) or `stdio`.
- `--url`. HTTP endpoint to listen on. Default is `http://localhost:6799/mcp/`.
- `--allow-origin`. Additional allowed value of the HTTP `Origin` header. Loopback origins are always allowed, `*` allows any origin.
- `--unsafe`. Allow unsafe functions and modification commands (`INSERT`, `UPDATE`, `DELETE`). By default, the server runs in safe mode and only read-only functions can be called.

**Note: The HTTP transport has no authentication. Do not run it on an external interface (that is accessible over internal or external network). Prefer the stdio transport when the client supports it.**

## Tools

- `search_functions`. Search functions by a part of the name or description.
- `get_function_info`. Get all function overloads with arguments and return type. For functions that return `object<IRowsInput>` or `object<IRowsIterator>` it also returns the columns' schema if `args` are provided (only safe functions are called).
- `call_function`. Call the function with named (object) or positional (array) arguments.
- `execute_query`. Run SQL query and return the result rows. At most 1000 rows are returned; the `truncated` flag is set if there are more rows.

## Client Configuration

Example of the stdio server configuration for an MCP client:

```json
{
  "mcpServers": {
    "querycat": {
      "command": "qcat",
      "args": ["mcp", "--transport", "stdio"]
    }
  }
}
```

Example of the HTTP server configuration:

```json
{
  "mcpServers": {
    "querycat": {
      "type": "http",
      "url": "http://localhost:6799/mcp/"
    }
  }
}
```
