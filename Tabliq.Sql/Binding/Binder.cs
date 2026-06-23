//using System.Collections.Generic;
//using Tabliq.Sql.Core;
//using Tabliq.Sql.Diagnostics;
//using Tabliq.Sql.Parsing.Nodes;

using System.Net.Http.Headers;
using System.Reflection;
using System.Xml.Linq;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Core;
using Tabliq.Sql.Diagnostics;
using Tabliq.Sql.Lexing;
using Tabliq.Sql.Parsing;

namespace Tabliq.Sql.Binding;

public class Binder
{
    public static CompilationResult Bind(CompilationResult syntaxTree, ISchemaProvider catalog)
    {
        var binder = new Binder(catalog);

        // Binding logic will go here
        return binder.Bind(syntaxTree);
    }

    private DiagnosticBag _diagnostics = new();
    private readonly ISchemaProvider _catalog;

    public DiagnosticBag Diagnostics => _diagnostics;
    public Binder(ISchemaProvider catalog)
    {
        _catalog = catalog;
    }
    private BindingScope Current { get; set; }

    private FunctionSymbol? LookupFunction(string name)
    {
        return _catalog.GetFunction(name) ?? BuiltInFunctions.GetFunction(name);
    }

    public CompilationResult Bind(CompilationResult root)
    {
        _diagnostics = new DiagnosticBag(); // reset state!

        Bind(root.Script);

        var diags = new List<Diagnostic>();

        diags.AddRange(root.Diagnostics);
        diags.AddRange(_diagnostics.Diagnostics);

        return new CompilationResult(root.Text, root.Script, root.Tokens, diags);
    }

    private void Bind(SqlScript sqlScript)
    {
        Current = new BindingScope(_catalog);

        BindChildren(sqlScript);
    }

    void WithProjectionsInScope(Action action)
    {
        var prev = Current.CanBindToProjections;
        Current.CanBindToProjections = true;
        try
        {
            action();
        }
        finally
        {
            Current.CanBindToProjections = prev;
        }
    }
    void WithNewTable(string name, Action action)
    {
        var previous = Current;
        Current = new BindingScope(name, previous);
        try
        {
            action();
        }
        finally
        {
            var boundTable = Current.Build();
            if (boundTable is not null)
            {
                previous.AddTableToCatalog(boundTable); // ?? this might be wrong and we need to add to a scoped catalog!
            }
            Current = previous;
        }
    }

    void Bind(SyntaxNode node)
    {
        // switchng logic here to bind different node types
        switch (node)
        {
            case ParameterIdentifier ParameterIdentifier:
                Bind(ParameterIdentifier);
                // we need to suppress walking the from clause, as it will be handled separately as it needs preprocessing before the other part to discover/process tables in scope
                break;
            case StarIdentifierExpression StarIdentifierExpression:
                Bind(StarIdentifierExpression);
                // we need to suppress walking the from clause, as it will be handled separately as it needs preprocessing before the other part to discover/process tables in scope
                break;
            case SelectStatement selectStatement:
                // we need to init a new catalog scope for the select statment, with inline names/aliases overriding parent scope tables
                Bind(selectStatement);
                break;
            case SelectExpression selectExpression:
                Bind(selectExpression);
                break;
            case CommonTableExpression cte:
                // we need to extract a TableSymbol from the CTE and add it to the catalog for the select statment
                Bind(cte);
                break;
            case FromClause:
                // we need to suppress walking the from clause, as it will be handled separately as it needs preprocessing before the other part to discover/process tables in scope
                break;
            case NamedTableReference NamedTableReference:
                Bind(NamedTableReference);
                break;
            case SelectTableReference SelectTableReference:
                Bind(SelectTableReference);
                break;
            case IdentifierExpression IdentifierExpression:
                Bind(IdentifierExpression);
                break;
            case SelectProjection SelectProjection:
                Bind(SelectProjection);
                break;
            case FunctionCallExpression FunctionCallExpression:
                Bind(FunctionCallExpression);
                break;
            case OrderByClause OrderByClause:
                Bind(OrderByClause);
                break;
            default:
                BindChildren(node);
                break;
        }
    }

    private void Bind(OrderByClause p)
    {
        WithProjectionsInScope(() =>
        {
            BindChildren(p);
        });
    }

