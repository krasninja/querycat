# AI

Uses AI to convert question into SQL and run the query.

## Syntax

```
ai_input(question: string, ...source?: any[]): object<IRowsInput>
```

Parameters:

- `question`. The user question. For example, "select all users who have 18 age or greater".
- `source`. List of sources or AI agent.

## Setup

Right now, there are no embedded AI agents in the QueryCat. You can use the one provided by the following plugins:

- `QueryCat.Plugins.OpenAI`. The LLM developed by OpenAI company. https://openai.com.
- `QueryCat.Plugins.GigaChat`. The LLM developed by Sber Bank Russian company. https://giga.chat.
- `QueryCat.Plugins.Ollama`. The framework for building and running language models on the local machine. https://ollama.com.

There are three ways to set up the AI answer agent.

1. (Recommended) Use the `agent` CLI commands. The `qcat agent list` command shows all functions that create an answer agent
(they return `object<IAnswerAgent>`), `qcat agent set` saves the agent function call in the config file
(the `__ai_agent` key) and `qcat agent get` prints the saved function call. The `set` command executes
the function call to verify it returns an `IAnswerAgent` object before saving:

    ```
    $ qcat agent list
    | name         | signature                                                                                    | description                      |
    | ------------ | -------------------------------------------------------------------------------------------- | -------------------------------- |
    | OLLAMA_AGENT | OLLAMA_AGENT(MODEL: String, URL: String := 'http://localhost:11434'): Object<IAnswerAgent>   | Create instance of Ollama agent. |
    | OPENAI_AGENT | OPENAI_AGENT(AUTH_KEY: String, MODEL: String := 'gpt-4o-mini'): Object<IAnswerAgent>         | Create instance of OpenAI agent. |
    Current AI agent: ollama_agent('qwen3:8b')

    $ qcat agent set "ollama_agent('qwen3:8b')"
    AI agent is set to 'ollama_agent('qwen3:8b')'.

    $ qcat agent get
    ollama_agent('qwen3:8b')
    ```

2. Define the special `_ANSWER_AGENT`. It has priority over the config value. Examples:

    ```
    declare _ANSWER_AGENT := ollama_agent('qwen3:8b');
    declare _ANSWER_AGENT := openai_agent('API_KEY');
    declare _ANSWER_AGENT := gigachat_agent('API_KEY');
    ```

    You can set this up in autoexec `rc.sql` file (see Autoexec section for more details).

3. Provide through the sources:

    ```
    ai_input('select all actors', 'actors=/home/ivan/temp/MoviesActors.csv', ollama_agent('qwen3:8b'));
    ```

    As you can see we used Ollama agent and provided it as source. The `ai_input` function understands this and use it to format SQL.

## Examples

**Select all movies from one source**

```
ai_input('Select all movies', 'actors=/home/ivan/temp/MoviesActors.csv')
-- SQL: SELECT * FROM actors;
```

```
ai_input('Select all movies', '/home/ivan/temp/MoviesActors.csv')
-- SQL: SELECT * FROM input0;
```

**Select from two sources**

```
ai_input('select names that we have both in actors ans hackers', 'actors=/home/ivan/temp/MoviesActors.csv', 'hackers=/home/ivan/temp/hackers.csv')
-- SQL: SELECT hackers.Name FROM hackers INNER JOIN actors ON hackers.Name = actors.name;
```
