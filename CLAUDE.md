# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity project focused on VRM (Virtual Reality Model) file processing and expression data extraction. The project uses Unity's VRM10 package to work with VRM avatar files and provides tools for extracting, previewing, and exporting expression data.

## Key Dependencies

- Unity Editor (project appears to be Unity 2022.3 or compatible)
- UniVRM10 package (com.vrmc.vrm: 0.129.2)
- UniTask (for async operations)
- Unity Test Framework

## Common Development Commands

### Unity Editor Operations
Since this is a Unity project, most operations are performed through the Unity Editor:

1. **Open the project**: Open Unity Hub and load the project from this directory
2. **Access VRM Tools**: In Unity Editor menu bar, go to `VRM Tools > Expression Extractor`
3. **Run the scene**: Open `Assets/Scenes/SampleScene.unity` and press Play

### Code Compilation
Unity automatically compiles C# scripts when:
- Files are saved in the Assets folder
- Unity Editor regains focus
- Manually triggered via `Assets > Reimport All`

### Testing
To run tests in Unity:
1. Open Test Runner: `Window > General > Test Runner`
2. Switch between Edit Mode and Play Mode tests
3. Click "Run All" or run specific tests

## Architecture Overview

### Core Components

1. **VRMExpressionExtractor** (`Assets/Editor/VRMExpressionExtractor.cs`)
   - Main EditorWindow that provides UI for expression extraction
   - Handles VRM file loading and expression discovery
   - Coordinates the extraction process

2. **VRMExpressionData** (`Assets/Editor/VRMExpressionData.cs`)
   - Data structures for storing expression information
   - Contains classes for: VRMExpressionData, ExpressionInfo, BlendShapeInfo, MaterialColorInfo, MaterialUVInfo

3. **VRMPreviewCapture** (`Assets/Editor/VRMPreviewCapture.cs`)
   - Utility for capturing preview images of expressions
   - Sets up camera and rendering environment
   - Applies expressions and captures screenshots

4. **ExcelExporter** (`Assets/Editor/ExcelExporter.cs`)
   - Exports expression data to CSV format
   - Creates summary files and character detail files
   - Generates HTML viewer for visual inspection

### Data Flow

1. User places VRM files in the Assets folder
2. Opens VRM Expression Extractor window from menu
3. Tool scans for VRM files and extracts expression data
4. Optional: Captures preview images for each expression
5. Data can be exported to CSV/Excel format with HTML viewer

### Key Concepts

- **VRM Expressions**: Facial expressions defined in VRM files, including blend shapes and material modifications
- **Editor Tools**: All tools are Unity Editor extensions (not runtime code)
- **Data Persistence**: Extracted data is saved as Unity ScriptableObjects or exported to CSV

## Important Notes

- This is an Editor-only toolset - code in the Editor folder only runs in Unity Editor, not in builds
- VRM files should be imported into the Assets folder before processing
- The project uses the VRM 1.0 specification (UniVRM10 namespace)