using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using VRMExpressionExporter;

namespace VRMExpressionExporter.Editor
{
    /// <summary>
    /// VRM表情データをExcel互換形式（CSV）でエクスポートするクラス
    /// </summary>
    public static class ExcelExporter
    {
        /// <summary>
        /// 抽出したVRM表情データをCSVファイルとしてエクスポート
        /// </summary>
        public static void ExportToCSV(List<VRMExpressionData> expressionDataList, string outputPath)
        {
            if (expressionDataList == null || expressionDataList.Count == 0)
            {
                Debug.LogError("No expression data to export.");
                return;
            }
            
            // 出力ディレクトリを作成
            string exportDir = Path.Combine(outputPath, "ExcelExport");
            if (!Directory.Exists(exportDir))
            {
                Directory.CreateDirectory(exportDir);
            }
            
            // 全体のサマリーファイルを作成
            CreateSummaryFile(expressionDataList, exportDir);
            
            // 各キャラクターの詳細ファイルを作成
            foreach (var characterData in expressionDataList)
            {
                CreateCharacterDetailFile(characterData, exportDir);
            }
            
            // HTMLビューアーも生成
            CreateHTMLViewer(expressionDataList, exportDir);
            
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Export Complete", 
                $"Expression data has been exported to:\n{exportDir}\n\n" +
                "You can open the CSV files in Excel or view the HTML file in a web browser.", 
                "OK");
            
            // エクスプローラー/Finderで開く
            EditorUtility.RevealInFinder(exportDir);
        }
        
