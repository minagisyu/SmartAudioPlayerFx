using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;

// アセンブリに関する一般情報は以下の属性セットをとおして制御されます。
// アセンブリに関連付けられている情報を変更するには、
// これらの属性値を変更してください。
[assembly: AssemblyTitle("SmartAudioPlayer Fx")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("intre.")]
[assembly: AssemblyProduct("SmartAudioPlayer")]
[assembly: AssemblyCopyright("Copyright (C) 2011 MinagiSyu, intre.")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// ComVisible を false に設定すると、その型はこのアセンブリ内で COM コンポーネントから
// 参照不可能になります。COM からこのアセンブリ内の型にアクセスする場合は、
// その型の ComVisible 属性を true に設定してください。
[assembly: ComVisible(false)]

//ローカライズ可能なアプリケーションのビルドを開始するには、
//.csproj ファイルの <UICulture>CultureYouAreCodingWith</UICulture> を
//<PropertyGroup> 内部で設定します。たとえば、
//ソース ファイルで英語を使用している場合、<UICulture> を en-US に設定します。次に、
//下の NeutralResourceLanguage 属性のコメントを解除します。下の行の "en-US" を
//プロジェクト ファイルの UICulture 設定と一致するよう更新します。

//[assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.Satellite)]


[assembly: ThemeInfo(
	ResourceDictionaryLocation.None, //テーマ固有のリソース ディクショナリが置かれている場所
	//(リソースがページ、
	//またはアプリケーション リソース ディクショナリに見つからない場合に使用されます)
	ResourceDictionaryLocation.SourceAssembly //汎用リソース ディクショナリが置かれている場所
	//(リソースがページ、
	//アプリケーション、またはいずれのテーマ固有のリソース ディクショナリにも見つからない場合に使用されます)
)]


// アセンブリのバージョン情報は、以下の 4 つの値で構成されています:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// すべての値を指定するか、下のように '*' を使ってビルドおよびリビジョン番号を 
// 既定値にすることができます:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("3.2.0.3")]

// internalクラスを"SAPFxLib.Test"に公開する(デバッグ時のみ)
#if DEBUG
[assembly: InternalsVisibleTo("SmartAudioPlayerFx.Test, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b87623faa4edbde4128bce447f59ac8a10ffc3b2eb8328e6cbd85f59bac337c43638819cee5c4683b7b8c7526253d18dc18420904dd4583f8f4a108e2eab9186a044c6e051fe32555af055637ca667b8aba041365c6a6e4a899a5d99b2fd273b298995b0058598f5828dc3d797e379054f6d49d80928b6649f58df0a75877ea")]
#endif