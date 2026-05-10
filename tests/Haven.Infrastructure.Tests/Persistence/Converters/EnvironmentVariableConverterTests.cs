using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Infrastructure.Persistence.Converters;
using Shouldly;
using Environment = System.Environment;

namespace Haven.Infrastructure.Tests.Persistence.Converters;

[Category("Unit")]
public sealed class EnvironmentVariableConverterTests
{
    private static readonly Guid TestParentId = Guid.NewGuid();

    #region Convert(IEnumerable<EnvironmentVariables>, bool) Tests

    [Test]
    public void Convert_WithEmptyList_ShouldReturnEmptyString()
    {
        var variables = Array.Empty<EnvironmentVariables>();

        var result = EnvironmentVariableConverter.Convert(variables);

        result.ShouldBeEmpty();
    }

    [Test]
    public void Convert_WithSingleSimpleVariable_ShouldFormatCorrectly()
    {
        var variables = new[]
        {
            new EnvironmentVariables { Key = "KEY", Value = "value", ParentId = TestParentId }
        };

        var result = EnvironmentVariableConverter.Convert(variables);

        result.ShouldBe("KEY=value");
    }

    [Test]
    public void Convert_WithMultipleVariables_ShouldFormatAllLines()
    {
        var variables = new[]
        {
            new EnvironmentVariables { Key = "KEY1", Value = "value1", ParentId = TestParentId },
            new EnvironmentVariables { Key = "KEY2", Value = "value2", ParentId = TestParentId },
            new EnvironmentVariables { Key = "KEY3", Value = "value3", ParentId = TestParentId }
        };

        var result = EnvironmentVariableConverter.Convert(variables);

        var lines = result.Split(Environment.NewLine);
        lines.Length.ShouldBe(3);
        lines[0].ShouldBe("KEY1=value1");
        lines[1].ShouldBe("KEY2=value2");
        lines[2].ShouldBe("KEY3=value3");
    }

    [Test]
    public void Convert_WithNullValue_ShouldFormatAsEmptyQuotes()
    {
        var variables = new[]
        {
            new EnvironmentVariables { Key = "EMPTY_KEY", Value = null, ParentId = TestParentId }
        };

        var result = EnvironmentVariableConverter.Convert(variables);

        result.ShouldBe("EMPTY_KEY=\"\"");
    }

    [Test]
    public void Convert_WithEmptyStringValue_ShouldFormatAsEmptyQuotes()
    {
        var variables = new[]
        {
            new EnvironmentVariables { Key = "EMPTY_KEY", Value = string.Empty, ParentId = TestParentId }
        };

        var result = EnvironmentVariableConverter.Convert(variables);

        result.ShouldBe("EMPTY_KEY=\"\"");
    }

    [Test]
    public void Convert_WithValueContainingSpaces_ShouldQuoteValue()
    {
        var variables = new[]
        {
            new EnvironmentVariables { Key = "KEY", Value = "value with spaces", ParentId = TestParentId }
        };

        var result = EnvironmentVariableConverter.Convert(variables);

        result.ShouldBe("KEY=\"value with spaces\"");
    }

    [Test]
    public void Convert_WithValueContainingQuotes_ShouldEscapeAndQuote()
    {
        var variables = new[]
        {
            new EnvironmentVariables { Key = "KEY", Value = "value with \"quotes\"", ParentId = TestParentId }
        };

        var result = EnvironmentVariableConverter.Convert(variables);

        result.ShouldBe("KEY=\"value with \\\"quotes\\\"\"");
    }

    [Test]
    public void Convert_WithValueContainingEquals_ShouldQuoteValue()
    {
        var variables = new[]
        {
            new EnvironmentVariables { Key = "KEY", Value = "value=with=equals", ParentId = TestParentId }
        };

        var result = EnvironmentVariableConverter.Convert(variables);

        result.ShouldBe("KEY=\"value=with=equals\"");
    }

