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

There are two ways to setup the AI answer agent.

1. Define the special `_ANSWER_AGENT`. Examples:

    ```
    declare _ANSWER_AGENT := ollama_agent('qwen3:8b');
    declare _ANSWER_AGENT := openai_agent('API_KEY');
    declare _ANSWER_AGENT := gigachat_agent('API_KEY');
    ```

2. Provide thru the sources:

    ```
    ai_input('select all actors', 'actors=/home/ivan/temp/MoviesActors.csv', ollama_agent('qwen3:8b'));
    ```

    As you can see we used Ollama agent and provided it as source. The `ai_input` functions understands this and use it to format SQL.

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

***Select from two sources**

```
ai_input('select names that we have both in actors ans hackers', 'actors=/home/ivan/temp/MoviesActors.csv', 'hackers=/home/ivan/temp/hackers.csv')
-- SQL: SELECT hackers.Name FROM hackers INNER JOIN actors ON hackers.Name = actors.name;
```
