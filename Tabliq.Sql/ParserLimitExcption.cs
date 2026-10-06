namespace Tabliq.Sql;

internal class ParserLimitExcption : Exception
{
    public ParserLimitExcption(string message) : base(message)
    {
    }
}