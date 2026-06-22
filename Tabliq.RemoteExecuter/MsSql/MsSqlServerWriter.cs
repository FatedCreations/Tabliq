using Tabliq.Sql.Printer;

namespace Tabliq.RemoteExecuter.MsSql;

internal class MsSqlServerWriter : SqlWriter
{
    private static readonly string[] QuotedIdentifiers = [
        "OPENROWSET",
        "OPEN",
        "DATE",
    ];

    protected override bool RequiresQuotedIdentifier(string name)
    {
        return base.RequiresQuotedIdentifier(name) || (QuotedIdentifiers.Contains(name, StringComparer.OrdinalIgnoreCase));
    }

}
