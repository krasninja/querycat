# Aggregate Functions

Aggregate functions are performed on several rows instead of single one.

| Function and Description |
| --- |
| `avg(value: integer): float` <br /> `avg(value: float): float` <br /> `avg(value: numeric): numeric`<br /><br /> Computes the average (arithmetic mean) of all the non-null input values. |
| `count(value: any): integer` <br /> `count(*): integer`<br /><br /> Computes the number of input not null values. |
| `first_value(value: any): any` <br /> Returns value evaluated at the row that is the first row of the window frame. |
| `last_value(value: any): any` <br /> Returns value evaluated at the row that is the last row of the window frame. |
| `max(value: integer): integer` <br /> `max(value: float): float` <br /> `max(value: numeric): numeric`<br /><br /> Computes the maximum of the non-null input values. |
| `min(value: integer): integer` <br /> `min(value: float): float` <br /> `min(value: numeric): numeric`<br /><br /> Computes the minimum of the non-null input values. |
| `row_number(): integer` <br /><br /> Returns the number of the current row within its partition, counting from 1. |
| `string_agg(target: string, delimiter: string): string`<br /><br /> Concatenates the non-null input values into a string. |
| `sum(value: integer): integer` <br /> `sum(value: float): float` <br /> `sum(value: numeric): numeric`<br /><br /> Computes the sum of the non-null input values. |
