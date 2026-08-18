using System;
using OpenLearning.Web.Pages.Admin.Reports;
using Xunit;

namespace OpenLearning.UnitTests.Csv;

public sealed class CsvHelperTests
{
    private static readonly string[] _twoColumnHeaders = { "Col1", "Col2" };

    private static readonly string[] _singleColumnHeaders = { "Id" };
    [Fact]
    public void Escape_returns_empty_string_for_null()
    {
        Assert.Equal(string.Empty, CsvHelper.Escape(null));
    }

    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("has space", "has space")]
    [InlineData("un,quoted", "\"un,quoted\"")]
    [InlineData("has\"quote", "\"has\"\"quote\"")]
    [InlineData("line\nbreak", "\"line\nbreak\"")]
    [InlineData("a\r\nb", "\"a\r\nb\"")]
    public void Escape_quotes_cells_with_separators_quotes_or_newlines(string input, string expected)
    {
        Assert.Equal(expected, CsvHelper.Escape(input));
    }

    [Theory]
    [InlineData("=SUM(A1)", "'=SUM(A1)")]
    [InlineData("+cmd", "'+cmd")]
    [InlineData("-1", "'-1")]
    [InlineData("@import", "'@import")]
    [InlineData("\tstart", "'\tstart")]
    [InlineData("\rcarriage", "\"'\rcarriage\"")]
    public void Escape_neutralizes_spreadsheet_formula_injection(string input, string expected)
    {
        Assert.Equal(expected, CsvHelper.Escape(input));
    }

    [Fact]
    public void Escape_does_not_prefix_formula_characters_mid_string()
    {
        Assert.Equal("a=b", CsvHelper.Escape("a=b"));
    }

    [Fact]
    public void Build_writes_header_then_rows_with_escaping()
    {
        var rows = new[]
        {
            new string?[] { "a", "b,c" },
            new string?[] { "=SUM(A1)", "d" },
        };
        var csv = CsvHelper.Build(_twoColumnHeaders, rows);

        var lines = csv.TrimEnd('\r', '\n').Split('\n');
        Assert.Equal("Col1,Col2", lines[0]);
        Assert.Equal("a,\"b,c\"", lines[1]);
        Assert.Equal("'=SUM(A1),d", lines[2]);
    }

    [Fact]
    public void Build_with_no_rows_returns_only_the_header()
    {
        var csv = CsvHelper.Build(_singleColumnHeaders, Array.Empty<string[]>());
        Assert.Equal("Id" + Environment.NewLine, csv);
    }
}