    [Test]
    public void Convert_WithValueContainingHash_ShouldQuoteValue()
    {
        var variables = new[]
        {
            new EnvironmentVariables { Key = "KEY", Value = "value#with#hash", ParentId = TestParentId }
        };

        var result = EnvironmentVariableConverter.Convert(variables);

        result.ShouldBe("KEY=\"value#with#hash\"");
    }

    [Test]
    public void Convert_WithValueContainingBackslash_ShouldQuoteValue()
    {
        var variables = new[]
        {
            new EnvironmentVariables { Key = "KEY", Value = @"value\with\backslash", ParentId = TestParentId }
        };

        var result = EnvironmentVariableConverter.Convert(variables);

        result.ShouldBe("KEY=\"value\\with\\backslash\"");
    }

    [Test]
    public void Convert_WithIncludeValuesFalse_ShouldOmitValues()
    {
        var variables = new[]
        {
            new EnvironmentVariables { Key = "KEY1", Value = "value1", ParentId = TestParentId },
            new EnvironmentVariables { Key = "KEY2", Value = "value2", ParentId = TestParentId }
        };

        var result = EnvironmentVariableConverter.Convert(variables, includeValues: false);

        var lines = result.Split(Environment.NewLine);
        lines.Length.ShouldBe(2);
        lines[0].ShouldBe("KEY1=");
        lines[1].ShouldBe("KEY2=");
    }

    [Test]
    public void Convert_WithEmptyKeyVariable_ShouldSkipVariable()
    {
        var variables = new[]
        {
            new EnvironmentVariables { Key = "VALID_KEY", Value = "value1", ParentId = TestParentId },
            new EnvironmentVariables { Key = string.Empty, Value = "value2", ParentId = TestParentId },
            new EnvironmentVariables { Key = "ANOTHER_KEY", Value = "value3", ParentId = TestParentId }
        };

        var result = EnvironmentVariableConverter.Convert(variables);

        var lines = result.Split(Environment.NewLine);
        lines.Length.ShouldBe(2);
        lines[0].ShouldBe("VALID_KEY=value1");
        lines[1].ShouldBe("ANOTHER_KEY=value3");
    }

    [Test]
    public void Convert_WithComplexValues_ShouldHandleCorrectly()
    {
        var variables = new[]
        {
            new EnvironmentVariables { Key = "DATABASE_URL", Value = "postgresql://user:pass@localhost:5432/db?timeout=30", ParentId = TestParentId },
            new EnvironmentVariables { Key = "JSON_CONFIG", Value = "{\"key\": \"value\"}", ParentId = TestParentId }
        };

        var result = EnvironmentVariableConverter.Convert(variables);

        var lines = result.Split(Environment.NewLine);
        lines.Length.ShouldBe(2);
        lines[0].ShouldContain("DATABASE_URL=");
        lines[1].ShouldContain("JSON_CONFIG=");
    }

    #endregion

    #region Convert(string, Guid, EnvironmentVariableParentType) Tests

    [Test]
    public void Convert_WithEmptyString_ShouldReturnEmptyList()
    {
        var result = EnvironmentVariableConverter.Convert(string.Empty, TestParentId, EnvironmentVariableParentType.Project);

        result.ShouldBeEmpty();
    }

    [Test]
    public void Convert_WithSimpleKeyValuePair_ShouldParseCorrectly()
    {
        var envContent = "KEY=value";

        var result = EnvironmentVariableConverter.Convert(envContent, TestParentId, EnvironmentVariableParentType.Project);

        result.Count.ShouldBe(1);
        result[0].Key.ShouldBe("KEY");
        result[0].Value.ShouldBe("value");
        result[0].ParentId.ShouldBe(TestParentId);
        result[0].ParentType.ShouldBe(EnvironmentVariableParentType.Project);
    }

    [Test]
    public void Convert_WithMultipleKeyValuePairs_ShouldParseAll()
    {
        var envContent = "KEY1=value1\nKEY2=value2\nKEY3=value3";

        var result = EnvironmentVariableConverter.Convert(envContent, TestParentId, EnvironmentVariableParentType.Environment);

        result.Count.ShouldBe(3);
        result[0].Key.ShouldBe("KEY1");
        result[0].Value.ShouldBe("value1");
        result[1].Key.ShouldBe("KEY2");
        result[1].Value.ShouldBe("value2");
        result[2].Key.ShouldBe("KEY3");
        result[2].Value.ShouldBe("value3");
    }

