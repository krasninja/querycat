# Thrift Connection Design

## Authentication Sequence Diagram

```
    |---------------------|               |---------------------|                   |---------------------|
    | ThriftPluginsLoader |               | ThriftPluginsServer |                   | ThriftPluginsClient |
    |---------------------|               |---------------------|                   |---------------------|
              |                                      |                                      |
 Load ------> |                                      |                                      |
              |                                      |                                      |
              | 1. New ThriftPluginsServer, Start()  |                                      |
              | -----------------------------------> |                                      |
              |                                      |                                      |
              | 2. SetRegistrationToken()            |                                      |
              | -----------------------------------> |                                      |
              |                                      |                                      |
              | 3. Load ThriftPluginsClient exe/lib  |                                      |
              | --------------------------------------------------------------------------> |
              |                                      |                                      |
              | 4. WaitForPluginRegistration()       |                                      |
              | -----------------------------------> |                                      |
              |                                      |                                      |
              |                                      |                                      | 5. (StartServer, create URI)
              |                                      |                                      | ---
              |                                      |                                      |   |
              |                                      |                                      | <--
              |                                      |                                      |
              |                                      | 6. RegisterPluginAsync RPC           |
              |                                      |    (callback URI, plugin functions)  |
              |                                      | <----------------------------------- |
              |                                      |                                      |
              |                                      | 7. Create context, create connection |
              |                                      |     using callback URI               |
              |                                      | -----------------------------------> |
              |                                      |                                      |
              |                                      | 8. Serve RPC                         |
              |                                      | -----------------------------------> |
              |                                      |                                      |
```

1. Create `ThriftPluginsServer` class and start the server. The socket/named pipe will be created.
2. For the plugin generate registration token. Associate registration token with the plugin name (usually file name).
3. Loader will execute plugin proxy to load the NuGet package. The following parameters are passed:
    - server endpoint;
    - registration token;
    - PID of the QueryCat host;
4. The loader will wait for a couple of seconds for plugin registration.
5. Make the connection to call plugin manager (Thrift PluginManager service) methods. Generate endpoint for plugin manager.
6. Call registration and provide for the server plugin functions and endpoint (step 5).
7. Create another connection to the client to call client methods (Thrift Plugin service). Also, ConfirmRegistrationToken() is called to allow next plugin loading. The authentication token is passed back client so it can use it to call PluginsManager methods.
8. The host may ask for additional connection.
