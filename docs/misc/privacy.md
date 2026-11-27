# Privacy Policy

Effective date: November 20, 2026

This policy describes how the QueryCat project ("QueryCat", "we") handles data in
relation to the `qcat` command line tool, the QueryCat .NET libraries and the
project websites. QueryCat is an open-source project distributed under its
[license](https://github.com/krasninja/querycat/blob/develop/LICENSE.txt).

## Summary

**QueryCat does not collect, transmit or sell your personal data.** The tool runs
entirely on your machine, processes the files and streams you point it at locally,
and contains no telemetry, analytics, advertising or tracking code. Network
connections happen only when you explicitly ask for them (see below).

## Data processed by the tool

QueryCat reads the input sources you specify (files, stdin, command output, URLs)
and writes results to the outputs you specify (stdout, files, etc.). All query
processing happens locally in your environment. The QueryCat developers never
receive your queries, your input data or your results.

## Data stored locally

QueryCat stores its own state on your machine only:

- **Configuration** (for example, the selected AI agent) in the `config.json` file
  of a `qcat` subdirectory of your user configuration directory (`~/.config/qcat` on Linux,
  `%APPDATA%\qcat` on Windows).
- **Local application data** such as downloaded plugins and caches in a `qcat`
  subdirectory of your local application-data directory (for example,
  `~/.local/share/qcat` on Linux, `%LOCALAPPDATA%\qcat` on Windows).
- **Plugin authentication tokens**, if you use plugins that require authentication,
  in the `auth.json` file of the local application data directory (readable only by
  the current user).

Configuration may contain credentials you provide (for example, an AI provider API
key saved via `qcat agent set`). These values are stored in plain files under your
user profile and are never sent to the QueryCat developers. Protecting these files
is the responsibility of the user or system administrator. You can delete this
data at any time by removing the directories above.

## When QueryCat makes network requests

QueryCat contacts the network only as a direct result of your actions:

- **Installing, updating or listing plugins.** By default, the plugins list and
  plugin packages are downloaded from the QueryCat plugins repository hosted on
  Yandex Cloud Object Storage (`querycat.storage.yandexcloud.net`). Applications
  that embed the QueryCat libraries can point to another repository.
- **Downloading the plugin proxy executable** from the
  [GitHub releases](https://github.com/krasninja/querycat/releases) of the project,
  when a plugin requires it.
- **Reading remote sources**, when you query a URL or use functions that perform
  HTTP requests or OAuth authentication.
- **AI features** (`ai_input`), when you configure an AI agent. The following is
  sent to the provider you selected: your question; the schema of the referenced
  sources (table and column names, types and descriptions); the list of available
  functions; your answers to clarifying questions; and, when a generated query
  fails, the error message so the model can fix the query. Error messages may
  include fragments of your data (for example, a value that could not be
  converted). Cloud providers such as OpenAI or GigaChat process this data under their own
  privacy policies; a local provider such as Ollama keeps it on your machine.
  QueryCat does not choose a provider for you and sends nothing to AI services
  unless you configure one.
- **MCP server resources**, when you expose an `http(s)://` URL as a resource
  with `qcat mcp --resource`; the page is fetched when an MCP client reads it.

## Web server and MCP server

`qcat serve` (web UI / REST API) and the MCP server run on your machine and are
operated by you. QueryCat does not receive any of the traffic they handle. The
built-in web server supports only basic authentication and no TLS; as noted in the
[web server documentation](../features/web-server.md), do not expose it directly to
untrusted networks - put a properly configured reverse proxy in front of it.
Securing the data you serve is your responsibility.

## Third-party plugins

Plugins are separate programs, often developed by third parties, that run with
your permissions. This policy does not cover them. A plugin may access files,
credentials or the network according to its own behavior and privacy policy.
Install plugins only from sources you trust.

## Project websites and distribution channels

- **Downloads and source code** are hosted on [GitHub](https://github.com/krasninja/querycat)
  and [NuGet](https://www.nuget.org/packages/QueryCat/). These services may collect
  usage data (such as download counts and IP addresses) under their own privacy
  policies.
- **Plugins** are hosted on Yandex Cloud Object Storage. As with any web server,
  requests to it (including your IP address and the requested plugin packages) may
  be recorded in access logs by the storage provider under its own privacy policy.
- **Documentation** is hosted on [Read the Docs](https://querycat.readthedocs.io/),
  which has its own privacy policy and may use analytics/cookies.
- **The online [playground](https://querycat.anti-soft.ru/playground/)** is a hosted
  web service: queries and data you enter there are sent to and processed on that
  server, not by your local installation, and may appear in its server logs. Do not
  enter sensitive data into the playground.

The QueryCat project does not run analytics or advertising on these pages and does
not use them to collect personal data beyond what the hosting platforms record
(for example, download statistics and access logs).

## Children's privacy

QueryCat is a developer tool and is not directed at children. We do not knowingly
collect any personal information from anyone, including children.

## Changes to this policy

This policy may be updated as the project evolves. The current version is always
available in the
[repository](https://github.com/krasninja/querycat/blob/develop/docs/misc/privacy.md)
and in the documentation; the effective date above reflects the latest revision.

## Contact

Questions about this policy can be raised via
[GitHub issues](https://github.com/krasninja/querycat/issues) or
[discussions](https://github.com/krasninja/querycat/discussions) in the project
repository.
