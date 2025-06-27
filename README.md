# VRM Expression Exporter

[![Unity 2022.3+](https://img.shields.io/badge/unity-2022.3%2B-blue.svg)](https://unity3d.com/get-unity/download)
[![UniVRM](https://img.shields.io/badge/UniVRM-0.129.2%2B-green.svg)](https://github.com/vrm-c/UniVRM)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

[日本語版 README はこちら](README_ja.md)

A comprehensive Unity toolset for extracting, visualizing, and exporting VRM expression data. This package provides powerful tools for VRM avatar developers and content creators to analyze and utilize expression configurations.

![VRM Expression Exporter Screenshot](Documentation~/images/tool-screenshot.png)

## 🚀 Features

### Core Functionality
- **🎭 Expression Data Extraction**: Extract complete expression data from VRM 1.0 files
- **📸 Preview Capture**: Automatically generate preview images for each expression
- **📊 Multiple Export Formats**: CSV, Excel (with embedded images), HTML viewer
- **🎬 AnimationClip Generation**: Create Unity AnimationClips from VRM expressions
- **⚡ Batch Processing**: Process multiple VRM files simultaneously
- **🔧 Flexible Configuration**: Customizable export options and filters

### Supported Expression Types
- Blend Shape expressions (facial morphs)
- Material color modifications
- Material UV transform animations
- Expression presets (Happy, Angry, Sad, etc.)

## 📦 Installation

### Option 1: Unity Package Manager (Git URL)

1. Open Unity Package Manager (Window → Package Manager)
2. Click the **+** button → "Add package from git URL..."
3. Enter:
   ```
   https://github.com/YOUR_USERNAME/Unity_VRMExpressionExporter.git#main
   ```

### Option 2: Package Manager (Local)

1. Clone this repository
2. Open Unity Package Manager
3. Click the **+** button → "Add package from disk..."
4. Select the `package.json` file from `Assets/VRMExpressionExporter/`

### Option 3: Direct Import

1. Download the latest release
2. Import the `.unitypackage` file into your project

### Dependencies

The following packages will be automatically installed:
- **UniVRM** (com.vrmc.vrm) 0.129.2+
- **UniTask** (com.cysharp.unitask) 2.3.3+

## 📖 Usage

### Basic Workflow

1. **Import VRM Files**
   - Place your VRM files anywhere in the Assets folder
   - Unity will automatically import them

2. **Open Expression Exporter**
   - Menu: `VRM Tools → Expression Extractor`
   - Or use the shortcut: `Ctrl+Shift+E` (Windows) / `Cmd+Shift+E` (Mac)

3. **Add VRM Objects**
   - Click "Add VRM Object" or drag VRM files from the Project window
   - Multiple VRM files can be processed at once

4. **Configure Export Options**
   - **CSV Export**: Export expression data in spreadsheet format
   - **Capture Previews**: Generate preview images for each expression
   - **Generate AnimationClips**: Create Unity AnimationClips
   - **Exclude Mouth BlendShapes**: Filter out mouth-related morphs

5. **Export**
   - Click "Export All" to process all VRM files
   - Output files will be saved to `Assets/ExpressionExports/`

### Advanced Features

#### CSV to Excel Conversion
```
VRM Tools → Convert CSV to Excel
```
- Converts CSV files to Excel format with embedded preview images
- Requires Python with `openpyxl` and `Pillow` packages

#### HTML Viewer Generation
The exporter automatically generates an interactive HTML viewer featuring:
- Visual gallery of all expressions
- Search and filter capabilities
- Side-by-side comparison mode
- Export individual expression data

### 🔧 Export Options

| Option | Description | Default |
|--------|-------------|---------|
| CSV Export | Export expression data to CSV format | ✓ |
| Capture Previews | Generate preview images (512x512 PNG) | ✓ |
| Generate AnimationClips | Create .anim files for each expression | ✓ |
| Exclude Mouth BlendShapes | Filter out viseme and mouth-related shapes | ✗ |
| Resolution | Preview image resolution | 512x512 |
| Background | Preview background color | Transparent |

## 💻 API Reference

### Basic Usage
```csharp
using VRMExpressionExporter;
using UnityEngine;

public class ExpressionExportExample : MonoBehaviour
{
    async void ExportVRMExpressions()
    {
        // Get VRM GameObject
        GameObject vrmObject = GameObject.Find("YourVRMModel");
        
        // Extract expression data
        var extractor = new VRMExpressionExtractor();
        var data = await extractor.ExtractExpressionsAsync(vrmObject);
        
        // Export to CSV
        var exporter = new ExcelExporter();
        exporter.ExportToCSV(data, "Assets/export.csv");
        
        // Capture preview
        var capture = new VRMPreviewCapture();
        foreach (var expression in data.expressions)
        {
            var texture = await capture.CaptureExpressionAsync(vrmObject, expression);
            // Save texture...
        }
    }
}
```

### Custom Export Pipeline
```csharp
// Create custom export settings
var settings = new ExportSettings
{
    capturePreview = true,
    previewResolution = 1024,
    excludeMouthBlendShapes = true,
    backgroundColor = Color.gray
};

// Export with custom settings
await exporter.ExportWithSettingsAsync(vrmObject, settings);
```

## 📁 Output Structure

```
Assets/ExpressionExports/
├── CharacterName_yyyyMMdd_HHmmss/
│   ├── summary.csv                 # Overview of all expressions
│   ├── CharacterName_details.csv   # Detailed expression data
│   ├── Previews/                   # Preview images
│   │   ├── happy.png
│   │   ├── angry.png
│   │   └── ...
│   ├── AnimationClips/             # Generated animation clips
│   │   ├── CharacterName_happy.anim
│   │   ├── CharacterName_angry.anim
│   │   └── ...
│   └── viewer.html                 # Interactive HTML viewer
```

## 🐍 Python Integration

The package includes a Python script for enhanced Excel export:

```bash
# Install required packages
pip install openpyxl pillow pandas

# Convert CSV to Excel with embedded images
python convert_csv_to_excel.py input.csv output.xlsx
```

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## 🙏 Acknowledgments

- [VRM Consortium](https://vrm.dev/) for the VRM specification
- [UniVRM](https://github.com/vrm-c/UniVRM) for VRM implementation in Unity
- [Cysharp](https://github.com/Cysharp/UniTask) for the excellent UniTask library