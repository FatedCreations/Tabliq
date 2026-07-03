---
name: SQL Parser
about: Issues with parsing/binding sql queries against a schema
title: ''
labels: ''
assignees: ''

---

SQL Query:

```sql
    SELECT * FROM foo
```

Schema

```json
[
    {
        "tableName": "foo",
        "columns": [
            { "name": "col1" },
            { "name": "col2" },
        ]
    }
]
```
