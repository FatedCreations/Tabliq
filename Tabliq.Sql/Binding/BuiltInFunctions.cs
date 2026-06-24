//using System.Collections.Generic;
//using Tabliq.Sql.Core;
//using Tabliq.Sql.Diagnostics;
//using Tabliq.Sql.Parsing.Nodes;

using Tabliq.Sql.Ast;

namespace Tabliq.Sql.Binding;

public static class BuiltInFunctions
{
    private static Dictionary<string, FunctionSymbol> functions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CONCAT"] = new FunctionSymbol(
            "CONCAT",
            IsAggregate: false,
            [
                    new FunctionArgumentSymbol("arg1")
            ],
            new FunctionArgumentSymbol("argOthers")),

        ["DATEPART"] = new FunctionSymbol(
            "DATEPART",
            IsAggregate: false,
            [
                new FunctionArgumentSymbol("datepart", BinderHandling: BinderHandling.Skip), // datepart symbol.
                new FunctionArgumentSymbol("date") // date expression
            ]),
        ["EXTRACT"] = new FunctionSymbol("EXTRACT", IsAggregate: false, [
                new FunctionArgumentSymbol("from_expression", RequiredType: typeof(ValueFromExpression)),
            ]),
        ["CAST"] = new FunctionSymbol("CAST", IsAggregate: false, [
                new FunctionArgumentSymbol("as_expression", RequiredType: typeof(AsExpression)),
            ]),
        ["CONVERT"] = new FunctionSymbol("CONVERT", IsAggregate: false, [
                new FunctionArgumentSymbol("datatype", BinderHandling: BinderHandling.Skip), // datepart symbol.
                new FunctionArgumentSymbol("expression"), // date expression
                new FunctionArgumentSymbol("style", Optional: true), // optional style argument
            ]),
        ["AVG"] = new FunctionSymbol("AVG", IsAggregate: true, [
                new FunctionArgumentSymbol("expression"), 
            ]),
        ["COUNT"] = new FunctionSymbol("COUNT", IsAggregate: true, [
                new FunctionArgumentSymbol("expression"), 
            ]),
        ["MIN"] = new FunctionSymbol("MIN", IsAggregate: true, [
                new FunctionArgumentSymbol("expression"),
            ]),
        ["MAX"] = new FunctionSymbol("MAX", IsAggregate: true, [
                new FunctionArgumentSymbol("expression"), 
            ]),
        ["SUM"] = new FunctionSymbol("MAX", IsAggregate: true, [
                new FunctionArgumentSymbol("expression"), 
            ]),
        ["STDEV"] = new FunctionSymbol("STDEV", IsAggregate: true, [
                new FunctionArgumentSymbol("expression"),
            ]),
        ["STDEVP"] = new FunctionSymbol("STDEVP", IsAggregate: true, [
                new FunctionArgumentSymbol("expression"),
            ]),
        ["VAR"] = new FunctionSymbol("VAR", IsAggregate: true, [
                new FunctionArgumentSymbol("expression"),
            ]),
        ["VAR"] = new FunctionSymbol("VARP", IsAggregate: true, [
                new FunctionArgumentSymbol("expression"),
            ])
    };

    public static FunctionSymbol? GetFunction(string name)
    {
        if (functions.TryGetValue(name, out var function))
        {
            return function;
        }

        return null;
    }
}
