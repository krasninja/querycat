# Autoexec

The `rc.sql` file is automatically execution upon application startup. You can use it to declare common variables and/or execute initialization commands. Example:

```
declare GITHUB_TOKEN := 'token';
declare JIRA_URL := 'https://example.atlassian.net/';
declare _ANSWER_AGENT := ollama_agent('qwen3:8b');
```

The file is found in the following locations:

- Application directory. For example, `/home/User/.local/share/qcat/` on Linux.
- Current working directory.
