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

    **NOTE:** The plugin must contain "Plugin" (case-insensitive) word in its name.

2. Reference `QueryCat` package: [NuGet](https://www.nuget.org/packages/QueryCat). For example here is the proposed `.csproj` structure:

    ```xml
    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <PackageVersion>0.1.0</PackageVersion>
        <Title>My Plugin</Title>
        <Authors>Author Name</Authors>
        <Description>Plugin description.</Description>
        <PackageTags>myplugin querycat</PackageTags>
        <AssemblyName Condition=" '$(UseAssemblyName)' == 'true' ">qcat-plugins-my-$(PackageVersion)-$(Runtime)</AssemblyName>
      </PropertyGroup>
      <ItemGroup>
        <PackageReference Include="QueryCat" Version="0.19.0" />
      </ItemGroup>
    </Project>
    ```

3. Every plugin MUST have a `Registration` static class. The plugin loader discovers it via reflection.

    ```csharp
    using QueryCat.Backend.Core.Functions;

    namespace QueryCat.Plugins.MyPlugin;

    public static class Registration
    {
        public static void RegisterFunctions(IFunctionsManager functionsManager)
        {
            functionsManager.RegisterFunction(MyFunction.MyFunctionDelegate);
            functionsManager.RegisterFunction(MyRowsInput.MyRowsInputFunction);
        }

        // Optional. Called after plugin is loaded.
        public static Task OnLoadAsync(IExecutionThread executionThread, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
    ```

4. Implement functions and rows inputs. They will be discovered in the assembly using reflection.

4. Publish the assembly. It can be published as `.dll` or `.nupkg` file.

## Plugin Project Structure

```
QueryCat.Plugins.<Name>/
├── QueryCat.Plugins.<Name>.csproj   # Project file
├── Registration.cs                   # Entry point — registers all functions
├── Functions/                        # Custom functions (optional)
│   └── MyFunction.cs
├── Inputs/                           # Rows input sources (optional)
│   └── MyRowsInput.cs
├── Models/                           # Domain models (optional)
│   └── MyModel.cs
└── Docs/                             # Plugin documentation (optional)
```