        private static void CreateSummaryFile(List<VRMExpressionData> dataList, string outputDir)
        {
            string filePath = Path.Combine(outputDir, "VRM_Expression_Summary.csv");
            
            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                // BOMを追加（Excelで日本語を正しく表示するため）
                writer.Write('\uFEFF');
                
                // ヘッダー
                writer.WriteLine("Character Name,File Name,Total Expressions,Preset Expressions,Custom Expressions,Has Preview Images");
                
                foreach (var data in dataList)
                {
                    int presetCount = data.Expressions.Count(e => e.Type == "Preset");
                    int customCount = data.Expressions.Count(e => e.Type == "Custom");
                    bool hasImages = data.Expressions.Any(e => !string.IsNullOrEmpty(e.PreviewImagePath));
                    
                    writer.WriteLine($"\"{data.CharacterName}\",\"{data.FileName}\",{data.Expressions.Count},{presetCount},{customCount},{(hasImages ? "Yes" : "No")}");
                }
            }
        }
        
        private static void CreateCharacterDetailFile(VRMExpressionData characterData, string outputDir)
        {
            string fileName = $"{characterData.CharacterName}_Expressions.csv";
            string filePath = Path.Combine(outputDir, fileName);
            
            // 全てのブレンドシェイプ名を収集
            HashSet<string> allBlendShapes = new HashSet<string>();
            foreach (var expr in characterData.Expressions)
            {
                foreach (var bs in expr.BlendShapes)
                {
                    string key = $"{bs.RelativePath}:{bs.Name ?? $"Index_{bs.Index}"}";
                    allBlendShapes.Add(key);
                }
            }
            
            List<string> blendShapeColumns = allBlendShapes.OrderBy(x => x).ToList();
            
            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                // BOMを追加
                writer.Write('\uFEFF');
                
                // ヘッダー行を作成
                StringBuilder header = new StringBuilder();
                header.Append("Expression Name,Type,Preview Image Path");
                
                foreach (string bsColumn in blendShapeColumns)
                {
                    header.Append($",\"{bsColumn}\"");
                }
                writer.WriteLine(header.ToString());
                
                // 各表情のデータを書き込み
                foreach (var expression in characterData.Expressions)
                {
                    StringBuilder row = new StringBuilder();
                    row.Append($"\"{expression.Name}\",\"{expression.Type}\",\"{expression.PreviewImagePath ?? ""}\"");
                    
                    // ブレンドシェイプの値を設定
                    Dictionary<string, float> bsValues = new Dictionary<string, float>();
                    foreach (var bs in expression.BlendShapes)
                    {
                        string key = $"{bs.RelativePath}:{bs.Name ?? $"Index_{bs.Index}"}";
                        bsValues[key] = bs.Weight;
                    }
                    
                    // 全てのブレンドシェイプ列に対して値を出力
                    foreach (string bsColumn in blendShapeColumns)
                    {
                        if (bsValues.ContainsKey(bsColumn))
                        {
                            row.Append($",{bsValues[bsColumn]:F2}");
                        }
                        else
                        {
                            row.Append(",0");
                        }
                    }
                    
                    writer.WriteLine(row.ToString());
                }
            }
        }
        
        private static void CreateHTMLViewer(List<VRMExpressionData> dataList, string outputDir)
        {
            string htmlPath = Path.Combine(outputDir, "VRM_Expression_Viewer.html");
            
            StringBuilder html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"ja\">");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset=\"UTF-8\">");
            html.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            html.AppendLine("    <title>VRM Expression Viewer</title>");
            html.AppendLine("    <style>");
            html.AppendLine("        body { font-family: Arial, sans-serif; margin: 20px; background-color: #f5f5f5; }");
            html.AppendLine("        .character { background: white; padding: 20px; margin-bottom: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
            html.AppendLine("        .character h2 { color: #333; border-bottom: 2px solid #4CAF50; padding-bottom: 10px; }");
            html.AppendLine("        .expression-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: 20px; margin-top: 20px; }");
            html.AppendLine("        .expression-card { background: #f9f9f9; border: 1px solid #ddd; border-radius: 4px; padding: 10px; text-align: center; }");
            html.AppendLine("        .expression-card img { max-width: 100%; height: auto; border-radius: 4px; }");
            html.AppendLine("        .expression-name { font-weight: bold; margin-top: 10px; }");
            html.AppendLine("        .expression-type { color: #666; font-size: 0.9em; }");
            html.AppendLine("        .no-image { background: #eee; height: 150px; display: flex; align-items: center; justify-content: center; color: #999; }");
            html.AppendLine("        table { border-collapse: collapse; width: 100%; margin-top: 10px; }");
            html.AppendLine("        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            html.AppendLine("        th { background-color: #4CAF50; color: white; }");
            html.AppendLine("        .blend-shape-value { text-align: right; }");
            html.AppendLine("    </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("    <h1>VRM Expression Viewer</h1>");
            
            foreach (var characterData in dataList)
            {
                html.AppendLine($"    <div class=\"character\">");
                html.AppendLine($"        <h2>{characterData.CharacterName}</h2>");
                html.AppendLine($"        <p>File: {characterData.FileName}</p>");
                html.AppendLine($"        <div class=\"expression-grid\">");
                
                foreach (var expression in characterData.Expressions)
                {
                    html.AppendLine($"            <div class=\"expression-card\">");
                    
                    if (!string.IsNullOrEmpty(expression.PreviewImagePath) && File.Exists(expression.PreviewImagePath))
                    {
                        // 相対パスに変換
                        string relativePath = Path.GetRelativePath(outputDir, expression.PreviewImagePath);
                        relativePath = relativePath.Replace('\\', '/');
                        html.AppendLine($"                <img src=\"{relativePath}\" alt=\"{expression.Name}\">");
                    }
                    else
                    {
                        html.AppendLine($"                <div class=\"no-image\">No Preview</div>");
                    }
                    
                    html.AppendLine($"                <div class=\"expression-name\">{expression.Name}</div>");
                    html.AppendLine($"                <div class=\"expression-type\">{expression.Type}</div>");
                    
                    // ブレンドシェイプの詳細を表示
                    if (expression.BlendShapes.Count > 0)
                    {
                        html.AppendLine("                <details>");
                        html.AppendLine("                    <summary>Blend Shapes</summary>");
                        html.AppendLine("                    <table>");
                        html.AppendLine("                        <tr><th>Name</th><th>Weight</th></tr>");
                        
                        foreach (var bs in expression.BlendShapes.Where(b => b.Weight > 0))
                        {
                            string name = bs.Name ?? $"Index_{bs.Index}";
                            html.AppendLine($"                        <tr><td>{name}</td><td class=\"blend-shape-value\">{bs.Weight:F1}</td></tr>");
                        }
                        
                        html.AppendLine("                    </table>");
                        html.AppendLine("                </details>");
                    }
                    
                    html.AppendLine($"            </div>");
                }
                
                html.AppendLine($"        </div>");
                html.AppendLine($"    </div>");
            }
            
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            
            File.WriteAllText(htmlPath, html.ToString());
        }
    }
}