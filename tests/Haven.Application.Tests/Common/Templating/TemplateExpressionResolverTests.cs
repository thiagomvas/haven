using Haven.Application.Common.Templating;

using Shouldly;

namespace Haven.Application.Tests.Common.Templating;

[TestFixture]
[Category("Unit")]
public class TemplateExpressionResolverTests
{
    [Test]
    public void Resolve_SubstitutesSuppliedNamespace()
    {
        var result = TemplateExpressionResolver.Resolve(
            "hello ${{ inputs.name }}",
            new Dictionary<string, TemplateNamespaceResolver> { ["inputs"] = k => k == "name" ? "world" : null });

        result.ShouldBe("hello world");
    }

    [Test]
    public void Resolve_LeavesUnsuppliedNamespaceUntouched()
    {
        var result = TemplateExpressionResolver.Resolve(
            "${{ inputs.name }} at ${{ runtime.host }}",
            new Dictionary<string, TemplateNamespaceResolver> { ["inputs"] = _ => "value" });

        result.ShouldBe("value at ${{ runtime.host }}");
    }

    [Test]
    public void Resolve_LeavesUnknownKeyWithinSuppliedNamespaceUntouched()
    {
        var result = TemplateExpressionResolver.Resolve(
            "${{ env.MISSING }}",
            new Dictionary<string, TemplateNamespaceResolver> { ["env"] = _ => null });

        result.ShouldBe("${{ env.MISSING }}");
    }

    [Test]
    public void Resolve_NoNamespacesSupplied_IsNoOp()
    {
        var input = "${{ inputs.x }} plain text ${{ env.Y }}";

        var result = TemplateExpressionResolver.Resolve(input, new Dictionary<string, TemplateNamespaceResolver>());

        result.ShouldBe(input);
    }

    [Test]
    public void Resolve_AppliesUrlencodeFilter()
    {
        var result = TemplateExpressionResolver.Resolve(
            "${{ env.PASSWORD | urlencode }}",
            new Dictionary<string, TemplateNamespaceResolver> { ["env"] = _ => "p@ss/word" });

        result.ShouldBe(Uri.EscapeDataString("p@ss/word"));
    }

    [Test]
    public void FindKeys_ReturnsDistinctKeysForNamespace()
    {
        var keys = TemplateExpressionResolver.FindKeys(
            "${{ inputs.a }}-${{ inputs.b }}-${{ inputs.a }}-${{ env.c }}", "inputs");

        keys.ShouldBe(["a", "b"], ignoreOrder: true);
    }

    [Test]
    public void FindKeys_NoMatches_ReturnsEmpty()
    {
        var keys = TemplateExpressionResolver.FindKeys("no placeholders here", "inputs");

        keys.ShouldBeEmpty();
    }
}