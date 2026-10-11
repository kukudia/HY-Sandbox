// 职责：用 Roslyn 校验/整理自有脚本格式、脚本介绍和函数图导航，输出可审查的复杂度提示。
// 入口：dotnet run --project Tools/CodeReadability -- --check <project-root>；--write 显式写入。
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;

if (args.Length != 2 || args[0] is not ("--check" or "--write" or "--inventory" or "--check-tools" or "--write-tools"))
{
    Console.Error.WriteLine("Usage: CodeReadability --check|--write|--inventory|--check-tools|--write-tools <Unity project root>");
    return 2;
}

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
string projectRoot = Path.GetFullPath(args[1]);
bool write = args[0] is "--write" or "--write-tools";
bool toolScripts = args[0] is "--check-tools" or "--write-tools";
bool inventoryOnly = args[0] == "--inventory";
var inventory = new List<object>();
string[] roots = toolScripts ? ["Blueprints/CODEX", "Blueprints/CODEX_Enemies", "Blueprints/DSH_Enemies"] : ["Assets/Scripts", "Assets/Editor"];
using var workspace = new AdhocWorkspace();
var options = workspace.Options
    .WithChangedOption(FormattingOptions.UseTabs, LanguageNames.CSharp, false)
    .WithChangedOption(FormattingOptions.TabSize, LanguageNames.CSharp, 4)
    .WithChangedOption(FormattingOptions.IndentationSize, LanguageNames.CSharp, 4)
    .WithChangedOption(FormattingOptions.NewLine, LanguageNames.CSharp, "\r\n")
    .WithChangedOption(CSharpFormattingOptions.WrappingPreserveSingleLine, false)
    .WithChangedOption(CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine, false);