    private void Bind(FunctionCallExpression p)
    {
        var def = LookupFunction(p.FunctionName);
        p.Binding = def;

        if (def is null)
        {
            BindChildren(p);
            return;
        }

        var countOfRequired = def.Arguments.Count(a => !a.Optional);

        if (p.Arguments.Count < countOfRequired)
        {
            Diagnostics.Report("InvalidArgumentCount", $"Function '{p.FunctionName}' requires at least {countOfRequired} arguments", p.Span);
            return;
        }

        for (var i = 0; i < p.Arguments.Count; i++)
        {
            var argumentDef = def.Arguments.Count > i ? def.Arguments[i] : def.ParamsArgument;
            if (argumentDef is null)
            {
                Diagnostics.Report("InvalidArgumentCount", $"Function '{p.FunctionName}' requires at least {countOfRequired} arguments", p.Span);
            }
            else
            {
                if (argumentDef.RequiredType is not null)
                {
                    if (!p.Arguments[i].GetType().IsAssignableTo(argumentDef.RequiredType))
                    {
                        Diagnostics.Report("InvalidArgumentType", $"Function '{p.FunctionName}' argument '{argumentDef.Name}' requires type '{argumentDef.RequiredType.Name}'", p.Arguments[i].Span);
                    }
                }

                if (argumentDef.BinderHandling == BinderHandling.Skip)
                {
                    //skip binding
                    continue;
                }
            }

            Bind(p.Arguments[i]);
        }
    }
    private void Bind(SelectProjection p)
    {
        BindChildren(p);
        if (!string.IsNullOrEmpty(p.Alias))
        {
            Current.AddColumn(p.Alias);
        }
    }

    private void Bind(IdentifierExpression p)
    {
        string columName = p.Column;
        string? tableName = null;
        if (p.IdentifierParts.Count == 2)
        {
            tableName = p.IdentifierParts[0];
            var (table, col) = Current.FindColumn(tableName, columName);
            if (table is null || col is null)
            {
                if (table is null)
                {
                    Diagnostics.Report("TableNotFound", $"Table '{tableName}' not found in the current scope", p.Span);
                }
                else
                {
                    Diagnostics.Report("ColumnNotFound", $"Column '{columName}' not found in table '{table.Name}'", p.Span);
                }
                return;
            }

            p.Binding = new ColumnBinding(table, col);
            return;
        }
        else if (p.IdentifierParts.Count > 2)
        {
            Diagnostics.Report("ColumnNotFound", "Only simple '[col]' or 'table.[col]' identifiers are supported for now", p.Span);
            return;
        }

        // need to handle aliased columns too??

        var cols = Current.FindColumnsInScope(columName).ToList();
        if (cols.Count == 0)
        {
            Diagnostics.Report("ColumnNotFound", $"Column '{columName}' not found in the current scope", p.Span);
            return;
        }
        else if (cols.Count > 1)
        {
            Diagnostics.Report("AmbiguousColumn", $"Column '{columName}' is ambiguous in the current scope", p.Span);
            return;
        }
        var (t, c) = cols[0];
        p.Binding = new ColumnBinding(t, c);
        return;
    }

    private void Bind(StarIdentifierExpression p)
    {
        if (p.IdentifierParts.Count == 0)
        {
            // all columns for all reference tables in scope
            var refs = Current.ReferencedTables;
            if (!refs.Any())
            {
                Diagnostics.Report("NoTablesInScope", "No tables in scope to expand '*'", p.Span);
                return;
            }

            List<ColumnBinding> bindings = new List<ColumnBinding>();
            foreach (var table in refs)
            {
                foreach (var column in table.Columns)
                {
                    bindings.Add(new ColumnBinding(table, column));
                }
            }
            p.Bindings = bindings;
        }
        else if (p.IdentifierParts.Count == 1)
        {
            // all columns for a specific table in scope
            var tableName = p.IdentifierParts[0];
            var table = Current.LookupReferenceTables(tableName);
            if (table is null)
            {
                Diagnostics.Report("TableNotFound", $"Table '{tableName}' not found in the current scope", p.Span);
                return;
            }
            List<ColumnBinding> bindings = new List<ColumnBinding>();
            foreach (var column in table.Columns)
            {
                bindings.Add(new ColumnBinding(table, column));
            }
            p.Bindings = bindings;
        }
        else
        {
            Diagnostics.Report("UnsupportedStarIdentifier", "Only simple '*' or 'table.*' identifiers are supported for now", p.Span);
        }
    }
    private void Bind(ParameterIdentifier p)
    {
        var symbol = Current.LookupParameter(p.ParamterName);

        if (symbol is null)
        {
            Diagnostics.Report("ParameterNotFound", $"Parameter '@{p.ParamterName}' not provided", p.Span);
        }
        else
        {
            p.Binding = new ParameterBinding(symbol);
        }
    }

    private void Bind(SelectTableReference tbl)
    {
        var previous = Current;
        Current = new BindingScope(string.Empty, previous);
        try
        {
            Bind(tbl.Select);
        }
        finally
        {
            var boundTable = Current.Build();
            if (boundTable is not null)
            {
                previous.AddTableReference(boundTable, tbl.Alias); // ?? this might be wrong and we need to add to a scoped catalog!
            }
            Current = previous;
        }
    }

    private void Bind(NamedTableReference tbl)
    {
        // we lookup the table in the current scope, and if found
        if (tbl.Identifer.IdentifierParts.Count > 1)
        {
            Diagnostics.Report("UnsupportedTableReference", "Only simple table references are supported for now", tbl.Identifer.Span);
        }

        var table = Current.LookupTablesInScope(tbl.Identifer.IdentifierParts[0]);

        if (table is null)
        {
            Diagnostics.Report("UnsupportedTableReference", $"Table '{tbl.Identifer.IdentifierParts[0]}' not found in the current scope", tbl.Identifer.Span);
            return;
        }

        tbl.Binding = table;

        Current.AddTableReference(table, tbl.Alias);
    }

