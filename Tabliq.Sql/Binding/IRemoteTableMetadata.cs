using Tabliq.Sql.Ast;

namespace Tabliq.Sql.Binding;

public interface IRemoteTableMetadata
{
    IdentifierExpression? RemoteSqlTableName { get; }
    SelectExpression? RemoteSql { get; }
}
