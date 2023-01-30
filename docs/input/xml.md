# XML

The XML input format parses XML text files.

XML files (also called "XML documents") are hierarchies of nodes. Nodes can include other nodes, and each node can have a node value and a set of attributes. For example, the following XML node has a value (in this instance, "Rome"), and a single attribute ("Population", whose value is, in this example, "3350000"):

```xml
<CITY Population='3350000'>Rome</CITY>
```

The XML input processes nodes attributes and nodes text.

## Syntax

```
xml(): object<IRowsFormatter>
```

## Examples

**Select from XML file**

Source file:

```xml
<?xml version="1.0" ?>
<Users>
  <User Id="1">John</User>
  <User Id="3">Ali</User>
</Users>
```

Query:

```
select * from 'xml_file.xml';
```

Result:

```
| Id    | User       |
| ----- | ---------- |
| 1     | John       |
| 3     | Ali        |
```