    [Test]
    public void Convert_WithEmptyLines_ShouldSkipThem()
    {
        var envContent = "KEY1=value1\n\n\nKEY2=value2";

        var result = EnvironmentVariableConverter.Convert(envContent, TestParentId, EnvironmentVariableParentType.Service);

        result.Count.ShouldBe(2);
        result[0].Key.ShouldBe("KEY1");
        result[1].Key.ShouldBe("KEY2");
    }

    [Test]
    public void Convert_WithCommentLines_ShouldSkipThem()
    {
        var envContent = "# This is a comment\nKEY1=value1\n# Another comment\nKEY2=value2";

        var result = EnvironmentVariableConverter.Convert(envContent, TestParentId, EnvironmentVariableParentType.Project);

        result.Count.ShouldBe(2);
        result[0].Key.ShouldBe("KEY1");
        result[1].Key.ShouldBe("KEY2");
    }

    [Test]
    public void Convert_WithWhitespaceOnlyLines_ShouldSkipThem()
    {
        var envContent = "KEY1=value1\n   \n\t\nKEY2=value2";

        var result = EnvironmentVariableConverter.Convert(envContent, TestParentId, EnvironmentVariableParentType.Project);

        result.Count.ShouldBe(2);
        result[0].Key.ShouldBe("KEY1");
        result[1].Key.ShouldBe("KEY2");
    }

    [Test]
    public void Convert_WithQuotedValue_ShouldUnquoteCorrectly()
    {
        var envContent = "KEY=\"quoted value\"";

        var result = EnvironmentVariableConverter.Convert(envContent, TestParentId, EnvironmentVariableParentType.Project);

        result.Count.ShouldBe(1);
        result[0].Key.ShouldBe("KEY");
        result[0].Value.ShouldBe("quoted value");
    }

    [Test]
    public void Convert_WithQuotedValueContainingEscapedQuotes_ShouldUnescapeCorrectly()
    {
        var envContent = "KEY=\"value with \\\"escaped quotes\\\"\"";

        var result = EnvironmentVariableConverter.Convert(envContent, TestParentId, EnvironmentVariableParentType.Project);

        result.Count.ShouldBe(1);
        result[0].Value.ShouldBe("value with \"escaped quotes\"");
    }

    [Test]
    public void Convert_WithEmptyQuotedValue_ShouldParseAsEmptyString()
    {
        var envContent = "EMPTY=\"\"";

        var result = EnvironmentVariableConverter.Convert(envContent, TestParentId, EnvironmentVariableParentType.Project);

        result.Count.ShouldBe(1);
        result[0].Key.ShouldBe("EMPTY");
        result[0].Value.ShouldBe(string.Empty);
    }

    [Test]
    public void Convert_WithValueContainingEquals_ShouldParseCorrectly()
    {
        var envContent = "URL=postgresql://user:pass@localhost:5432/db?timeout=30";

        var result = EnvironmentVariableConverter.Convert(envContent, TestParentId, EnvironmentVariableParentType.Project);

        result.Count.ShouldBe(1);
        result[0].Key.ShouldBe("URL");
        result[0].Value.ShouldBe("postgresql://user:pass@localhost:5432/db?timeout=30");
    }

    [Test]
    public void Convert_WithKeyWithoutValue_ShouldParseAsEmptyValue()
    {
        var envContent = "KEY=";

        var result = EnvironmentVariableConverter.Convert(envContent, TestParentId, EnvironmentVariableParentType.Project);

        result.Count.ShouldBe(1);
        result[0].Key.ShouldBe("KEY");
        result[0].Value.ShouldBe(string.Empty);
    }

