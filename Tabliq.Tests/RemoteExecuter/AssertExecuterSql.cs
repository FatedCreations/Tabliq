using Tabliq.RemoteExecuter;
using Tabliq.RemoteExecuter.MsSql;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Core;
using Tabliq.Sql.Printer;

namespace Tabliq.Tests.RemoteExecuter;

public class AssertExecuterSql
{
    public static Asserter WithSchema(VirtualSchema provider)
        => new Asserter(provider);
    public static Asserter WithParameters(IEnumerable<string> parameters)
        => new Asserter().WithParameters(parameters.Select(x => new ExecuterParameter(x, null)));
    public static Asserter WithParameters(params string[] parameters)
        => new Asserter().WithParameters(parameters.Select(x => new ExecuterParameter(x, null)));


    public static void Equal(string underTest, string expected)
        => new Asserter().Equal(underTest, expected);
    public static void Errors(string underTest, params string[] expectedDiagnostics)
        => new Asserter().Errors(underTest, expectedDiagnostics);

    //public static void WithErrors(string underTest, List<string>? errors = null)
    //    => new Asserter().WithErrors(underTest, errors ?? []);

    public class Asserter
    {
        private readonly VirtualSchema _databaseSchema;
        private IEnumerable<ExecuterParameter> _parameters;

        public Asserter(VirtualSchema? schema = null, IEnumerable<ExecuterParameter> parameters = null)
        {
            _parameters = parameters ?? [];
            _databaseSchema = schema ?? TestConfigSchema.SchemaFriendlyNamesSchema;
        }

        internal Asserter WithSchema(VirtualSchema provider)
            => new Asserter(provider);

        internal Asserter WithParameters(params ExecuterParameter[] parameters)
            => WithParameters((IEnumerable<ExecuterParameter>)parameters);

        internal Asserter WithParameters(IEnumerable<ExecuterParameter> parameters)
        {
            return new Asserter(_databaseSchema, parameters);
        }

        //public void WithErrors(string underTest, List<string> errors)
        //{
        //    try
        //    {
        //        Task.Run(() =>
        //        {
        //            var tree = Parser.Parse(underTest);
        //            var boundTree = Binder.Bind(tree, _databaseSchema);

        //            foreach (var rewiter in _rewriters)
        //            {
        //                boundTree = rewiter.Rewrite(boundTree);
        //            }

        //            Assert.NotEmpty(boundTree.Diagnostics);

        //            Assert.All(errors, expectedError =>
        //            {
        //                Assert.Contains(boundTree.Diagnostics, d => d.Message == expectedError);
        //            });
        //        }, new CancellationTokenSource(100).Token).GetAwaiter().GetResult();
        //    }
        //    catch (TaskCanceledException)
        //    {
        //        Assert.Fail("The test timed out. This may indicate an infinite loop or a long-running operation in the parser or binder.");
        //    }
        //}

        private IEnumerable<SyntaxNode> GetAllNodes(SyntaxNode syntaxNode)
        {
            yield return syntaxNode;
            foreach (var child in syntaxNode.GetChildren())
            {
                foreach (var descendant in GetAllNodes(child))
                {
                    yield return descendant;
                }
            }
        }

        private class FakeDataExecuter : IDatabaseExecuter
        {
            public List<(SqlScript Sql, IDictionary<string, object> Parameters, CancellationToken cancellationToken)> ExecutedCommands { get; } = new();
            public Task<ExecutionResult> ExecuteAsync(SqlScript sql, IDictionary<string, object> paramaters, CancellationToken cancellationToken)
            {
                var rewitten = Tabliq.RemoteExecuter.MsSql.RewriteForMsSqlServer.Instance.Execute(sql);
                rewitten.ThrowIfInvalid();

                ExecutedCommands.Add((rewitten.Script, paramaters, cancellationToken));
                return Task.FromResult(new ExecutionResult());
            }
        }
        public void Equal(string underTest, string expected)
        {
            try
            {
                Task.Run(() =>
                {
                    var fakeDataExecuter = new FakeDataExecuter();
                    var ex = new RemoteSqlExecuter(_databaseSchema, fakeDataExecuter);

                    _ = ex.ExecuteAsync(underTest, _parameters, default).GetAwaiter().GetResult();

                    var cmd = Assert.Single(fakeDataExecuter.ExecutedCommands);

                    // run in the mssql generator to ensure that the generated SQL is valid and can be parsed
                    var cmdSql = MsSqlDatabaseExecuter.GenerateSqlQuery(cmd.Sql);

                    // Normalize line endings to avoid platform-specific differences
                    var canon = cmdSql.Replace("\r\n", "\n").Trim();
                    expected = expected.Replace("\r\n", "\n").Trim();

                    // Do not parse the expected SQL -- tests assert the raw expected formatting
                    Console.WriteLine("--- ASSERTSQL DIAGNOSTIC START ---");
                    Console.WriteLine("EXPECTED (raw):");
                    Console.WriteLine(string.Empty);
                    Console.WriteLine(expected);
                    Console.WriteLine("-----");
                    Console.WriteLine("REWITTEN:");
                    Console.WriteLine(string.Empty);
                    Console.WriteLine(canon);
                    Console.WriteLine("--- ASSERTSQL DIAGNOSTIC END ---");

                    Assert.Equal(expected, canon);

                }, new CancellationTokenSource(100).Token).GetAwaiter().GetResult();
            }
            catch (TaskCanceledException)
            {
                Assert.Fail("The test timed out. This may indicate an infinite loop or a long-running operation in the parser or binder.");
            }
        }
        public void Errors(string underTest, params string[] expectedDiagnostics)
        {
            var ex = Assert.Throws<CompilationDiagnosticsException>(() =>
           {
               try
               {
                   Task.Run(() =>
                   {
                       var fakeDataExecuter = new FakeDataExecuter();
                       var ex = new RemoteSqlExecuter(_databaseSchema, fakeDataExecuter);

                       _ = ex.ExecuteAsync(underTest, _parameters, default).GetAwaiter().GetResult();
                   }, new CancellationTokenSource(100).Token).GetAwaiter().GetResult();
               }
               catch (TaskCanceledException)
               {
                   Assert.Fail("The test timed out. This may indicate an infinite loop or a long-running operation in the parser or binder.");
               }
           });

            var messages = ex.Diagnostics.Select(x => $"{x.Id}: [{x.Start}:{x.Length}] : {x.Message}");

            Console.WriteLine("--- ASSERTSQL DIAGNOSTIC START ---");
            Console.WriteLine("EXPECTED (messages):");
            Console.WriteLine(string.Empty);
            Console.WriteLine(string.Join("\n", expectedDiagnostics));
            Console.WriteLine("-----");
            Console.WriteLine("ACTUAL:");
            Console.WriteLine(string.Empty);
            Console.WriteLine(string.Join("\n", messages));
            Console.WriteLine("--- ASSERTSQL DIAGNOSTIC END ---");

            Assert.Equal(expectedDiagnostics, messages);
        }
    }
}