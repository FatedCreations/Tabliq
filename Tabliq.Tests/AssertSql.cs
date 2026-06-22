using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;
using Tabliq.Sql.Core;
using Tabliq.Sql.Parsing;
using Tabliq.Sql.Printer;
using Tabliq.Sql.Rewriter;
using Xunit;

public class AssertSql
{
    public static Asserter WithSchema(ISchemaProvider provider)
        => new Asserter(provider);
    public static Asserter WithParameters(IEnumerable<string> parameters)
        => new Asserter().WithParameters(parameters);
    public static Asserter WithParameters(params string[] parameters)
        => new Asserter().WithParameters(parameters);

    public static Asserter WithRewriter(SqlRewiter sqlRewiter)
        => new Asserter().WithRewriter(sqlRewiter);

    public static Asserter WithRewriter<T>(params object[] args) where T : SqlRewiter
        => new Asserter().WithRewriter<T>(args);

    public static void Equal(string underTest, string expected)
        => new Asserter().Equal(underTest, expected);

    public static void Equal(string underTest, SyntaxNode expectedAst)
        => new Asserter().Equal(underTest, expectedAst);

    public static void WithErrors(string underTest, List<string>? errors = null)
        => new Asserter().WithErrors(underTest, errors ?? []);

    internal static Asserter SkipBinder(bool skip = true)
        => new Asserter().SkipBinder(skip);

    public class Asserter : IServiceProvider
    {
        private readonly ISchemaProvider _databaseSchema;
        private IEnumerable<SqlRewiter> _rewriters;

        public bool RunBinder { get; private set; } = true;

        public Asserter(ISchemaProvider? schema = null, IEnumerable<SqlRewiter> rewriters = null)
        {
            _rewriters = rewriters ?? [];
            _databaseSchema = schema ?? TestSchema.DatabaseSchema;
        }

        internal Asserter WithSchema(ISchemaProvider provider)
            => new Asserter(new CombineSchema(provider, this._databaseSchema));

        internal Asserter SkipBinder(bool skip = true)
            => new Asserter(this._databaseSchema, this._rewriters)
            {
                RunBinder = !skip,
            };

        internal Asserter WithParameters(params string[] parameters)
            => WithParameters((IEnumerable<string>)parameters);

        internal Asserter WithRewriter<T>(T sqlRewiter) where T : SqlRewiter
        {
            var rewriter = _rewriters.Append(sqlRewiter);
            return new Asserter(_databaseSchema, rewriter);
        }

        internal Asserter WithRewriter<T>(params object[] args) where T : SqlRewiter
        {
            var sqlRewiter = ActivatorUtilities.CreateInstance<T>(this, [.. args]);

            return WithRewriter(sqlRewiter);
        }
        object IServiceProvider.GetService(Type serviceType)
        {
            if (serviceType == typeof(ISchemaProvider))
            {
                return _databaseSchema;
            }
            return null!;
        }

        internal Asserter WithParameters(IEnumerable<string> parameters)
        {
            var s = new TestSchema
            {
                Parameters = parameters.ToList()
            };
            return new Asserter(new CombineSchema(s, this._databaseSchema));
        }

        public void WithErrors(string underTest, List<string> errors)
        {
            try
            {
                Task.Run(() =>
                {
                    var tree = Parser.Parse(underTest);
                    var boundTree = Binder.Bind(tree, _databaseSchema);

                    foreach (var rewiter in _rewriters)
                    {
                        boundTree = rewiter.Execute(boundTree);
                    }

                    Assert.NotEmpty(boundTree.Diagnostics);

                    Assert.All(errors, expectedError =>
                    {
                        Assert.Contains(boundTree.Diagnostics, d => d.Message == expectedError);
                    });
                }, new CancellationTokenSource(100).Token).GetAwaiter().GetResult();
            }
            catch (TaskCanceledException)
            {
                Assert.Fail("The test timed out. This may indicate an infinite loop or a long-running operation in the parser or binder.");
            }
        }

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
        public void Equal(string underTest, string expected)
        {
            try
            {
                Task.Run(() =>
                {
                    var tree = Parser.Parse(underTest);

                    if (RunBinder)
                    {
                        tree = Binder.Bind(tree, _databaseSchema);
                    }

                    Assert.NotNull(tree.Script);

                    foreach (var rewiter in _rewriters)
                    {
                        tree = rewiter.Execute(tree);
                    }

                    Assert.Empty(tree.Diagnostics);

                    var canon = SqlWriter.ToSql(tree.Script);

                    // Normalize line endings to avoid platform-specific differences
                    canon = canon.Replace("\r\n", "\n").Trim();
                    expected = expected.Replace("\r\n", "\n").Trim();

                    // Do not parse the expected SQL -- tests assert the raw expected formatting
                    Console.WriteLine("--- ASSERTSQL DIAGNOSTIC START ---");
                    Console.WriteLine("EXPECTED (raw):");
                    Console.WriteLine(string.Empty);
                    Console.WriteLine(expected);
                    Console.WriteLine("-----");
                    Console.WriteLine("CANONICAL:");
                    Console.WriteLine(string.Empty);
                    Console.WriteLine(canon);
                    Console.WriteLine("--- ASSERTSQL DIAGNOSTIC END ---");

                    Assert.Equal(expected, canon);

                    // only check for spans etc 
                    Assert.All(GetAllNodes(tree.Script), node =>
                    {
                        Assert.True(node.Span.StartToken is not null, $"Location not set");
                    });


                    DeepCloneRewiter.AssertDeepClone(tree.Script);

                }, new CancellationTokenSource(100).Token).GetAwaiter().GetResult();
            }
            catch (TaskCanceledException)
            {
                Assert.Fail("The test timed out. This may indicate an infinite loop or a long-running operation in the parser or binder.");
            }
        }