    [Test]
    public void Convert_WithKeyWithoutEqualsSign_ShouldSkipLine()
    {
        var envContent = "KEY1=value1\nINVALID_KEY\nKEY2=value2";

        var result = EnvironmentVariableConverter.Convert(envContent, TestParentId, EnvironmentVariableParentType.Project);

        result.Count.ShouldBe(2);
        result[0].Key.ShouldBe("KEY1");
        result[1].Key.ShouldBe("KEY2");
    }

    [Test]
    public void Convert_WithWindowsLineEndings_ShouldParseCorrectly()
    {
        var envContent = "KEY1=value1\r\nKEY2=value2\r\nKEY3=value3";

        var result = EnvironmentVariableConverter.Convert(envContent, TestParentId, EnvironmentVariableParentType.Project);

        result.Count.ShouldBe(3);
        result[0].Key.ShouldBe("KEY1");
        result[1].Key.ShouldBe("KEY2");
        result[2].Key.ShouldBe("KEY3");
    }

    [Test]
    public void Convert_WithMixedLineEndings_ShouldParseCorrectly()
    {
        var envContent = "KEY1=value1\nKEY2=value2\r\nKEY3=value3";

        var result = EnvironmentVariableConverter.Convert(envContent, TestParentId, EnvironmentVariableParentType.Project);

        result.Count.ShouldBe(3);
    }

    [Test]
    public void Convert_WithTrimmedKeys_ShouldPreserveTrimmedKeys()
    {
        var envContent = "  KEY1  =value1";

        var result = EnvironmentVariableConverter.Convert(envContent, TestParentId, EnvironmentVariableParentType.Project);

        result.Count.ShouldBe(1);
        result[0].Key.ShouldBe("KEY1");
    }

    [Test]
    public void Convert_WithLeadingWhitespaceInValue_ShouldPreserveAfterTrim()
    {
        var envContent = "KEY=   value";

        var result = EnvironmentVariableConverter.Convert(envContent, TestParentId, EnvironmentVariableParentType.Project);

        result.Count.ShouldBe(1);
        result[0].Value.ShouldBe("value");
    }

