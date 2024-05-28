using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.IO;
using System.Linq;
using CsvHelper;
using System.Text;

//警告に従ってプロジェクトの設定を変更するとうまく動作しなくなる
//ずっと警告が表示されるのも気持ち悪いので無効化
#pragma warning disable RS1036

namespace YukkuriMovieMaker.Generator
{
    [Generator(LanguageNames.CSharp)]
    public sealed class AutoGenLocalizerPluginGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            //AutoGenLocalizerAttributeを生成
            context.RegisterPostInitializationOutput(context =>context.AddSource(
                "AutoGenLocalizerAttribute.g.cs",
                $@"namespace YukkuriMovieMaker.Generator
{{
    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    sealed class AutoGenLocalizerAttribute : System.Attribute
    {{
        public AutoGenLocalizerAttribute()
        {{
        }}
    }}
}}"));

            //コンパイル時に一度[AutoGenLocalizer]のINamedTypeSymbolを通知するValueProvider
            var attributeSymbolProvider =
                context.CompilationProvider.Select(
                    (compilation, token) =>
                    {
                        return
                            compilation.GetTypeByMetadataName("YukkuriMovieMaker.Generator.AutoGenLocalizerAttribute")
                            ?? throw new InvalidOperationException("AutoGenLocalizerAttribute not found");
                    });

            //プロジェクト中のcsvファイルの変更通知するValueProvider
            //利用側の.csprojに
            //<ItemGroup>
            //  <AdditionalFiles Include="**/*.csv" />
            //</ItemGroup>
            //を追加する必要がある
            var csvProvider =
                context.AdditionalTextsProvider
                .Where(x => x.Path.EndsWith(".csv"))
                .Collect()
                .Select((x, token)=> x.Select(x=>x.GetText(token)?.GetContentHash()));

            //Attributeを1つ以上もつpartial classの変更通知をするValueProvider
            var classProvider =
                context.SyntaxProvider.CreateSyntaxProvider(
                    predicate: (node, token) =>
                    {
                        token.ThrowIfCancellationRequested();
                        return node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };
                    },
                    transform: (context, token) =>
                    {
                        token.ThrowIfCancellationRequested();
                        if (context.Node is not ClassDeclarationSyntax classDecl)
                            return default;

                        var symbol = context.SemanticModel.GetDeclaredSymbol(classDecl, token);
                        if (symbol is null)
                            return default;

                        if (!classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                            return default;

                        var filePath = context.Node.SyntaxTree.FilePath;
                        if (string.IsNullOrEmpty(filePath))
                            return default;
                        var csvFile = Path.ChangeExtension(filePath, ".csv");

                        return (symbol, classDecl.Modifiers, csvFile);
                    }
                )
                .Where(x => x is (not null, _, _));

            //各プロバイダの変更通知を受け取り、AutoGenLocalizerAttributeが存在し、classと同名のcsvファイルが存在する場合にのみ通知を通すValueProvider
            var provider = 
                classProvider
                .Combine(attributeSymbolProvider)
                .Combine(csvProvider)
                .Select((x, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    var (Left, Right) = x.Left;
                    var (classSymbol, modifiers, csvFile) = Left;
                    var attributeSymbol = Right;

                    var csvHash = x.Right;

                    return (classSymbol, modifiers, attributeSymbol, csvFile, csvHash);
                })
                .Where(x => File.Exists(x.csvFile))
                .Where(x => x.classSymbol.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, x.attributeSymbol)));

            //プロバイダの変更通知を受け取り、ソースコードを生成する
            context.RegisterSourceOutput(
                provider,
                (context, tuple) =>
                {
                    var token = context.CancellationToken;
                    token.ThrowIfCancellationRequested();

                    var (symbol, modifiers, attributeSymbol, csvFile, csvHash) = tuple;

                    var csvConfig = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = true,
                        Delimiter = ",",
                        IgnoreBlankLines = true,
                        TrimOptions = CsvHelper.Configuration.TrimOptions.Trim,
                        MissingFieldFound = null,
                    };
                    using var streamReader = new StreamReader(csvFile);
                    using var csvReader = new CsvReader(streamReader, csvConfig);
                    var records = csvReader.GetRecords<TranslateRecord>().Distinct(new TranslateRecordKeyComparer()).ToList();
                    token.ThrowIfCancellationRequested();

                    //resxファイルをソースフォルダに直接出力する
                    //.csとは異なりAddSource等で追加できないため
                    foreach (var langCode in TranslateRecord.LangCodes)
                    {
                        token.ThrowIfCancellationRequested();
                        var resxPath = langCode is "ja-jp" ?
                            Path.ChangeExtension(csvFile, ".resx")
                            : Path.ChangeExtension(csvFile, $".{langCode}.resx");

                        using var writer = new ResXResourceWriter(resxPath);
                        writer.AddResource("CurrentCulture", langCode);
                        foreach (var record in records)
                        {
                            var value = record.GetValue(langCode);
                            if (string.IsNullOrEmpty(record.Key) || string.IsNullOrEmpty(value))
                                continue;
                            writer.AddResource(record.Key!, value!);
                        }

                    }

                    var sourceBuilder = new StringBuilder();
                    sourceBuilder.AppendLine($@"//{Path.ChangeExtension(csvFile, ".resx")}
using YukkuriMovieMaker.Plugin;

namespace {symbol.ContainingNamespace.ToDisplayString()}
{{
    {string.Join(" ", modifiers.Select(x => x.ToString()))} class {symbol.Name} : ILocalizePlugin
    {{
        public string Name => ""{symbol.Name}多言語対応プラグイン（自動生成）{csvHash}"";
        public void SetCulture(System.Globalization.CultureInfo cultureInfo) => {symbol.Name}.Culture = cultureInfo;


        private static System.Resources.ResourceManager _resourceManager;
        private static System.Globalization.CultureInfo _cultureInfo;

        public static System.Resources.ResourceManager ResourceManager
        {{
            get
            {{
                if (_resourceManager is null)
                    _resourceManager = new System.Resources.ResourceManager(typeof({symbol.Name}));
                return _resourceManager;
            }}
        }}

        public static System.Globalization.CultureInfo Culture
        {{
            get => _cultureInfo;
            set => _cultureInfo = value;
        }}

        public static string GetString(string key)
        {{
            return ResourceManager.GetString(key, Culture);
        }}");
                    sourceBuilder.AppendLine($@"        ///<summary>");
                    sourceBuilder.AppendLine($@"        /// 言語コード");
                    sourceBuilder.AppendLine($@"        ///</summary>");
                    sourceBuilder.AppendLine($@"        public static string CurrentCulture => GetString(""CurrentCulture"");");
                    foreach (var record in records)
                    {
                        token.ThrowIfCancellationRequested();
                        sourceBuilder.AppendLine($@"        ///<summary>");
                        sourceBuilder.AppendLine($@"        /// {record.Japanese?.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).Aggregate((a,b)=>$"{a}\r\n        /// {b}")}");
                        sourceBuilder.AppendLine($@"        ///</summary>");
                        sourceBuilder.AppendLine($@"        public static string {record.Key} => GetString(""{record.Key}"");");
                    }
                    sourceBuilder.AppendLine(@$"    }}
}}");
                    //生成したソースコードを追加
                    context.AddSource($"{symbol.Name}.g.cs", sourceBuilder.ToString());
                });
        }
    }
}
