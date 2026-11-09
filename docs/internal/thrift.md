# Thrift Plugins System

## How to build

1. Go to `sdk` directory.
2. Execute below:
    ```bash
    thrift --gen netstd:no_deepcopy,net8,pascal,async_postfix -out ./dotnet-sdk/ QueryCat.thrift && \
        mv ./dotnet-sdk/QueryCat/Plugins/Sdk/* ./dotnet-sdk/ && \
        rm -R ./dotnet-sdk/QueryCat && \
        sed -i 's/(LogLevel\.Error)/(Microsoft.Extensions.Logging.LogLevel.Error)/g' ./dotnet-sdk/*.cs
    ```

The last step is required for Thrift 0.24+: the generated code uses `LogLevel.Error` that conflicts with the `LogLevel` enum declared in `QueryCat.thrift`.
