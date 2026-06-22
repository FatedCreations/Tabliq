using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Tabliq.RemoteExecuter.MsSql;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;
using Tabliq.Sql.Diagnostics;
using Tabliq.Sql.Printer;

namespace Tabliq.RemoteExecuter;

public class RemoteSqlExecuter
{
    public RemoteSqlExecuter(VirtualSchema schema, IDatabaseExecuter executer)
    {
        Schema = schema;
        Executer = executer;
    }

    public IDatabaseExecuter Executer { get; }

    public VirtualSchema Schema { get; }

    public async Task<ExecutionResult> ExecuteAsync(string sql, IEnumerable<ExecuterParameter> paramaters, CancellationToken cancellationToken)
    {
        var rewittenSql = Parse(sql, paramaters);

        var normalisedParamaters = paramaters.GroupBy(x => x.Name).ToDictionary(p => p.Key, p => p.Last().Value);

        return await Executer.ExecuteAsync(rewittenSql, normalisedParamaters, cancellationToken);
    }

    internal SqlScript Parse(string sql, IEnumerable<ExecuterParameter> paramaters)
    {
        var parsed = Sql.Parsing.Parser.Parse(sql);
        parsed.ThrowIfInvalid();

        var ctx = new ExecutionContext(this, paramaters);
        parsed = Binder.Bind(parsed, ctx);
        parsed = SkippingReplaceStarsRewriter.Instance.Execute(parsed);

        parsed = RemoteSqlRewiter.Instance.Execute(parsed);

        parsed.ThrowIfInvalid();

        return parsed.Script;
    }

    public void Validate(string sql, IEnumerable<ExecuterParameter> paramaters)
        => Parse(sql, paramaters);

    private class ExecutionContext : ISchemaProvider
    {
        private RemoteSqlExecuter _context;
        private readonly IReadOnlyList<ExecuterParameter> _paramaters;

        public ExecutionContext(RemoteSqlExecuter context, IEnumerable<ExecuterParameter> paramaters)
        {
            _context = context;
            _paramaters = paramaters.ToList();
        }
        public FunctionSymbol? GetFunction(string name) => null;
        public ParameterSymbol? GetParameter(string name) => _paramaters.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.AsSymbol();
        public TableSymbol? GetTable(string name) => _context.Schema.Tables.FirstOrDefault(x => x.TableName.Equals(name, StringComparison.OrdinalIgnoreCase))?.AsSymbol();
    }
}

public class ExecutionResult
{
    public ExecutionResult() { }
    public List<string> Columns { get; } = new List<string>();
    public List<object?[]> Data { get; } = new List<object?[]>();
}