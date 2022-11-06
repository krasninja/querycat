# Storage

The submodule contains everything related to methods of getting rows from various sources. The main interfaces are `IRowsInput` and `IRowsOutput`.

## Query Context

Both `IRowsInput` and `IRowsOutput` has `SetContext` method. It sets the current query context and has information about execution details. In general, this is the way to communicate between application and specific rows input/output. There are examples when query context might be useful:

1. Optimize rows input reading. For example, there is GitHub commits endpoint. A user executes the query `SELECT sha FROM github_commits('test/repo') WHERE author_name = 'test@example.com'`. The rows input can get `WHERE` conditions from the query context and make the request only for the specified author. This way, less data will be fetched over the network and query will be executed much more efficiently.
2. Cache control. The rows input can set up the current cache key to be used for subqueries. Instead of executing the same query twice, the further requests would fetch data from memory.
3. Common key-value storage. For example, there are `crm_tasks` and `crm_users` rows input. They share the same authentication information. If these two rows inputs are used within the same query, the authentication query will be executed once.
