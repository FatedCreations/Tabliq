using System;
using System.Collections.Generic;
using System.Text;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;

namespace Tabliq.RemoteExecuter.MsSql;

public static class BuiltinFunctions
{
    private static List<FunctionSymbol> functions = new List<FunctionSymbol>
    {
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/date-bucket-transact-sql?view=sql-server-ver17
        new FunctionSymbol(
            "DATE_BUCKET",
            IsAggregate: false,
            [
                new FunctionArgumentSymbol("datepart", BinderHandling: BinderHandling.Skip), // datepart symbol.
                new FunctionArgumentSymbol("number"),
                new FunctionArgumentSymbol("date"), // date expression
                new FunctionArgumentSymbol("origin", Optional: true)
            ]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/dateadd-transact-sql?view=sql-server-ver17
        new FunctionSymbol(
            "DATEADD",
            IsAggregate: false,
            [
                new FunctionArgumentSymbol("datepart", BinderHandling: BinderHandling.Skip), // datepart symbol.
                new FunctionArgumentSymbol("count"),
                new FunctionArgumentSymbol("date") // date expression
            ]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/datediff-transact-sql?view=sql-server-ver17
        new FunctionSymbol(
            "DATEDIFF",
            IsAggregate: false,
            [
                new FunctionArgumentSymbol("datepart", BinderHandling: BinderHandling.Skip), // datepart symbol.
                new FunctionArgumentSymbol("startDate"), // date expression
                new FunctionArgumentSymbol("endDate") // date expression
            ]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/datediff-big-transact-sql?view=sql-server-ver17
        new FunctionSymbol(
            "DATEDIFF_BIG",
            IsAggregate: false,
            [
                new FunctionArgumentSymbol("datepart", BinderHandling: BinderHandling.Skip), // datepart symbol.
                new FunctionArgumentSymbol("startDate"), // date expression
                new FunctionArgumentSymbol("endDate") // date expression
            ]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/datefromparts-transact-sql?view=sql-server-ver17
        new FunctionSymbol(
            "DATEFROMPARTS",
            IsAggregate: false,
            [
                new FunctionArgumentSymbol("year"), // year expression
                new FunctionArgumentSymbol("month"), // month expression
                new FunctionArgumentSymbol("day") // day expression
            ]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/datename-transact-sql?view=sql-server-ver17
        new FunctionSymbol(
            "DATENAME",
            IsAggregate: false,
            [
                new FunctionArgumentSymbol("datepart", BinderHandling: BinderHandling.Skip), // datepart symbol.
                new FunctionArgumentSymbol("date") // date expression
            ]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/datepart-transact-sql?view=sql-server-ver17
        new FunctionSymbol(
            "DATEPART",
            IsAggregate: false,
            [
                new FunctionArgumentSymbol("datepart", BinderHandling: BinderHandling.Skip), // datepart symbol.
                new FunctionArgumentSymbol("date") // date expression
            ]),

        // https://learn.microsoft.com/en-us/sql/t-sql/functions/datetime2fromparts-transact-sql?view=sql-server-ver17
        new FunctionSymbol(
            "DATETIME2FROMPARTS",
            IsAggregate: false,
            [
                new FunctionArgumentSymbol("year"), // year expression
                new FunctionArgumentSymbol("month"), // month expression
                new FunctionArgumentSymbol("day"), // day expression
                new FunctionArgumentSymbol("hour"), // hour expression
                new FunctionArgumentSymbol("minute"), // minute expression
                new FunctionArgumentSymbol("second"), // second expression
                new FunctionArgumentSymbol("fractions"), // fractions expression
                new FunctionArgumentSymbol("precision"), // precision expression
            ]),

        // https://learn.microsoft.com/en-us/sql/t-sql/functions/datetimefromparts-transact-sql?view=sql-server-ver17
        new FunctionSymbol(
            "DATETIMEFROMPARTS",
            IsAggregate: false,
            [
                new FunctionArgumentSymbol("year"), // year expression
                new FunctionArgumentSymbol("month"), // month expression
                new FunctionArgumentSymbol("day"), // day expression
                new FunctionArgumentSymbol("hour"), // hour expression
                new FunctionArgumentSymbol("minute"), // minute expression
                new FunctionArgumentSymbol("second"), // second expression
                new FunctionArgumentSymbol("milliseconds"), // milliseconds expression
            ]),

        // https://learn.microsoft.com/en-us/sql/t-sql/functions/datetimeoffsetfromparts-transact-sql?view=sql-server-ver17
        new FunctionSymbol(
            "DATETIMEOFFSETFROMPARTS",
            IsAggregate: false,
            [
                new FunctionArgumentSymbol("year"), // year expression
                new FunctionArgumentSymbol("month"), // month expression
                new FunctionArgumentSymbol("day"), // day expression
                new FunctionArgumentSymbol("hour"), // hour expression
                new FunctionArgumentSymbol("minute"), // minute expression
                new FunctionArgumentSymbol("second"), // second expression
                new FunctionArgumentSymbol("fractions"), // fractions expression
                new FunctionArgumentSymbol("hour_offset"), // hour offset expression
                new FunctionArgumentSymbol("minute_offset"), // minute offset expression
                new FunctionArgumentSymbol("precision"), // precision expression
            ]),

        // https://learn.microsoft.com/en-us/sql/t-sql/functions/datetrunc-transact-sql?view=sql-server-ver17
        new FunctionSymbol(
            "DATETRUNC",
            IsAggregate: false,
            [
                new FunctionArgumentSymbol("datepart", BinderHandling: BinderHandling.Skip), // datepart symbol.
                new FunctionArgumentSymbol("date") // date expression
            ]),

        // https://learn.microsoft.com/en-us/sql/t-sql/functions/day-transact-sql?view=sql-server-ver17
        new FunctionSymbol("DAY", IsAggregate: false, [new FunctionArgumentSymbol("date")]),

        // https://learn.microsoft.com/en-us/sql/t-sql/functions/eomonth-transact-sql?view=sql-server-ver17
        new FunctionSymbol("EOMONTH", IsAggregate: false, [new FunctionArgumentSymbol("start_date"), new FunctionArgumentSymbol("month_to_add", Optional: true)]),

        // https://learn.microsoft.com/en-us/sql/t-sql/functions/getdate-transact-sql?view=sql-server-ver17
        new FunctionSymbol("GETDATE", IsAggregate: false, []),

        // https://learn.microsoft.com/en-us/sql/t-sql/functions/getutcdate-transact-sql?view=sql-server-ver17
        new FunctionSymbol("GETUTCDATE", IsAggregate: false, []),

        // https://learn.microsoft.com/en-us/sql/t-sql/functions/isdate-transact-sql?view=sql-server-ver17
        new FunctionSymbol("ISDATE", IsAggregate: false, [new FunctionArgumentSymbol("date")]),

        // https://learn.microsoft.com/en-us/sql/t-sql/functions/month-transact-sql?view=sql-server-ver17
        new FunctionSymbol("MONTH", IsAggregate: false, [new FunctionArgumentSymbol("expression")]),

        // https://learn.microsoft.com/en-us/sql/t-sql/functions/smalldatetimefromparts-transact-sql?view=sql-server-ver17
        new FunctionSymbol(
            "SMALLDATETIMEFROMPARTS",
            IsAggregate: false,
            [
                new FunctionArgumentSymbol("year"), // year expression
                new FunctionArgumentSymbol("month"), // month expression
                new FunctionArgumentSymbol("day"), // day expression
                new FunctionArgumentSymbol("hour"), // hour expression
                new FunctionArgumentSymbol("minute"), // minute expression
            ]),

        // https://learn.microsoft.com/en-us/sql/t-sql/functions/switchoffset-transact-sql?view=sql-server-ver17
        new FunctionSymbol(
            "SWITCHOFFSET",
            IsAggregate: false,
            [
                new FunctionArgumentSymbol("datetimeoffset_expression"), // datetimeoffset expression
                new FunctionArgumentSymbol("timezoneoffset_expression"), // timezoneoffset expression
            ]),

        // https://learn.microsoft.com/en-us/sql/t-sql/functions/sysdatetime-transact-sql?view=sql-server-ver17
        new FunctionSymbol("SYSDATETIME", IsAggregate: false, []),

        // https://learn.microsoft.com/en-us/sql/t-sql/functions/sysdatetimeoffset-transact-sql?view=sql-server-ver17
        new FunctionSymbol("SYSDATETIMEOFFSET", IsAggregate: false, []),

        // https://learn.microsoft.com/en-us/sql/t-sql/functions/sysutcdatetime-transact-sql?view=sql-server-ver17
        new FunctionSymbol("SYSUTCDATETIME", IsAggregate: false, []),

        // https://learn.microsoft.com/en-us/sql/t-sql/functions/timefromparts-transact-sql?view=sql-server-ver17
        new FunctionSymbol(
            "TIMEFROMPARTS",
            IsAggregate: false,
            [
                new FunctionArgumentSymbol("hour"), // hour expression
                new FunctionArgumentSymbol("minute"), // minute expression
                new FunctionArgumentSymbol("second"), // second expression
                new FunctionArgumentSymbol("fractions"), // fractions expression
                new FunctionArgumentSymbol("precision"), // precision expression
            ]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/todatetimeoffset-transact-sql?view=sql-server-ver17
        new FunctionSymbol(
            "TODATETIMEOFFSET",
            IsAggregate: false,
            [
                new FunctionArgumentSymbol("datetimeoffset_expression"), // datetimeoffset expression
                new FunctionArgumentSymbol("timezoneoffset_expression"), // timezoneoffset expression
            ]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/year-transact-sql?view=sql-server-ver17
        new FunctionSymbol("YEAR", IsAggregate: false, [new FunctionArgumentSymbol("expression")]),

        // agg functions
        // inherits the built-in aggregate functions from the base class, but add a few more that are specific to SQL Server.
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/count-big-transact-sql?view=sql-server-ver17
        new FunctionSymbol("COUNT_BIG", IsAggregate: true, [new FunctionArgumentSymbol("expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/product-aggregate-transact-sql?view=sql-server-ver17
        new FunctionSymbol("PRODUCT", IsAggregate: true, [new FunctionArgumentSymbol("expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/stdev-transact-sql?view=sql-server-ver17
        new FunctionSymbol("STDEV", IsAggregate: true, [new FunctionArgumentSymbol("expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/stdevp-transact-sql?view=sql-server-ver17
        new FunctionSymbol("STDEVP", IsAggregate: true, [new FunctionArgumentSymbol("expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/var-transact-sql?view=sql-server-ver17
        new FunctionSymbol("VAR", IsAggregate: true, [new FunctionArgumentSymbol("expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/varp-transact-sql?view=sql-server-ver17
        new FunctionSymbol("VARP", IsAggregate: true, [new FunctionArgumentSymbol("expression")]),

        // Analytic functions
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/cume-dist-transact-sql?view=sql-server-ver17
        new FunctionSymbol("CUME_DIST", IsAggregate: false, []),

        // https://learn.microsoft.com/en-us/sql/t-sql/functions/first-value-transact-sql?view=sql-server-ver17
        new FunctionSymbol("FIRST_VALUE", IsAggregate: false, [new FunctionArgumentSymbol("scalar_expression", Optional: true)]),

        // https://learn.microsoft.com/en-us/sql/t-sql/functions/lag-transact-sql?view=sql-server-ver17
        new FunctionSymbol(
            "LAG",
            IsAggregate: false,
            [
                new FunctionArgumentSymbol("scalar_expression"),
                new FunctionArgumentSymbol("offset", Optional: true),
                new FunctionArgumentSymbol("default", Optional: true),
            ]),

        // https://learn.microsoft.com/en-us/sql/t-sql/functions/last-value-transact-sql?view=sql-server-ver17
        new FunctionSymbol(
            "LAST_VALUE",
            IsAggregate: false,
            [
                new FunctionArgumentSymbol("scalar_expression", Optional: true)
            ]),

        // https://learn.microsoft.com/en-us/sql/t-sql/functions/lead-transact-sql?view=sql-server-ver17
        new FunctionSymbol(
            "LEAD",
            IsAggregate: false,
            [
                new FunctionArgumentSymbol("scalar_expression"),
                new FunctionArgumentSymbol("offset", Optional: true),
                new FunctionArgumentSymbol("defaul", Optional: true),
            ]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/percentile-cont-transact-sql?view=sql-server-ver17
        new FunctionSymbol(
            "PERCENTILE_CONT",
            IsAggregate: false,
            [
                new FunctionArgumentSymbol("numeric_literal")
            ]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/percentile-disc-transact-sql?view=sql-server-ver17
        new FunctionSymbol(
            "PERCENTILE_DISC",
            IsAggregate: false,
            [
                new FunctionArgumentSymbol("numeric_literal"),
            ]),

        // https://learn.microsoft.com/en-us/sql/t-sql/functions/percent-rank-transact-sql?view=sql-server-ver17
        new FunctionSymbol("PERCENT_RANK", IsAggregate: false, []),

        // Conversion

        // https://learn.microsoft.com/en-us/sql/t-sql/functions/cast-and-convert-transact-sql?view=sql-server-ver17
        new FunctionSymbol(
            "CONVERT",
            IsAggregate: false,
            [
                new FunctionArgumentSymbol("datatype", BinderHandling: BinderHandling.Skip), // datepart symbol.
                new FunctionArgumentSymbol("expression"), // date expression
                new FunctionArgumentSymbol("style", Optional: true), // optional style argument
            ]),

        // https://learn.microsoft.com/en-us/sql/t-sql/functions/cast-and-convert-transact-sql?view=sql-server-ver17
        new FunctionSymbol(
            "CAST",
            IsAggregate: false,
            [
                new FunctionArgumentSymbol("as_expression", RequiredType: typeof(AsExpression)),
            ]),

        // https://learn.microsoft.com/en-us/sql/t-sql/functions/parse-transact-sql?view=sql-server-ver17
        new FunctionSymbol(
            "PARSE",
            IsAggregate: false,
            [
                new FunctionArgumentSymbol("as_expression", RequiredType: typeof(AsExpression)),
            ]),

        // https://learn.microsoft.com/en-us/sql/t-sql/functions/try-cast-transact-sql?view=sql-server-ver17
        new FunctionSymbol(
            "TRY_CAST",
            IsAggregate: false,
            [
                new FunctionArgumentSymbol("as_expression", RequiredType: typeof(AsExpression)),
            ]),

        // https://learn.microsoft.com/en-us/sql/t-sql/functions/try-parse-transact-sql?view=sql-server-ver17
        new FunctionSymbol(
            "TRY_PARSE",
            IsAggregate: false,
            [
                new FunctionArgumentSymbol("as_expression", RequiredType: typeof(AsExpression)),
            ]),

        // https://learn.microsoft.com/en-us/sql/t-sql/functions/try-convert-transact-sql?view=sql-server-ver17
        new FunctionSymbol(
            "TRY_CONVERT",
            IsAggregate: false,
            [
                new FunctionArgumentSymbol("datatype", BinderHandling: BinderHandling.Skip), // datepart symbol.
                new FunctionArgumentSymbol("expression"), // date expression
                new FunctionArgumentSymbol("style", Optional: true), // optional style argument
            ]),

        // math functions
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/abs-transact-sql?view=sql-server-ver17
        new FunctionSymbol("ABS", IsAggregate: false, [new FunctionArgumentSymbol("numeric_expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/acos-transact-sql?view=sql-server-ver17
        new FunctionSymbol("ACOS", IsAggregate: false, [new FunctionArgumentSymbol("numeric_expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/asin-transact-sql?view=sql-server-ver17
        new FunctionSymbol("ASIN", IsAggregate: false, [new FunctionArgumentSymbol("numeric_expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/atan-transact-sql?view=sql-server-ver17
        new FunctionSymbol("ATAN", IsAggregate: false, [new FunctionArgumentSymbol("numeric_expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/atn2-transact-sql?view=sql-server-ver17
        new FunctionSymbol("ATN2", IsAggregate: false, [new FunctionArgumentSymbol("x"), new FunctionArgumentSymbol("y")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/cos-transact-sql?view=sql-server-ver17
        new FunctionSymbol("COS", IsAggregate: false, [new FunctionArgumentSymbol("numeric_expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/cot-transact-sql?view=sql-server-ver17
        new FunctionSymbol("COT", IsAggregate: false, [new FunctionArgumentSymbol("numeric_expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/degrees-transact-sql?view=sql-server-ver17
        new FunctionSymbol("DEGREES", IsAggregate: false, [new FunctionArgumentSymbol("numeric_expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/exp-transact-sql?view=sql-server-ver17
        new FunctionSymbol("EXP", IsAggregate: false, [new FunctionArgumentSymbol("numeric_expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/floor-transact-sql?view=sql-server-ver17
        new FunctionSymbol("FLOOR", IsAggregate: false, [new FunctionArgumentSymbol("numeric_expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/log-transact-sql?view=sql-server-ver17
        new FunctionSymbol("LOG", IsAggregate: false, [new FunctionArgumentSymbol("numeric_expression"),  new FunctionArgumentSymbol("base", Optional: true)]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/log10-transact-sql?view=sql-server-ver17
        new FunctionSymbol("LOG10", IsAggregate: false, [new FunctionArgumentSymbol("numeric_expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/pi-transact-sql?view=sql-server-ver17
        new FunctionSymbol("PI", IsAggregate: false, []),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/power-transact-sql?view=sql-server-ver17
        new FunctionSymbol("POWER", IsAggregate: false, [new FunctionArgumentSymbol("numeric_expression"),  new FunctionArgumentSymbol("exponent")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/radians-transact-sql?view=sql-server-ver17
        new FunctionSymbol("RADIANS", IsAggregate: false, [new FunctionArgumentSymbol("numeric_expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/rand-transact-sql?view=sql-server-ver17
        new FunctionSymbol("RAND", IsAggregate: false, [new FunctionArgumentSymbol("seed", Optional: true)]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/round-transact-sql?view=sql-server-ver17
        new FunctionSymbol("ROUND", IsAggregate: false, [new FunctionArgumentSymbol("numeric_expression"),  new FunctionArgumentSymbol("length"),  new FunctionArgumentSymbol("function", Optional: true)]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/sign-transact-sql?view=sql-server-ver17
        new FunctionSymbol("SIGN", IsAggregate: false, [new FunctionArgumentSymbol("numeric_expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/sin-transact-sql?view=sql-server-ver17
        new FunctionSymbol("SIN", IsAggregate: false, [new FunctionArgumentSymbol("numeric_expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/sqrt-transact-sql?view=sql-server-ver17
        new FunctionSymbol("SQRT", IsAggregate: false, [new FunctionArgumentSymbol("numeric_expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/square-transact-sql?view=sql-server-ver17
        new FunctionSymbol("SQUARE", IsAggregate: false, [new FunctionArgumentSymbol("numeric_expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/tan-transact-sql?view=sql-server-ver17
        new FunctionSymbol("TAN", IsAggregate: false, [new FunctionArgumentSymbol("numeric_expression")]),

        // Logical functions
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/logical-functions-greatest-transact-sql?view=sql-server-ver17
        new FunctionSymbol("GREATEST", IsAggregate: false, [new FunctionArgumentSymbol("numeric_expression")], ParamsArgument: new FunctionArgumentSymbol("numeric_expression")),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/logical-functions-least-transact-sql?view=sql-server-ver17
        new FunctionSymbol("LEAST", IsAggregate: false, [new FunctionArgumentSymbol("numeric_expression")], ParamsArgument: new FunctionArgumentSymbol("numeric_expression")),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/logical-functions-iif-transact-sql?view=sql-server-ver17
        new FunctionSymbol("IIF", IsAggregate: false, [new FunctionArgumentSymbol("boolean_expression"), new FunctionArgumentSymbol("true_value"), new FunctionArgumentSymbol("false_value")]),

        //RANKING
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/dense-rank-transact-sql?view=sql-server-ver17
        new FunctionSymbol("DENSE_RANK", IsAggregate: false, []),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/ntile-transact-sql?view=sql-server-ver17
        new FunctionSymbol("NTILE", IsAggregate: false, [new FunctionArgumentSymbol("integer_expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/rank-transact-sql?view=sql-server-ver17
        new FunctionSymbol("RANK", IsAggregate: false, []),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/row-number-transact-sql?view=sql-server-ver17
        new FunctionSymbol("ROW_NUMBER", IsAggregate: false, []),

        // string functions
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/ascii-transact-sql?view=sql-server-ver17
        new FunctionSymbol("ASCII", IsAggregate: false, [new FunctionArgumentSymbol("character_expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/char-transact-sql?view=sql-server-ver17
        new FunctionSymbol("CHAR", IsAggregate: false, [new FunctionArgumentSymbol("integer_expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/charindex-transact-sql?view=sql-server-ver17
        new FunctionSymbol("CHARINDEX", IsAggregate: false, [new FunctionArgumentSymbol("expression_to_find"), new FunctionArgumentSymbol("expression_to_search"), new FunctionArgumentSymbol("start_location", Optional: true)]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/concat-transact-sql?view=sql-server-ver17
        new FunctionSymbol("CONCAT", IsAggregate: false, [new FunctionArgumentSymbol("arg1")], new FunctionArgumentSymbol("argOthers")),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/concat-ws-transact-sql?view=sql-server-ver17
        new FunctionSymbol(
            "CONCAT_WS",
            IsAggregate: false,
            [
                new FunctionArgumentSymbol("separator"),
                new FunctionArgumentSymbol("arg1")
            ],
            new FunctionArgumentSymbol("argOthers")),

        // https://learn.microsoft.com/en-us/sql/t-sql/functions/difference-transact-sql?view=sql-server-ver17
        new FunctionSymbol("DIFFERENCE", IsAggregate: false, [new FunctionArgumentSymbol("character_expression1"), new FunctionArgumentSymbol("character_expression2")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/format-transact-sql?view=sql-server-ver17
        new FunctionSymbol("FORMAT", IsAggregate: false, [new FunctionArgumentSymbol("value"), new FunctionArgumentSymbol("format"), new FunctionArgumentSymbol("culture", Optional: true)]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/left-transact-sql?view=sql-server-ver17
        new FunctionSymbol("LEFT", IsAggregate: false, [new FunctionArgumentSymbol("string_expression"), new FunctionArgumentSymbol("integer_expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/len-transact-sql?view=sql-server-ver17
        new FunctionSymbol("LEN", IsAggregate: false, [new FunctionArgumentSymbol("string_expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/lower-transact-sql?view=sql-server-ver17
        new FunctionSymbol("LOWER", IsAggregate: false, [new FunctionArgumentSymbol("string_expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/ltrim-transact-sql?view=sql-server-ver17
        new FunctionSymbol("LTRIM", IsAggregate: false, [new FunctionArgumentSymbol("string_expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/nchar-transact-sql?view=sql-server-ver17
        new FunctionSymbol("NCHAR", IsAggregate: false, [new FunctionArgumentSymbol("integer_expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/patindex-transact-sql?view=sql-server-ver17
        new FunctionSymbol("PATINDEX", IsAggregate: false, [new FunctionArgumentSymbol("pattern"), new FunctionArgumentSymbol("expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/right-transact-sql?view=sql-server-ver17
        new FunctionSymbol("RIGHT", IsAggregate: false, [new FunctionArgumentSymbol("string_expression"), new FunctionArgumentSymbol("integer_expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/rtrim-transact-sql?view=sql-server-ver17
        new FunctionSymbol("RTRIM", IsAggregate: false, [new FunctionArgumentSymbol("string_expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/trim-transact-sql?view=sql-server-ver17
        new FunctionSymbol("TRIM", IsAggregate: false, [new FunctionArgumentSymbol("string_expression")]),
        // https://learn.microsoft.com/en-us/sql/t-sql/functions/substring-transact-sql?view=sql-server-ver17
        new FunctionSymbol("SUBSTRING", IsAggregate: false, [new FunctionArgumentSymbol("expression"), new FunctionArgumentSymbol("start"), new FunctionArgumentSymbol("length", Optional: true)]),

        // fake functions to make the parser work, handle special group by types
        new FunctionSymbol("ROLLUP", IsAggregate: false, [new FunctionArgumentSymbol("expression")], ParamsArgument: new FunctionArgumentSymbol("expression")),
        new FunctionSymbol("CUBE", IsAggregate: false, [new FunctionArgumentSymbol("expression")], ParamsArgument: new FunctionArgumentSymbol("expression")),
    };

    public static IReadOnlyList<FunctionSymbol> Functions => functions;
}
