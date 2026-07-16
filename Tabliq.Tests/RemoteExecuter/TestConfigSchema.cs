using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Tabliq.RemoteExecuter;
using Tabliq.Sql.Binding;

namespace Tabliq.Tests.RemoteExecuter;


public class TestConfigSchema
{
    private static readonly Lazy<VirtualSchema> _schemaFriendlyNamesVirtualSchema;
    private static readonly Lazy<VirtualSchema> _schemaVirtualSchema;
    private static readonly Lazy<VirtualSchema> _anonSVirtualSchema;
    private static readonly Lazy<VirtualSchema> _wmsSVirtualSchema;

    public static VirtualSchema SchemaFriendlyNamesSchema => _schemaFriendlyNamesVirtualSchema.Value;
    public static VirtualSchema SchemaVirtualSchema => _schemaVirtualSchema.Value;
    public static VirtualSchema AnonVirtualSchema => _anonSVirtualSchema.Value;
    public static VirtualSchema WmsVirtualSchema => _wmsSVirtualSchema.Value;

    private static VirtualSchema Load(string json)
    {
        var tables = JsonSerializer.Deserialize<List<DatabaseTable>>(json)!;
        return new VirtualSchema
        {
            Tables = tables.Select(x => x.AsVirtualTable()).ToList()
        };
    }
    
    static TestConfigSchema()
    {
        _schemaVirtualSchema = new Lazy<VirtualSchema>(() => Load(Resources.Schemas.Default));
        _schemaFriendlyNamesVirtualSchema = new Lazy<VirtualSchema>(() => Load(Resources.Schemas.FriendlyNames));
        _anonSVirtualSchema = new Lazy<VirtualSchema>(() => Load(Resources.Schemas.AnonSchema));
        _wmsSVirtualSchema = new Lazy<VirtualSchema>(() => Load(Resources.Schemas.WmsSchema));
    }

    public class DatabaseTable
    {
        // this is the virtual table name to be used in the sql query
        public required string Name { get; set; }

        public List<DatabaseColumn> Columns { get; set; } = [];

        // description to pass along to help the llm understand the data
        public string? Description { get; set; }

        // this is eather a table name or a subselect representing the table
        public required string RemoteTableSql { get; set; }

        // per user we will allow defining table row filters to apply to each of these
        // per user we will allow defining column filters (to exclude visibility of the column at all)
        // per user we will allow defining table filter (to exclude visibility of the table at all)

        public VirtualTable AsVirtualTable()
        {
            return new VirtualTable(Name, RemoteTableSql)
            {
                Columns = Columns.Select(c => c.AsVirtualColumn()).ToList()
            };
        }
    }

    public class DatabaseColumn
    {
        public required string Name { get; set; }

        public required string DataType { get; set; }

        // could be a column name or even an expression (basically it will be injected in place of the column name).
        public required string RemoteColumnSql { get; set; }

        // description to pass along to help the llm understand the data
        public string? Description { get; set; }
        public bool ExcludeFromStarExpansion { get; set; } = false;

        public VirtualColumn AsVirtualColumn()
        {
            return new VirtualColumn(Name, DataType, true, RemoteColumnSql)
            {
                ExcludeFromExpansion = ExcludeFromStarExpansion,
            };
        }
    }

    public enum DatabaseType
    {
        Unknown,
        MsSql,
    }
}
