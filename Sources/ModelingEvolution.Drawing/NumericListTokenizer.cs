using System.Globalization;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Helper class for parsing serialized data structures from the System.Windows namespace.
/// </summary>
internal class NumericListTokenizer
{
    private readonly string _str;
    private readonly char _separator;
    private int _position;

    private enum Symbol
    {
        Token,
        Separator,
        Whitspace,
        EndOfLine
    }

    public NumericListTokenizer(string str, IFormatProvider formatProvider)
    {
        _str = str ?? throw new ArgumentNullException(nameof(str));
        _separator = GetSeparator(formatProvider ?? throw new ArgumentNullException(nameof(formatProvider)));
    }

    /// <summary>
    /// Gets the appropriate separator character based on the format provider's decimal separator.
    /// </summary>
    /// <param name="formatProvider">The format provider to examine.</param>
    /// <returns>The separator character (',' if decimal separator is not ',', otherwise ';').</returns>
    public static char GetSeparator(IFormatProvider formatProvider)
    {
        // By convention, string representations of target classes always use ';' as a separator
        // if the decimal number separator is ','. Otherwise, the separator is ','.
        return NumberFormatInfo.GetInstance(formatProvider).NumberDecimalSeparator != "," ? ',' : ';';
    }

    private Symbol GetCurrentSymbol()
    {
        if (_position >= _str.Length)
            return Symbol.EndOfLine;
        if (_str[_position] == _separator)
            return Symbol.Separator;
        if (char.IsWhiteSpace(_str, _position))
            return Symbol.Whitspace;
        return Symbol.Token;
    }

    private void SkipAllWhitespaces()
    {
        while (GetCurrentSymbol() == Symbol.Whitspace)
        {
            _position++;
        }
    }

    private void SkipNextDelimeter()
    {
        SkipAllWhitespaces();
        switch (GetCurrentSymbol())
        {
            case Symbol.Token:
                return;
            case Symbol.Separator:
                _position++;
                SkipAllWhitespaces();
                return;
            default:
                throw new InvalidOperationException("Separator not found");
        }
    }

    public bool HasNoMoreTokens()
    {
        SkipAllWhitespaces();
        return GetCurrentSymbol() == Symbol.EndOfLine;
    }

    public string GetNextToken()
    {
        var length = 0;
        if (_position == 0)
        {
            SkipAllWhitespaces();
        }
        else
        {
            SkipNextDelimeter();
        }

        while (GetCurrentSymbol() == Symbol.Token)
        {
            _position++;
            length++;
        }

        if (length == 0)
        {
            throw new InvalidOperationException("Next token not found");
        }

        return _str.Substring(_position - length, length);
    }
}