# Objects Selector

The QueryCat allows to select properties from POCO. You can customize this behavior by implementing `IObjectSelector` interface or overriding `DefaultObjectSelector` class. Here is how you can do that:

```
var thread = new ExecutionThreadBootstrapper()
    .WithObjectsSelector(new MyObjectSelector())
    .Create()
```

The `IObjectSelector` has selector methods that are called on object expression evaluation. Example:

- Expression: `email.Recipients[1].Length`

| Step |  Property/Index     | ObjectSelectorContext Data     | Comment                                               |
| ---- | ------------------- | ------------------------------ | ----------------------------------------------------- |
| 1    | `PROP` `email`      |                                | Not called, it is selected from source or variables.  |
| 2    | `PROP` `Recipients` | `LastValue = Email`            | Find `Recipients` property from `Email` object.       |
| 3    | `IND`  `[1]`        | `LastValue = List<string>`     | Find second recipient string from the list.           |
| 4    | `PROP` `Length`     | `LastValue = "example@ya.su"`  | Find property with name `Length`.                     |

On every step (except 1) you should return the token, which includes.
