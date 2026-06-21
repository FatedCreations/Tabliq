using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Tabliq.Sql.Binding;
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

    public async Task<ExecutionResult> ExecuteAsync(string sql, IEnumerable<ExecuterParameter> paramaters)
    {
        var rewittenSql = GenerateSql(sql, paramaters);

        var normalisedParamaters = paramaters.GroupBy(x => x.Name).ToDictionary(p => p.Key, p => p.Last().Value);

        return await Executer.ExecuteAsync(rewittenSql, normalisedParamaters);
    }

    internal string GenerateSql(string sql, IEnumerable<ExecuterParameter> paramaters)
    {
        var parsed = Sql.Parsing.Parser.Parse(sql);
        var ctx = new ExecutionContext(this, paramaters);
        parsed = Binder.Bind(parsed, ctx);
        parsed = SkippingReplaceStarsRewriter.Instance.Rewrite(parsed);

        parsed = RemoteSqlRewiter.Instance.Rewrite(parsed);

        if (parsed.Diagnostics.Any())
        {
            throw new Exception($"Invalid SQL: {sql}. Diagnostics: {string.Join(", ", parsed.Diagnostics.Select(d => d.Message))}");
        }

        return SqlWriter.ToSql(parsed.Script);
    }

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
    public IReadOnlyList<string> Columns { get; } = new List<string>();
    public IReadOnlyList<object?[]> Data { get; } = new List<object?[]>();
}