# Simple Plugin

The tutorial explains how to create custom plugin based on `IRowsInput` interface.

1. Create new empty library project.

    ```
    dotnet new classlib --name SimplePlugin
    ```

    **NOTE:** The plugin must contain "Plugin" word in its name.

2. Reference `QueryCat.Backend` project. Right now it is not available in NuGet, you can clone the repository somewhere and reference it. For example:

    ```
    <ItemGroup>
        <ProjectReference Include="..\querycat\src\QueryCat.Backend\QueryCat.Backend.csproj" />
    </ItemGroup>
    ```

3. Add any new input source based on `IRowsInput` interface.

    ```
    public class SamplePluginRowsInput : IRowsInput
    {
        private const long MaxValue = 9;

        private long _currentState;

        /// <inheritdoc />
        public Column[] Columns { get; } =
        {
            new("id", DataType.Integer, "Key.")
        };

        public SamplePluginRowsInput(long initialValue)
        {
            _currentState = initialValue;
        }

        /// <inheritdoc />
        public void Open()
        {
            Trace.WriteLine(nameof(Open));
        }

        /// <inheritdoc />
        public void SetContext(QueryContext queryContext)
        {
            Trace.WriteLine(nameof(SetContext));
        }

        /// <inheritdoc />
        public void Close()
        {
            Trace.WriteLine(nameof(Close));
        }

        /// <inheritdoc />
        public ErrorCode ReadValue(int columnIndex, out VariantValue value)
        {
            Trace.WriteLine(nameof(ReadValue));
            if (columnIndex == 0)
            {
                value = new VariantValue(_currentState);
                return ErrorCode.OK;
            }

            value = default;
            return ErrorCode.InvalidColumnIndex;
        }

        /// <inheritdoc />
        public bool ReadNext()
        {
            Trace.WriteLine(nameof(ReadNext));
            if (_currentState >= MaxValue)
            {
                return false;
            }
            _currentState++;
            return true;
        }

        /// <inheritdoc />
        public void Reset()
        {
            Trace.WriteLine(nameof(Reset));
            _currentState = 0;
        }

        /// <inheritdoc />
        public void Explain(IndentedStringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("Sample");
        }
    }
    ```

4. Add function definition for the new source. For example:

    ```csharp
    [Description("Sample input.")]
    [FunctionSignature("plugin(start: integer = 0): object<IRowsInput>")]
    public static VariantValue SamplePlugin(FunctionCallInfo args)
    {
        var startValue = args.GetAt(0).AsInteger;
        var rowsSource = new SamplePluginRowsInput(startValue);
        return VariantValue.CreateFromObject(rowsSource);
    }
    ```

5. Publish the plugin:

    ```
    dotnet publish ./SimplePlugin/
    ```

6. Run QueryCat with the new plugin.

    ```
    qcat "select * from plugin()" --plugin-dirs /mnt/data/work/SimplePlugin/SimplePlugin/bin/Debug/net7.0/publish/
    ```

    ```
    | id    |
    | ----- |
    | 1     |
    | 2     |
    | 3     |
    | 4     |
    | 5     |
    | 6     |
    | 7     |
    | 8     |
    | 9     |
    ```