int failures = 0;
int changed = 0;
int count = 0;
var symbols = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
foreach (string relative in roots)
{
    foreach (string file in Directory.EnumerateFiles(Path.Combine(projectRoot, relative), "*.cs", SearchOption.AllDirectories))
    {
        count++;
        byte[] bytes = File.ReadAllBytes(file);
        Encoding encoding;
        string text;
        try
        {
            encoding = new UTF8Encoding(bytes.AsSpan().StartsWith(new byte[] { 239, 187, 191 }), true);
            text = encoding.GetString(bytes).TrimStart('\uFEFF');
        }
        catch (DecoderFallbackException)
        {
            encoding = Encoding.GetEncoding(54936);
            text = encoding.GetString(bytes);
        }
        // Normalize accidental CRCRLF before parsing, without interpreting string literal contents.
        var tree = CSharpSyntaxTree.ParseText(text, new CSharpParseOptions(LanguageVersion.CSharp9, kind: toolScripts ? SourceCodeKind.Script : SourceCodeKind.Regular, preprocessorSymbols: new[] { "UNITY_EDITOR", "UNITY_6000_3_OR_NEWER", "ENABLE_INPUT_SYSTEM" }));
        if (tree.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            Console.Error.WriteLine($"Syntax error: {Path.GetRelativePath(projectRoot, file)}");
            failures++;
            continue;
        }
        var root = tree.GetRoot();
        string key = Path.GetRelativePath(projectRoot, file).Replace('\\', '/');
        symbols[key] = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Select(m => m.Identifier.ValueText).ToHashSet();
        if (inventoryOnly)
        {
            inventory.Add(new
            {
                file = key,
                introduction = string.Join(" ", text.Split('\n').TakeWhile(line => line.StartsWith("//")).Select(line => line.Trim())),
                methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Select(m => new
                {
                    type = m.Ancestors().OfType<TypeDeclarationSyntax>().First().Identifier.ValueText,
                    name = m.Identifier.ValueText,
                    signature = m.ReturnType + " " + m.Identifier + m.ParameterList.ToString(),
                    line = m.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    access = string.Join(" ", m.Modifiers.Select(t => t.Text))
                }).ToArray(),
                fields = root.DescendantNodes().OfType<FieldDeclarationSyntax>()
                    .Where(f => f.Modifiers.Any(SyntaxKind.PublicKeyword) || f.AttributeLists.ToString().Contains("SerializeField", StringComparison.Ordinal))
                    .SelectMany(f => f.Declaration.Variables.Select(v => new
                    {
                        type = f.Ancestors().OfType<TypeDeclarationSyntax>().First().Identifier.ValueText,
                        name = v.Identifier.ValueText,
                        fieldType = f.Declaration.Type.ToString(),
                        value = v.Initializer?.Value.ToString(),
                        modifiers = string.Join(" ", f.Modifiers.Select(t => t.Text))
                    })).ToArray()
            });
            continue;
        }
        var revised = new BracesRewriter().Visit(root)!.NormalizeWhitespace("    ", "\r\n");
        if (!toolScripts)
        {
            revised = new LongListsRewriter().Visit(revised)!;
        }
        var formatted = Formatter.Format(revised, workspace, options);
        string output = (toolScripts ? formatted.ToFullString() : IndentWrappedLists(formatted.ToFullString())).TrimEnd() + "\r\n";
        if (!toolScripts)
        {
            output = NormalizeDocumentedDeclarations(output);
        }
        // Added braces may change tokens; removing only brace tokens must leave every other token unchanged.
        var oldTokens = root.DescendantTokens().Where(t => !t.IsKind(SyntaxKind.OpenBraceToken) && !t.IsKind(SyntaxKind.CloseBraceToken)).Select(t => (t.RawKind, t.Text));
        var outputTree = CSharpSyntaxTree.ParseText(output, (CSharpParseOptions)tree.Options);
        var newTokens = outputTree.GetRoot().DescendantTokens().Where(t => !t.IsKind(SyntaxKind.OpenBraceToken) && !t.IsKind(SyntaxKind.CloseBraceToken)).Select(t => (t.RawKind, t.Text));
        if (!oldTokens.SequenceEqual(newTokens))
        {
            throw new InvalidOperationException($"Formatting changed non-brace tokens: {key}");
        }
        if (!root.GetLeadingTrivia().ToFullString().Contains("// 职责：", StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"Missing script introduction: {key}");
            failures++;
        }
        if (text.Contains('\uFFFD'))
        {
            Console.Error.WriteLine($"Unresolved replacement characters: {key}");
            failures++;
        }
        if (text != output)
        {
            changed++;
            if (write)
            {
                File.WriteAllText(file, output, encoding);
            }
            else
            {
                Console.Error.WriteLine($"Format differs: {key}");
                failures++;
            }
        }
        int lines = output.Split('\n').Length;
        if (relative == "Assets/Scripts" && lines > 350)
        {
            Console.WriteLine($"Review signal (not an error): {key}, {lines} lines");
        }
    }
}

