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
| 4    | `PROP` `Length`     | `LastValue = 'example@ya.su'`  | Find property with name `Length`.                     |

On every step (except 1) you should return the token, which includes next object and optional property info. If null is returned - stop iteration and return NULL as result.

## Filter Expressions

You can define filter expressions on lists and dictionaries using JSON Path-like syntax: `users[?@.Name = 'Lena'][0].Phone`. Implementation details are below.

```
AST

1-IdentifierExpressionNode (users)
|
\- 2-IdentifierFilterSelectorNode in SelectorNode[]
   |
   \- BinaryOperationExpressionNode
      |
      |- LiteralNode ('Lena')
      |- Operation (Equals)
      \- 3-IdentifierExpressionNode (@.Name)
```

1. Into node 1 we put a special `VariantValueContainer` ("object_selector_container_key" key). The node's delegate should return the current value of the expression.
2. Into node 3 we put the delegate to get container of the parent node 1 and fetch its value. We call `GetObjectBySelector` by using container value as start object.
3. Into node 1 we put its object selector context `ObjectSelectorContext` ("object_selector_key" key).
4. In delegate of node 2 we do the following steps:
    - Get the container of node 1 (ref step 1).
    - Get the current list of node 1 (ref step 3).
    - Iterate thru the list and set new container value on each step. That way, after calling the `BinaryOperationExpressionNode` delegate we can get the current iterator value and evaluate the filter.
