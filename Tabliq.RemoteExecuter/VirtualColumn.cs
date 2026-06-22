using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;
using Tabliq.Sql.Core;
using Tabliq.Sql.Printer;

namespace Tabliq.RemoteExecuter;

public class VirtualColumn
{
    public VirtualColumn(string colName, string dataType, bool isNullable, string remoteSql) // this should be a syntax node, but for now just a string
    {
        RemoteColumnName = remoteSql;
        ColumnName = colName;
        DataType = dataType;
        IsNullable = isNullable;
    }

    public ColumnSymbol AsSymbol()
        => new ColumnSymbol(ColumnName, DataType)
        {
            State = this
        };


    public string RemoteColumnName { get; }
    public string ColumnName { get; }
    public string DataType { get; }
    public bool IsNullable { get; }
    public bool ExcludeFromExpansion { get; init; }
}
