# Frequently Asked Questions

## The type detection resolves incorrect column type for my data.

The QueryCat prefetches first 10 rows to understand what columns data has and what type. However, it might be wrong in some cases. For example:

```csv
OrderId,Comment
0000512,"I'll hack the auxiliary SMS card, that should card the SMS card!"
1000451,"If we synthesize the pixel, we can get to the HDD pixel through the multi-byte HDD pixel!"
...
T000004,"I'll back up the back-end COM panel, that should panel the COM panel!"
```

According to the first rows we can consider the `OrderId` column has integer type. However, it might contain letters. So following query will produce error:

```sql
select * from 'orders.csv';
```

To force QueryCat override column type apply `cast` operator to that column:

```sql
select OrderId::string, Comment from 'orders.csv';
```
