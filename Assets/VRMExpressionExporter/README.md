# VRM Expression Exporter

A comprehensive Unity toolset for extracting, visualizing, and exporting VRM expression data.

## Features

- **Expression Extraction**: Extract all expression data from VRM files including blend shapes and material modifications
- **Preview Capture**: Automatically capture preview images for each expression
- **Multiple Export Formats**: Export to CSV, Excel (with embedded images), and HTML viewer
- **AnimationClip Generation**: Create Unity AnimationClips from VRM expressions
- **Batch Processing**: Process multiple VRM files at once
- **Customizable Options**: Filter expressions, exclude mouth blend shapes, and more

## Installation

### Via Unity Package Manager

1. Open the Package Manager window (Window > Package Manager)
2. Click the + button and select "Add package from git URL..."
3. Enter: `https://github.com/YOUR_REPO/VRMExpressionExporter.git`

### Via manifest.json

Add the following to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.voltaction.vrmexpressionexporter": "https://github.com/YOUR_REPO/VRMExpressionExporter.git"
  }
}
```

### Dependencies

This package requires:
- Unity 2022.3 or later
- UniVRM (com.vrmc.vrm) 0.129.2 or later
- UniTask 2.3.3 or later

## Usage

### VRM Expression Exporter

1. Import VRM files into your Unity project
2. Open the tool: `VRM Expression Exporter > Expression Exporter`
3. Add VRM objects by dragging them from the hierarchy or project window
4. Configure export options:
   - **CSV Export**: Export expression data to CSV format
   - **Preview Images**: Capture preview images for each expression
   - **AnimationClips**: Generate Unity AnimationClips
   - **Exclude Mouth Blend Shapes**: Option to exclude mouth-related blend shapes
5. Click "Export" to process all VRM files

### CSV to Excel Converter

1. Generate CSV files using the exporter
2. Open the tool: `VRM Expression Exporter > Convert CSV to Excel`
3. Select CSV file and convert
4. The tool will embed preview images into the Excel file

## Output Formats

### CSV Format
- Character name, expression name, and all blend shape values
- Compatible with Excel and other spreadsheet applications

### Excel Format
- Same data as CSV with embedded preview images
- Requires Python with openpyxl and Pillow packages

### HTML Viewer
- Visual gallery of all expressions with preview images
- Searchable and filterable interface

### AnimationClips
- Unity AnimationClip files for each expression
- Can be used in Animation Controllers and Timeline
- Option to generate versions with and without mouth blend shapes

## API Usage

```csharp
using VRMExpressionExporter;
using VRMExpressionExporter.Editor;

// Use the exporter programmatically
var exporter = new VRMExpressionExporter();
// Configure and use as needed

// Export to CSV
var excelExporter = new ExcelExporter();
excelExporter.ExportToCSV(expressionData, outputPath);

// Capture preview
var capture = new VRMPreviewCapture();
var texture = capture.CaptureExpression(vrmGameObject, expression);
```

## License

MIT License - See LICENSE.md for details

## Support

For issues and feature requests, please visit: [Your repository issues page]