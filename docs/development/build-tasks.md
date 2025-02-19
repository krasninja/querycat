# Build Tasks

The project uses [Cake](https://cakebuild.net) cross-platform build automation system.

## Build Tasks

The following tasks will build the platform specific binaries into `./output/` directory.

- `Build-Linux`. Build project for Linux target.
- `Build-Windows`. Build project for Windows target.
- `Build-Mac`. Build project for Mac target.
- `Build-Package`. Build NuGet package.

The following parameters available:

- `PublishAot`. Boolean parameter to turn on/off AOT build. Enabled by default.
- `Properties`. Provides additional key-value pairs for MSBuild.

Examples:

```bash
./build.sh -t Build-Linux -- --PublishAot=false --Properties='Plugin=Assembly'
```

## Maintenance Tasks

- `Build-Grammar`. Generate C# files for ANTLR4 grammar.
- `Clean`. Clean up output, bin and obj directories.