    [Test]
    public void Convert_WithComplexEnvFile_ShouldParseAllValidLines()
    {
        var envContent = @"# Configuration file
DATABASE_URL=postgresql://localhost/mydb
# API Configuration
API_KEY=""secret-key-123""
DEBUG=true

# Ignored line without equals
ANOTHER_KEY=""value with spaces""";

        var result = EnvironmentVariableConverter.Convert(envContent, TestParentId, EnvironmentVariableParentType.Project);

        result.Count.ShouldBe(4);
        result[0].Key.ShouldBe("DATABASE_URL");
        result[1].Key.ShouldBe("API_KEY");
        result[1].Value.ShouldBe("secret-key-123");
        result[2].Key.ShouldBe("DEBUG");
        result[3].Key.ShouldBe("ANOTHER_KEY");
        result[3].Value.ShouldBe("value with spaces");
    }

    #endregion

    #region Round-trip Tests

    [Test]
    public void RoundTrip_SimpleVariables_ShouldPreserveValues()
    {
        var original = new[]
        {
            new EnvironmentVariables { Key = "KEY1", Value = "value1", ParentId = TestParentId },
            new EnvironmentVariables { Key = "KEY2", Value = "value2", ParentId = TestParentId }
        };

        var formatted = EnvironmentVariableConverter.Convert(original);
        var parsed = EnvironmentVariableConverter.Convert(formatted, TestParentId, EnvironmentVariableParentType.Project);

        parsed.Count.ShouldBe(2);
        parsed[0].Key.ShouldBe(original[0].Key);
        parsed[0].Value.ShouldBe(original[0].Value);
        parsed[1].Key.ShouldBe(original[1].Key);
        parsed[1].Value.ShouldBe(original[1].Value);
    }

    [Test]
    public void RoundTrip_ComplexVariables_ShouldPreserveValues()
    {
        var original = new[]
        {
            new EnvironmentVariables { Key = "SIMPLE", Value = "value", ParentId = TestParentId },
            new EnvironmentVariables { Key = "WITH_SPACES", Value = "value with spaces", ParentId = TestParentId },
            new EnvironmentVariables { Key = "WITH_QUOTES", Value = "value with \"quotes\"", ParentId = TestParentId },
            new EnvironmentVariables { Key = "EMPTY", Value = string.Empty, ParentId = TestParentId }
        };

        var formatted = EnvironmentVariableConverter.Convert(original);
        var parsed = EnvironmentVariableConverter.Convert(formatted, TestParentId, EnvironmentVariableParentType.Project);

        parsed.Count.ShouldBe(4);
        parsed[0].Value.ShouldBe("value");
        parsed[1].Value.ShouldBe("value with spaces");
        parsed[2].Value.ShouldBe("value with \"quotes\"");
        parsed[3].Value.ShouldBe(string.Empty);
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Convert_WithKeyButNullValue_ShouldFormatAsQuotedEmpty()
    {
        var variables = new[]
        {
            new EnvironmentVariables { Key = "KEY", Value = null, ParentId = TestParentId }
        };

        var result = EnvironmentVariableConverter.Convert(variables);

        result.ShouldBe("KEY=\"\"");
    }

    [Test]
    public void Convert_WithTabCharacterInValue_ShouldQuoteValue()
    {
        var variables = new[]
        {
            new EnvironmentVariables { Key = "KEY", Value = "value\twith\ttabs", ParentId = TestParentId }
        };

        var result = EnvironmentVariableConverter.Convert(variables);

        result.ShouldContain("KEY=");
        result.ShouldContain("\"");
    }

    [Test]
    public void Convert_WithNewlineCharacterInValue_ShouldQuoteValue()
    {
        var variables = new[]
        {
            new EnvironmentVariables { Key = "KEY", Value = "value\nwith\nnewlines", ParentId = TestParentId }
        };

        var result = EnvironmentVariableConverter.Convert(variables);

        result.ShouldContain("KEY=");
        result.ShouldContain("\"");
    }

    [Test]
    public void Convert_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        var envContent = "KEY=\"value with !@#$%^&*() special chars\"";

        var result = EnvironmentVariableConverter.Convert(envContent, TestParentId, EnvironmentVariableParentType.Project);

        result.Count.ShouldBe(1);
        result[0].Value.ShouldContain("special chars");
    }

    [Test]
    public void Convert_ParseWithQuoteAtStartButNotEnd_ShouldNotUnquote()
    {
        var envContent = "KEY=\"unclosed quote value";

        var result = EnvironmentVariableConverter.Convert(envContent, TestParentId, EnvironmentVariableParentType.Project);

        result.Count.ShouldBe(1);
        result[0].Value.ShouldBe("\"unclosed quote value");
    }

    [Test]
    public void Convert_VeryLongValue_ShouldHandleCorrectly()
    {
        var longValue = new string('a', 10000);
        var variables = new[]
        {
            new EnvironmentVariables { Key = "KEY", Value = longValue, ParentId = TestParentId }
        };

        var result = EnvironmentVariableConverter.Convert(variables);
        var parsed = EnvironmentVariableConverter.Convert(result, TestParentId, EnvironmentVariableParentType.Project);

        parsed[0].Value.ShouldBe(longValue);
    }

    #endregion

    #region Parent Type Tests

    [Test]
    public void Convert_WithDifferentParentTypes_ShouldPreserveParentType()
    {
        var envContent = "KEY=value";

        var resultProject = EnvironmentVariableConverter.Convert(envContent, TestParentId, EnvironmentVariableParentType.Project);
        var resultEnvironment = EnvironmentVariableConverter.Convert(envContent, TestParentId, EnvironmentVariableParentType.Environment);
        var resultService = EnvironmentVariableConverter.Convert(envContent, TestParentId, EnvironmentVariableParentType.Service);

        resultProject[0].ParentType.ShouldBe(EnvironmentVariableParentType.Project);
        resultEnvironment[0].ParentType.ShouldBe(EnvironmentVariableParentType.Environment);
        resultService[0].ParentType.ShouldBe(EnvironmentVariableParentType.Service);
    }

    #endregion
}
