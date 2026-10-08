namespace Tabliq.Sql;

internal class ParserLimitException : Exception
{
    public ParserLimitException(string message) : base(message)
    {
    }
}