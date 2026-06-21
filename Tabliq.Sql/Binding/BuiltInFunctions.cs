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
        ["CONCAT"] = new FunctionSymbol("CONCAT", [new FunctionArgumentSymbol("arg1")], new FunctionArgumentSymbol("argOthers")),
        ["DATEPART"] = new FunctionSymbol("DATEPART", [
                new FunctionArgumentSymbol("datepart", BinderHandling: BinderHandling.Skip), // datepart symbol.
                new FunctionArgumentSymbol("date") // date expression
            ]),
        ["EXTRACT"] = new FunctionSymbol("EXTRACT", [
                new FunctionArgumentSymbol("from_expression", RequiredType: typeof(ValueFromExpression)),
            ]),
        ["CAST"] = new FunctionSymbol("CAST", [
                new FunctionArgumentSymbol("as_expression", RequiredType: typeof(AsExpression)),
            ]),
        ["CONVERT"] = new FunctionSymbol("CONVERT", [
                new FunctionArgumentSymbol("datatype", BinderHandling: BinderHandling.Skip), // datepart symbol.
                new FunctionArgumentSymbol("expression"), // date expression
                new FunctionArgumentSymbol("style", Optional: true), // optional style argument
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
