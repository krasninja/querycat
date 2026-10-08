# .NET Assembly

Making plugin as a .NET assembly is the convenient way to build a plugin.

- [+] Since all calls are made within the same process, the data processing is very efficient.
- [-] It is not supported with the QueryCat CLI. You should include QueryCat as a package in your .NET project.
- [-] Only .NET specific language can be used.

To prepare a plugin follow the steps:

1. Create a new empty library project.

    ```
    dotnet new classlib --name SimplePlugin
    ```

    **NOTE:** The plugin must contain "Plugin" (case insensitive) word in its name.

2. Reference `QueryCat.Plugins.Client` package: [NuGet](https://www.nuget.org/packages/QueryCat.Plugins.Client). For example:

    ```
    <ItemGroup>
        <PackageReference Include="QueryCat.Plugins.Client" Version="0.11.0" />
    </ItemGroup>
    ```

3. Implement functions and rows inputs. They will be discovered in the assembly using reflection.

4. Publish the assembly. It can be published as `.dll` or `.nupkg` file.