    private void Bind(CommonTableExpression cte)
    => WithNewTable(cte.Alias, () =>
        {
            // we are adding the cte into the current parent scope
            // bind the common table expression
            BindChildren(cte);
        });

    private void Bind(SelectStatement selectStatement)
        => WithNewTable(string.Empty, () =>
        {
            BindChildren(selectStatement);
        });

    private void Bind(SelectExpression selectExpression)
    {
        // we need to bind the from clause first, so that we can add the table to the current scope
        // then we process the rest of the select expression, which may reference the table in the from clause
        if (selectExpression.From is not null)
        {
            foreach (var c in selectExpression.From.GetChildren())
            {
                Bind(c);
            }
        }
        // bind remaining children, from will be skipped as the case statment skips it!
        BindChildren(selectExpression);
    }

    // recursively walk the tree and bind each node  
    void BindChildren(SyntaxNode node)
    {
        foreach (var c in node.GetChildren())
        {
            Bind(c);
        }
    }
}


public class BindingScope
{
    private readonly BindingScope? _parent;
    private readonly ISchemaProvider _catalog;
    private readonly List<TableSymbol> _referencedTables = new();
    private readonly Dictionary<string, TableSymbol> _tables = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TableSymbol> _tablesInCatalog = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _tableName;
    private readonly Dictionary<string, ColumnSymbol> _columns = new(StringComparer.OrdinalIgnoreCase);

    public BindingScope(string tableName, BindingScope parent)
    {
        _tableName = tableName;
        _parent = parent;
        _catalog = parent._catalog;
    }

    public BindingScope(ISchemaProvider catalog)
    {
        _tableName = string.Empty;
        _parent = null;
        _catalog = catalog;
    }

    public TableSymbol? Build()
    {
        return new TableSymbol(_tableName ?? string.Empty, _columns.Values.ToList(), true);
    }

    public void AddColumn(string columnName)
    {
        _columns[columnName] = new ColumnSymbol(columnName, string.Empty, true);
    }

    public void AddTableToCatalog(TableSymbol table)
    {
        _tablesInCatalog[table.Name] = table;
    }

    public void AddTableReference(TableSymbol table, string? alias = null)
    {
        if (!string.IsNullOrEmpty(alias))
        {
            table = new TableSymbol(alias, table.Columns, table.IsLocal);
            _tables[alias] = table;
        }

        _tables[table.Name] = table;

        _referencedTables.Add(table);
    }

    public IEnumerable<TableSymbol> ReferencedTables => _referencedTables;

    public TableSymbol? LookupReferenceTables(string name)
    {
        if (_tables.TryGetValue(name, out var table))
        {
            return table;
        }

        return null;
    }

    public TableSymbol? LookupTablesInScope(string name)
    {
        // for handling recursive ctes!
        if (name.Equals(_tableName, StringComparison.OrdinalIgnoreCase))
            return Build();
        if (_tablesInCatalog.TryGetValue(name, out var table))
        {
            return table;
        }

        var t = _parent?.LookupTablesInScope(name);
        if (t is not null)
        {
            return t;
        }
        return _catalog?.GetTable(name);
    }

    public ParameterSymbol? LookupParameter(string name)
    {
        // lookup from parent for when we want to support DECLARE statements!
        var t = _parent?.LookupParameter(name) ?? _catalog.GetParameter(name);
        return t;
    }

    internal (TableSymbol? table, ColumnSymbol? col) FindColumn(string tableName, string colName)
    {
        var table = LookupReferenceTables(tableName);
        if (table is not null)
        {
            var col = table.Columns.FirstOrDefault(c => c.Name.Equals(colName, StringComparison.OrdinalIgnoreCase));
            if (col is not null)
            {
                return (table, col);
            }
            return (table, null);
        }

        return (null, null);
    }

    // are we in an order by, group by, having scope?
    public bool CanBindToProjections { get; set; } = false;
    internal IEnumerable<(TableSymbol? table, ColumnSymbol? col)> FindColumnsInScope(string colName)
    {
        if (CanBindToProjections)
        {
            if (_columns.TryGetValue(colName, out var col))
            {
                yield return (Build(), col);
                yield break;
            }
        }
        // we can look for the column in all referenced tables, and if found, return it
        foreach (var table in _referencedTables)
        {
            var col = table.Columns.FirstOrDefault(c => c.Name.Equals(colName, StringComparison.OrdinalIgnoreCase));
            if (col is not null)
            {
                yield return (table, col);
            }
        }
    }
}

public class ParameterBinding
{
    public ParameterBinding(ParameterSymbol parameter)
    {
        ParameterSymbol = parameter;
    }

    public ParameterSymbol ParameterSymbol { get; }
}

public class ColumnBinding
{
    public ColumnBinding(TableSymbol tableSymbol, ColumnSymbol column)
    {
        TableSymbol = tableSymbol;
        ColumnSymbol = column;
    }

    public TableSymbol TableSymbol { get; }
    public ColumnSymbol ColumnSymbol { get; }
}