if (inventoryOnly)
{
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(inventory, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    return failures == 0 ? 0 : 1;
}

if (toolScripts)
{
    Console.WriteLine($"Tool scripts: {count}; formatted/differing: {changed}; errors: {failures}");
    return failures == 0 ? 0 : 1;
}

string flowPath = Path.Combine(projectRoot, "Docs/RuntimeFlow.md");
if (!File.Exists(flowPath))
{
    Console.Error.WriteLine("Missing Docs/RuntimeFlow.md");
    failures++;
}
else
{
    string flow = File.ReadAllText(flowPath);
    var references = Regex.Matches(flow, @"<!-- source: (?<file>[^|]+)\|(?<method>\w+) -->");
    if (references.Count == 0)
    {
        Console.Error.WriteLine("RuntimeFlow.md has no validated source references");
        failures++;
    }
    foreach (Match match in references)
    {
        string file = match.Groups["file"].Value;
        string method = match.Groups["method"].Value;
        if (!symbols.TryGetValue(file, out var methods) || !methods.Contains(method))
        {
            Console.Error.WriteLine($"Invalid flow symbol: {file}::{method}");
            failures++;
        }
    }
    if (!flow.Contains("```mermaid", StringComparison.Ordinal))
    {
        Console.Error.WriteLine("RuntimeFlow.md has no Mermaid diagram");
        failures++;
    }
}
Console.WriteLine($"Scripts: {count}; formatted/differing: {changed}; errors: {failures}");
return failures == 0 ? 0 : 1;

static string IndentWrappedLists(string text)
{
    // Adhoc formatting has no document indentation service for newly wrapped lists.
    // Set only leading whitespace on argument/parameter lines, in source order so nested lists inherit it.
    var tree = CSharpSyntaxTree.ParseText(text);
    var source = tree.GetText();
    string[] lines = text.Split("\r\n");
    foreach (SyntaxNode list in tree.GetRoot().DescendantNodes().Where(n => n is ArgumentListSyntax or ParameterListSyntax).OrderBy(n => n.SpanStart))
    {
        SyntaxToken open;
        IEnumerable<SyntaxNode> items;
        if (list is ArgumentListSyntax arguments)
        {
            open = arguments.OpenParenToken;
            items = arguments.Arguments;
        }
        else
        {
            var parameters = (ParameterListSyntax)list;
            open = parameters.OpenParenToken;
            items = parameters.Parameters;
        }
        int openingLine = source.Lines.GetLineFromPosition(open.SpanStart).LineNumber;
        int indentation = lines[openingLine].TakeWhile(c => c == ' ').Count() + 4;
        foreach (SyntaxNode item in items)
        {
            var line = source.Lines.GetLineFromPosition(item.GetFirstToken().SpanStart);
            int column = item.GetFirstToken().SpanStart - line.Start;
            if (line.LineNumber != openingLine && source.ToString(line.Span)[..column].All(char.IsWhiteSpace))
            {
                lines[line.LineNumber] = new string(' ', indentation) + lines[line.LineNumber].TrimStart(' ', '\t');
            }
        }
    }
    return string.Join("\r\n", lines);
}

static string NormalizeDocumentedDeclarations(string text)
{
    // NormalizeWhitespace + Adhoc Formatter can over-indent declarations following XML trivia.
    // Align the documentation and declaration to their syntax ancestry without touching tokens.
    var tree = CSharpSyntaxTree.ParseText(text);
    var source = tree.GetText();
    string[] lines = text.Split("\r\n");
    foreach (var member in tree.GetRoot().DescendantNodes().OfType<MemberDeclarationSyntax>())
    {
        var docs = member.GetLeadingTrivia().Where(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)).ToArray();
        if (docs.Length == 0) { continue; }
        int indent = member.Ancestors().Count(n => n is BaseTypeDeclarationSyntax or NamespaceDeclarationSyntax) * 4;
        var declarationLine = source.Lines.GetLineFromPosition(member.GetFirstToken().SpanStart);
        int column = member.GetFirstToken().SpanStart - declarationLine.Start;
        if (source.ToString(declarationLine.Span)[..column].All(char.IsWhiteSpace))
        {
            lines[declarationLine.LineNumber] = new string(' ', indent) + lines[declarationLine.LineNumber].TrimStart();
        }
        foreach (var trivia in docs)
        {
            int first = source.Lines.GetLineFromPosition(trivia.FullSpan.Start).LineNumber;
            int last = source.Lines.GetLineFromPosition(trivia.FullSpan.End).LineNumber;
            for (int i = first; i <= last; i++)
            {
                if (lines[i].TrimStart().StartsWith("///", StringComparison.Ordinal))
                {
                    lines[i] = new string(' ', indent) + lines[i].TrimStart();
                }
            }
        }
    }
    return string.Join("\r\n", lines);
}

sealed class BracesRewriter : CSharpSyntaxRewriter
{
    private static StatementSyntax Block(StatementSyntax statement)
    {
        if (statement is BlockSyntax)
        {
            return statement;
        }
        return SyntaxFactory.Block(statement.WithoutLeadingTrivia())
            .WithLeadingTrivia(statement.GetLeadingTrivia());
    }

