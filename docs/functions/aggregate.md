# Aggregate Functions

Aggregate functions are performed on several rows instead of single one.

| Function and Description |
| ---  |
| `avg(value: integer): float` <br /> `avg(value: float): float` <br /> `avg(value: numeric): numeric` <br /><br /> Computes the average (arithmetic mean) of all the non-null input values. |
| `count(value: any): integer` <br /> `count(*): integer` <br /><br /> Computes the number of input not null values. |
| `max(value: integer): integer` <br /> `max(value: float): float` <br /> `max(value: numeric): numeric` <br /><br /> Computes the maximum of the non-null input values. |
| `min(value: integer): integer` <br /> `min(value: float): float` <br /> `min(value: numeric): numeric` <br /><br /> Computes the minimum of the non-null input values. |
| `sum(value: integer): integer` <br /> `sum(value: float): float` <br /> `sum(value: numeric): numeric` <br /><br /> Computes the sum of the non-null input values. |
