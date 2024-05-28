# YukkuriMovieMaker.Generator
YMM4用のソースジェネレーターです

## AutoGenLocalizerPluginGenerator
### 概要
csv形式の翻訳ファイルからresx及びclassファイルを生成するソースジェネレーターです

### 使い方
1. ソースジェネレーターを使用したいプロジェクトが存在するリポジトリのサブモジュールとしてYukkuriMovieMaker.Generatorを追加する
```
git submodule add https://github.com/manju-summoner/YukkuriMovieMaker.Generator.git
```
2. ソースジェネレーターを使用したいプロジェクトのcsprojを開き、以下のように編集する
```xml
  <ItemGroup>
    <ProjectReference Include="..\YukkuriMovieMaker.Generator\YukkuriMovieMaker.Generator\YukkuriMovieMaker.Generator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />   
    <AdditionalFiles Include="**/*.csv" />
  </ItemGroup>
```
3. csvファイルを追加する。（例: Translate.csv）
```csv
Key,comment,ja-jp,en-us,zh-cn,zh-tw,ko-kr,es-es,ar-sa,
SampleKey,コメント,サンプル,Sample,示例,範例,샘플,Muestra,عينة,
```
4. csvファイルと同じフォルダに、csvファイルと同名のpartial classを作成し、[AutoGenLocalizer]属性を付与する。（例: Translate.cs）
```cs
using System;
using YukkuriMovieMaker.Generator;

[AutoGenLocalizer]
partial class Translate
{

}
```