    public override SyntaxNode? VisitIfStatement(IfStatementSyntax node)
    {
        var visited = (IfStatementSyntax)base.VisitIfStatement(node)!;
        return visited.WithStatement(Block(visited.Statement));
    }

    public override SyntaxNode? VisitElseClause(ElseClauseSyntax node)
    {
        var visited = (ElseClauseSyntax)base.VisitElseClause(node)!;
        return visited.Statement is IfStatementSyntax ? visited : visited.WithStatement(Block(visited.Statement));
    }

    public override SyntaxNode? VisitForStatement(ForStatementSyntax node)
    {
        var visited = (ForStatementSyntax)base.VisitForStatement(node)!;
        return visited.WithStatement(Block(visited.Statement));
    }

    public override SyntaxNode? VisitForEachStatement(ForEachStatementSyntax node)
    {
        var visited = (ForEachStatementSyntax)base.VisitForEachStatement(node)!;
        return visited.WithStatement(Block(visited.Statement));
    }

    public override SyntaxNode? VisitForEachVariableStatement(ForEachVariableStatementSyntax node)
    {
        var visited = (ForEachVariableStatementSyntax)base.VisitForEachVariableStatement(node)!;
        return visited.WithStatement(Block(visited.Statement));
    }

    public override SyntaxNode? VisitWhileStatement(WhileStatementSyntax node)
    {
        var visited = (WhileStatementSyntax)base.VisitWhileStatement(node)!;
        return visited.WithStatement(Block(visited.Statement));
    }

    public override SyntaxNode? VisitDoStatement(DoStatementSyntax node)
    {
        var visited = (DoStatementSyntax)base.VisitDoStatement(node)!;
        return visited.WithStatement(Block(visited.Statement));
    }

    public override SyntaxNode? VisitUsingStatement(UsingStatementSyntax node)
    {
        var visited = (UsingStatementSyntax)base.VisitUsingStatement(node)!;
        return visited.WithStatement(Block(visited.Statement));
    }

    public override SyntaxNode? VisitLockStatement(LockStatementSyntax node)
    {
        var visited = (LockStatementSyntax)base.VisitLockStatement(node)!;
        return visited.WithStatement(Block(visited.Statement));
    }
}

// Wrap parameter/argument lists at semantic separators, never inside literals or expressions.
sealed class LongListsRewriter : CSharpSyntaxRewriter
{
    private static readonly SyntaxTrivia NewLine = SyntaxFactory.ElasticCarriageReturnLineFeed;
    private static SeparatedSyntaxList<T> Wrap<T>(SeparatedSyntaxList<T> list) where T : SyntaxNode
    {
        var nodes = list.GetWithSeparators().Select(item => item.IsToken
            ? (SyntaxNodeOrToken)item.AsToken().WithTrailingTrivia(item.AsToken().TrailingTrivia.Add(NewLine))
            : item).ToArray();
        return SyntaxFactory.SeparatedList<T>(nodes);
    }

    public override SyntaxNode? VisitArgumentList(ArgumentListSyntax node)
    {
        var visited = (ArgumentListSyntax)base.VisitArgumentList(node)!;
        return visited.Arguments.Count > 1 && (visited.ToString().Length > 100 || visited.Arguments.Count > 5)
            ? visited.WithOpenParenToken(visited.OpenParenToken.WithTrailingTrivia(NewLine)).WithArguments(Wrap(visited.Arguments))
            : visited;
    }

    public override SyntaxNode? VisitParameterList(ParameterListSyntax node)
    {
        var visited = (ParameterListSyntax)base.VisitParameterList(node)!;
        return visited.Parameters.Count > 1 && (visited.ToString().Length > 100 || visited.Parameters.Count > 5)
            ? visited.WithOpenParenToken(visited.OpenParenToken.WithTrailingTrivia(NewLine)).WithParameters(Wrap(visited.Parameters))
            : visited;
    }
}