        public void Equal(string underTest, SyntaxNode expectedAst)
        {
            try
            {
                Task.Run(() =>
                {
                    var tree = Parser.Parse(underTest);
                    if (RunBinder)
                    {
                        tree = Binder.Bind(tree, _databaseSchema);
                    }

                    Assert.NotNull(tree.Script);

                    foreach (var rewiter in _rewriters)
                    {
                        tree = rewiter.Execute(tree);
                    }

                    Assert.Empty(tree.Diagnostics);

                    var canon = SqlWriter.ToSql(tree.Script);

                    // Normalize line endings to avoid platform-specific differences
                    canon = canon.Replace("\r\n", "\n").Trim();
                    var expected = SqlWriter.ToSql(expectedAst).Replace("\r\n", "\n").Trim();

                    // Do not parse the expected SQL -- tests assert the raw expected formatting
                    Console.WriteLine("--- ASSERTSQL DIAGNOSTIC START ---");
                    Console.WriteLine("EXPECTED (raw):");
                    Console.WriteLine(string.Empty);
                    Console.WriteLine(expected);
                    Console.WriteLine("-----");
                    Console.WriteLine("CANONICAL:");
                    Console.WriteLine(string.Empty);
                    Console.WriteLine(canon);
                    Console.WriteLine("--- ASSERTSQL DIAGNOSTIC END ---");

                    Assert.Equal(expected, canon);

                    Assert.Equal(tree.Script, expectedAst);

                    DeepCloneRewiter.AssertDeepClone(tree.Script);
                }, new CancellationTokenSource(100).Token).GetAwaiter().GetResult();
            }
            catch (TaskCanceledException)
            {
                Assert.Fail("The test timed out. This may indicate an infinite loop or a long-running operation in the parser or binder.");
            }
        }
    }
}


public class DeepCloneRewiter : SqlRewiter
{
    public static void AssertDeepClone(SyntaxNode node)
    {
        var clone = new DeepCloneRewiter().Rewrite(node);

        Assert.Equal(node, clone);
        var anyHaveRef = GetReferenceEqualNodes(GetAllNodes(node), GetAllNodes(clone));
        Assert.Empty(anyHaveRef);
        static IEnumerable<SyntaxNode> GetAllNodes(SyntaxNode syntaxNode)
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
        static IEnumerable<SyntaxNode> GetReferenceEqualNodes(IEnumerable<SyntaxNode> first, IEnumerable<SyntaxNode> second)
        {
            var other = second.ToList();
            var toList = first.ToList();

            foreach (var f in toList)
            {
                foreach (var child in other)
                {
                    if (ReferenceEquals(f, child))
                    {
                        yield return f;
                    }
                }
            }
        }
    }
    protected override ParameterIdentifier Rewrite(ParameterIdentifier node)
        => new ParameterIdentifier(node.ParamterName);

    protected override StarIdentifierExpression Rewrite(StarIdentifierExpression node)
        => new StarIdentifierExpression([.. node.IdentifierParts]);

    protected override LiteralExpression Rewrite(LiteralExpression node)
        => new LiteralExpression(node.Value);

    protected override IdentifierExpression Rewrite(IdentifierExpression node)
        => new IdentifierExpression(new List<string>(node.IdentifierParts));
}