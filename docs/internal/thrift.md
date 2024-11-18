# Thrift Plugins System

## How to build

1. Go to `sdk` directory.
2. Execute below:
    ```bash
    thrift --gen netstd:no_deepcopy,net8,pascal,async_postfix -out ./dotnet-sdk/ QueryCat.thrift && \
        mv ./dotnet-sdk/QueryCat/Plugins/Sdk/* ./dotnet-sdk/ && \
        rm -R ./dotnet-sdk/QueryCat
    ```
