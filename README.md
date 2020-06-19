# Ptixed.Sql

This library can be used for accessing MSSQL databases (but it should be easy enough to write other adapters). It builds on idea by Jon Skeet from here https://www.youtube.com/watch?v=JIlO_EebEQI&t=44m40s so that you can execute queries like so:

```c#
var result = Database.Query($"SELECT * FROM Table WHERE id > {x}");
```

Library will translate the expression to something like:

```sql
SELECT * FROM Table WHERE id > @0
```

with `x` neatly packed into `SqlParameter`. Essentially this allows you to write normal SQL queries without worrying about SQLi. Untility functions like Insert, Update, Delete, GetById are also available. For more sample usage check out [unit tests](https://github.com/ptixed/Ptixed.Sql/blob/master/Ptixed.Sql.Tests/QueryTests.cs)

Library uses DynamicMethod api for emiting code during runtime for quickly accessing members while mapping objects between memory and db.
