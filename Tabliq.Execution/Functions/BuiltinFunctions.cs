using System;
using System.Collections.Generic;
using System.Text;
using Tabliq.Sql.Core;

namespace Tabliq.Execution.Functions;

public static class BuiltinFunctions
{
    public static IEnumerable<SqlFunction> BuiltingFunctions { get; } = [
            new CountFunction(),
            new MonthFunction(),
            new YearFunction(),
            new DayFunction(),
            new RightFunction(),
            new LeftFunction(),
       ];
}
