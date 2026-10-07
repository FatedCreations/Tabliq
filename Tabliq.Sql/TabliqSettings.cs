using System;
using System.Collections.Generic;
using System.Text;

namespace Tabliq.Sql;

public class TabliqSettings
{
    public int? MaxTokenLimit { get; set; } = 100000;

    public int? MaxSubQueryDepth { get; set; } = 64;

    public int? MaxExpressionDepth { get; set; } = 256;

    public int? MaxConditionDepth { get; set; } = 256;

}
