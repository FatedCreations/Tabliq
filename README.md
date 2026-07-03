# Tabliq
A .NET remote query engine, connects and queries against a virtual schema thats securly rewritten such that you can safely run external queries without providing data outside the scope of the virtual schema.

## Reporting issues

We aim to parse and rewrite any ANSI Sql complient syntax, if you find any queries that you believe are not being parsed or rewritten correctly, please report them as issues.

When reporting sql parsing issues, please provide the following information:

The query you are trying to run, the expected output and the actual output. If you can provide a minimal example that reproduces the issue, that would be very helpful.

Also make sure you are providing the Virtual schema too so that data binding and column resolution can be tested and that is a critical feature of this parser.