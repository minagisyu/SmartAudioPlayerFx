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
[assembly: AssemblyCopyright("Copyright (C) 2010 MinagiSyu, intre.")]
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
[assembly: AssemblyVersion("3.1.0.7")]

#if DEBUG

[assembly: InternalsVisibleTo("SmartAudioPlayerFx.Test, PublicKey=00240000048000009400000006020000002400005253413100040000010001000b6fcd17f102b4b9b23cf3d052b85a7ecdde13dee1fb1f1ca880c41b8af0fe1de071fcd2c024d44f1d09e51596a3d1c6dc6d49fe6708b9c2412df20a75378ccaa07f78615251211beea4d237939f8eef147c422b2a9ca5b15d1e17f64471e84e77f672ff63359ccb5640911c3c86f68bc50463e907d498a0b29bee0b14e8dcb5")]

#endif
