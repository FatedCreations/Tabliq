//using System.Collections.Generic;
//using Tabliq.Sql.Core;
//using Tabliq.Sql.Diagnostics;
//using Tabliq.Sql.Parsing.Nodes;

using Tabliq.Sql.Ast;

namespace Tabliq.Sql.Binding;

public static class BuiltInFunctions
{
    // https://en.wikibooks.org/wiki/SQL_Dialects_Reference
    private static Dictionary<string, FunctionSymbol> functions =
        new List<FunctionSymbol>()
        {
            // https://en.wikibooks.org/wiki/SQL_Dialects_Reference/Functions_and_expressions/Misc_expressions
            new FunctionSymbol("CAST", IsAggregate: false, [
                new FunctionArgumentSymbol("as_expression", RequiredType: typeof(AsExpression)),
            ]),
            new FunctionSymbol("COALESCE", IsAggregate: false, [
                new FunctionArgumentSymbol("val")
            ], ParamsArgument: new FunctionArgumentSymbol("fallbacks")),
            new FunctionSymbol("NULLIF", IsAggregate: false, [
                new FunctionArgumentSymbol("a"),
                new FunctionArgumentSymbol("b"),
            ]),

            // https://en.wikibooks.org/wiki/SQL_Dialects_Reference/Functions_and_expressions/Date_and_time_functions
            new FunctionSymbol("EXTRACT", IsAggregate: false, [
                new FunctionArgumentSymbol("from_expression", RequiredType: typeof(ValueFromExpression)),
            ]),

            // https://en.wikibooks.org/wiki/SQL_Dialects_Reference/Functions_and_expressions/Math_functions/Numeric_functions
            new FunctionSymbol("ABS", IsAggregate: false, [
                new FunctionArgumentSymbol("x"),
            ]),
            new FunctionSymbol("MOD", IsAggregate: false, [ // x % y in sql
                new FunctionArgumentSymbol("x"),
                new FunctionArgumentSymbol("y"),
            ]),
            new FunctionSymbol("CEILING", IsAggregate: false, [
                new FunctionArgumentSymbol("x"),
            ]),
            new FunctionSymbol("CEIL", IsAggregate: false, [ // CEILING ins mssql
                new FunctionArgumentSymbol("x"),
            ]),
            new FunctionSymbol("FLOOR", IsAggregate: false, [
                new FunctionArgumentSymbol("x"),
            ]),
            new FunctionSymbol("SQRT", IsAggregate: false, [
                new FunctionArgumentSymbol("x"),
            ]),
            new FunctionSymbol("EXP", IsAggregate: false, [
                new FunctionArgumentSymbol("x"),
            ]),
            new FunctionSymbol("POWER", IsAggregate: false, [
                new FunctionArgumentSymbol("x"),
                new FunctionArgumentSymbol("y"),
            ]),
            new FunctionSymbol("LN", IsAggregate: false, [ // log in mssql
                new FunctionArgumentSymbol("x"),
            ]),

            // https://en.wikibooks.org/wiki/SQL_Dialects_Reference/Functions_and_expressions/Math_functions/Aggregate_functions
            new FunctionSymbol("COUNT", IsAggregate: true, [
                new FunctionArgumentSymbol("x"),
            ]),
            new FunctionSymbol("SUM", IsAggregate: true, [
                new FunctionArgumentSymbol("x"),
            ]),
            new FunctionSymbol("AVG", IsAggregate: true, [
                new FunctionArgumentSymbol("x"),
            ]),
            new FunctionSymbol("MIN", IsAggregate: true, [
                new FunctionArgumentSymbol("x"),
            ]),
            new FunctionSymbol("MAX", IsAggregate: true, [
                new FunctionArgumentSymbol("x"),
            ]),
            new FunctionSymbol("STDDEV_POP", IsAggregate: true, [ // STDEV in mssql
                new FunctionArgumentSymbol("expression"),
            ]),
            new FunctionSymbol("STDDEV_SAMP", IsAggregate: true, [ // STDEVP in mssql
                new FunctionArgumentSymbol("expression"),
            ]),
            new FunctionSymbol("VAR_POP", IsAggregate: true, [ // VAR in mssql
                new FunctionArgumentSymbol("expression"),
            ]),
            new FunctionSymbol("VAR_SAMP", IsAggregate: true, [ // VARP in mssql
                new FunctionArgumentSymbol("expression"),
            ]),
            /*
             *      COVAR_POP(x, y) in t-sql woudl translate to :
             *      
             *     AVG(CASE WHEN x IS NOT NULL AND y IS NOT NULL THEN x * y END) - 
             *     (AVG(CASE WHEN y IS NOT NULL THEN x END) * 
             *       AVG(CASE WHEN x IS NOT NULL THEN y END))
             */
            new FunctionSymbol("COVAR_POP", IsAggregate: true, [
                new FunctionArgumentSymbol("x"),
                new FunctionArgumentSymbol("y"),
            ]),
            /*
             *      COVAR_SAMP(x, y) in t-sql woudl translate to :
             *      
             *     (COUNT(CASE WHEN x IS NOT NULL AND y IS NOT NULL THEN 1 END) * 
             *     
             *      (AVG(CASE WHEN x IS NOT NULL AND y IS NOT NULL THEN x * y END) - 
             *       (AVG(CASE WHEN y IS NOT NULL THEN x END) * 
             *        AVG(CASE WHEN x IS NOT NULL THEN y END)))
             *        
             *        ) / 
             *     NULLIF(COUNT(CASE WHEN x IS NOT NULL AND y IS NOT NULL THEN 1 END) - 1, 0)
             */
             new FunctionSymbol("COVAR_SAMP", IsAggregate: true, [
                new FunctionArgumentSymbol("x"),
                new FunctionArgumentSymbol("y"),
             ]),

             // https://en.wikibooks.org/wiki/SQL_Dialects_Reference/Functions_and_expressions/String_functions
             new FunctionSymbol("POSITION", IsAggregate: true, [ // CHARINDEX(expression.SubValue, expression.Expression)
                new FunctionArgumentSymbol("exp", RequiredType: typeof(InExpression)),
             ]),
             new FunctionSymbol("LOWER", IsAggregate: true, [
                new FunctionArgumentSymbol("x"),
             ]),
             new FunctionSymbol("UPPER", IsAggregate: true, [
                new FunctionArgumentSymbol("x"),
             ]),
             // require special parser handling for `TRIM({LEADING|TRAILING|BOTH} [' '] FROM x)`
             //new FunctionSymbol("TRIM", IsAggregate: true, [
             //   new FunctionArgumentSymbol("x"),
             //]),

             // require special parser handling for `SUBSTRING(str FROM start [FOR len])`
             //new FunctionSymbol("SUBSTRING", IsAggregate: true, [
             //   new FunctionArgumentSymbol("x"),
             //]),
             
             new FunctionSymbol("CHAR_LENGTH", IsAggregate: true, [ // LEN in mssql
                new FunctionArgumentSymbol("x"),
             ]),
             new FunctionSymbol("CHARACTER_LENGTH", IsAggregate: true, [ // LEN in mssql
                new FunctionArgumentSymbol("x"),
             ]),

        }.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

    public static FunctionSymbol? GetFunction(string name)
    {
        if (functions.TryGetValue(name, out var function))
        {
            return function;
        }

        return null;
    }
}
