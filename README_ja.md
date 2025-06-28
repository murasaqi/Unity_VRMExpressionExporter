# VRM Expression Exporter

[![Unity 2022.3+](https://img.shields.io/badge/unity-2022.3%2B-blue.svg)](https://unity3d.com/get-unity/download)
[![UniVRM](https://img.shields.io/badge/UniVRM-0.129.2%2B-green.svg)](https://github.com/vrm-c/UniVRM)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

[English README is here](README.md)

VRMファイルから表情データを抽出、可視化、エクスポートするための包括的なUnityツールセットです。VRMアバター開発者やコンテンツクリエイターが表情設定を分析・活用するための強力なツールを提供します。

![VRM Expression Exporter Screenshot](Documentation~/images/tool-screenshot.png)

## 🚀 機能

### 主要機能
- **🎭 表情データ抽出**: VRM 1.0ファイルから完全な表情データを抽出
- **📸 プレビューキャプチャ**: 各表情のプレビュー画像を自動生成
- **📊 複数のエクスポート形式**: CSV、Excel（画像埋め込み）、HTMLビューア
- **🎬 AnimationClip生成**: VRM表情からUnityのAnimationClipを作成
- **⚡ バッチ処理**: 複数のVRMファイルを同時処理
- **🔧 柔軟な設定**: カスタマイズ可能なエクスポートオプションとフィルター

### サポートされる表情タイプ
- ブレンドシェイプ表情（表情モーフ）
- マテリアルカラー変更
- マテリアルUVトランスフォームアニメーション
- 表情プリセット（Happy、Angry、Sad など）

## 📦 インストール

### 方法1: Unity Package Manager (Git URL)

1. Unity Package Managerを開く（Window → Package Manager）
2. **+** ボタンをクリック → "Add package from git URL..."
3. 以下を入力:
   ```
   https://github.com/murasaqi/Unity_VRMExpressionExporter.git?path=/com.murasaqi.vrmexpressionexporter#master
   ```

### 方法2: Package Manager (ローカル)

1. このリポジトリをクローン
2. Unity Package Managerを開く
3. **+** ボタンをクリック → "Add package from disk..."
4. `com.murasaqi.vrmexpressionexporter/`内の`package.json`ファイルを選択

### 方法3: 直接インポート

1. 最新リリースをダウンロード
2. `.unitypackage`ファイルをプロジェクトにインポート

### 依存関係

以下のパッケージが自動的にインストールされます：
- **UniVRM** (com.vrmc.vrm) 0.129.2以上
- **UniTask** (com.cysharp.unitask) 2.3.3以上

## 📖 使い方

### 基本的なワークフロー

1. **VRMファイルのインポート**
   - VRMファイルをAssetsフォルダ内の任意の場所に配置
   - Unityが自動的にインポートします

2. **Expression Exporterを開く**
   - メニュー: `VRM Tools → Expression Extractor`
   - またはショートカット: `Ctrl+Shift+E` (Windows) / `Cmd+Shift+E` (Mac)

3. **VRMオブジェクトを追加**
   - "Add VRM Object"をクリック、またはProjectウィンドウからVRMファイルをドラッグ
   - 複数のVRMファイルを一度に処理可能

4. **エクスポートオプションを設定**
   - **CSV Export**: スプレッドシート形式で表情データをエクスポート
   - **Capture Previews**: 各表情のプレビュー画像を生成
   - **Generate AnimationClips**: UnityのAnimationClipを作成
   - **Exclude Mouth BlendShapes**: 口関連のモーフをフィルター

5. **エクスポート**
   - "Export All"をクリックしてすべてのVRMファイルを処理
   - 出力ファイルは`Assets/ExpressionExports/`に保存されます

### 高度な機能

#### CSV から Excel への変換
```
VRM Tools → Convert CSV to Excel
```
- CSVファイルをプレビュー画像埋め込みのExcel形式に変換
- Python環境に`openpyxl`と`Pillow`パッケージが必要

#### HTMLビューアの生成
エクスポーターは以下の機能を持つインタラクティブなHTMLビューアを自動生成：
- すべての表情のビジュアルギャラリー
- 検索とフィルター機能
- 並列比較モード
- 個別の表情データのエクスポート

### 🔧 エクスポートオプション

| オプション | 説明 | デフォルト |
|-----------|------|-----------|
| CSV Export | CSV形式で表情データをエクスポート | ✓ |
| Capture Previews | プレビュー画像を生成（512x512 PNG） | ✓ |
| Generate AnimationClips | 各表情の.animファイルを作成 | ✓ |
| Exclude Mouth BlendShapes | ビジームと口関連のシェイプをフィルター | ✗ |
| Resolution | プレビュー画像の解像度 | 512x512 |
| Background | プレビューの背景色 | 透明 |

## 💻 API リファレンス

### 基本的な使用法
```csharp
using VRMExpressionExporter;
using UnityEngine;

public class ExpressionExportExample : MonoBehaviour
{
    async void ExportVRMExpressions()
    {
        // VRM GameObjectを取得
        GameObject vrmObject = GameObject.Find("YourVRMModel");
        
        // 表情データを抽出
        var extractor = new VRMExpressionExtractor();
        var data = await extractor.ExtractExpressionsAsync(vrmObject);
        
        // CSVにエクスポート
        var exporter = new ExcelExporter();
        exporter.ExportToCSV(data, "Assets/export.csv");
        
        // プレビューをキャプチャ
        var capture = new VRMPreviewCapture();
        foreach (var expression in data.expressions)
        {
            var texture = await capture.CaptureExpressionAsync(vrmObject, expression);
            // テクスチャを保存...
        }
    }
}
```

### カスタムエクスポートパイプライン
```csharp
// カスタムエクスポート設定を作成
var settings = new ExportSettings
{
    capturePreview = true,
    previewResolution = 1024,
    excludeMouthBlendShapes = true,
    backgroundColor = Color.gray
};

// カスタム設定でエクスポート
await exporter.ExportWithSettingsAsync(vrmObject, settings);
```

## 📁 出力構造

```
Assets/ExpressionExports/
├── キャラクター名_yyyyMMdd_HHmmss/
│   ├── summary.csv                 # すべての表情の概要
│   ├── キャラクター名_details.csv   # 詳細な表情データ
│   ├── Previews/                   # プレビュー画像
│   │   ├── happy.png
│   │   ├── angry.png
│   │   └── ...
│   ├── AnimationClips/             # 生成されたアニメーションクリップ
│   │   ├── キャラクター名_happy.anim
│   │   ├── キャラクター名_angry.anim
│   │   └── ...
│   └── viewer.html                 # インタラクティブHTMLビューア
```

## 🐍 Python 統合

パッケージには拡張Excelエクスポート用のPythonスクリプトが含まれています：

```bash
# 必要なパッケージをインストール
pip install openpyxl pillow pandas

# 画像埋め込みでCSVをExcelに変換
python convert_csv_to_excel.py input.csv output.xlsx
```

## 🤝 コントリビューション

貢献を歓迎します！プルリクエストをお気軽に送信してください。大きな変更については、まず変更したい内容について議論するためにイシューを開いてください。

1. リポジトリをフォーク
2. フィーチャーブランチを作成 (`git checkout -b feature/AmazingFeature`)
3. 変更をコミット (`git commit -m 'Add some AmazingFeature'`)
4. ブランチにプッシュ (`git push origin feature/AmazingFeature`)
5. プルリクエストを開く

## 📝 ライセンス

このプロジェクトはMITライセンスの下でライセンスされています - 詳細は[LICENSE.md](LICENSE.md)ファイルを参照してください。

## 🙏 謝辞

- VRM仕様を提供してくださった[VRMコンソーシアム](https://vrm.dev/)
- UnityでのVRM実装を提供してくださった[UniVRM](https://github.com/vrm-c/UniVRM)
- 優れたUniTaskライブラリを提供してくださった[Cysharp](https://github.com/Cysharp/UniTask)