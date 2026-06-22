using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;
using Tabliq.Sql.Core;
using Tabliq.Sql.Printer;

namespace Tabliq.RemoteExecuter;

public class VirtualTable
{
    public VirtualTable(string tableName, string? remoteSql = null)
    {
        // use the correct sql writer??
        TableName = tableName;

        // TODO move this block to some db specific sql generator
        remoteSql ??= new SqlWriter().ToSql(new IdentifierExpression(tableName));
        if (remoteSql.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            var parsed = Sql.Parsing.Parser.Parse(remoteSql); // validate the sql

            if (parsed.Diagnostics.Any())
            {
                throw new Exception($"Invalid remote SQL: {remoteSql}. Diagnostics: {string.Join(", ", parsed.Diagnostics.Select(d => d.Message))}");
            }
            var statement = parsed.Script.Statements[0] as SelectStatement;
            if (statement?.CommonTableExpressions.Any() == true)
            {
                throw new Exception($"Invalid remote SQL: {remoteSql}. Common table expressions are not supported in remote SQL.");
            }

            RemoteSql = statement?.SelectQuery ?? throw new Exception($"Invalid remote SQL: {remoteSql}");
        }
        else
        {
            // TODO: need to add options into the parser to allow for parsing sql fragments not just SqlScripts
            var sql = $"SELECT * FROM {remoteSql}".ToString(); // this is a hack to make sure the sql is valid, we could do a better validation here
            var parsed = Sql.Parsing.Parser.Parse(sql); // validate the sql
            if (parsed.Diagnostics.Any())
            {
                throw new Exception($"Invalid remote SQL: {remoteSql}. Diagnostics: {string.Join(", ", parsed.Diagnostics.Select(d => d.Message))}");
            }
            RemoteSqlTableName = ((parsed.Script.Statements[0] as SelectStatement)?.SelectQuery.From?.TableReferences[0] as NamedTableReference)?.Identifer ?? throw new Exception($"Invalid remote SQL: {remoteSql}");
        }
    }

    public VirtualTable(string tableName, SelectExpression selectExpression)
    {
        TableName = tableName;
        RemoteSql = selectExpression;
    }

    public VirtualTable(string tableName, IdentifierExpression remoteSqlTableName)
    {
        TableName = tableName;
        RemoteSqlTableName = remoteSqlTableName;
    }

    public IdentifierExpression? RemoteSqlTableName { get; }

    public SelectExpression? RemoteSql { get; }

    public string TableName { get; set; }

    public List<VirtualColumn> Columns { get; set; } = [];

    public TableSymbol AsSymbol()
        => new TableSymbol(TableName, Columns.Select(x => x.AsSymbol()).ToList())
        {
            State = this
        };
}
