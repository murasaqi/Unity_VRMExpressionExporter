using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEditor;
using VRMExpressionExporter;

namespace VRMExpressionExporter.Editor
{
    /// <summary>
    /// CSVファイルを画像埋め込みExcelファイルに変換するツール
    /// </summary>
    public class CSVToExcelConverter : EditorWindow
    {
        private string csvFilePath = "Assets/VRMExpressionList.csv";
        private string pythonScriptPath = "";
        private string outputPath = "";
        private string pythonExecutable = "python3";
        private bool useCustomPython = false;
        private Vector2 scrollPosition;
        
        [MenuItem("VRM Expression Exporter/Convert CSV to Excel")]
        private static void ShowWindow()
        {
            var window = GetWindow<CSVToExcelConverter>();
            window.titleContent = new GUIContent("CSV to Excel 変換");
            window.minSize = new Vector2(500, 400);
            window.Initialize();
            window.Show();
        }
        
        private void OnEnable()
        {
            Initialize();
        }
        
        private void Initialize()
        {
            // Pythonスクリプトのパスを自動的に見つける
            string[] guids = AssetDatabase.FindAssets("convert_csv_to_excel t:DefaultAsset");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("convert_csv_to_excel.py"))
                {
                    pythonScriptPath = path;
                    break;
                }
            }
            
            if (string.IsNullOrEmpty(pythonScriptPath))
            {
                // フォールバック: パッケージ内の既知のパスを試す
                pythonScriptPath = "Assets/VRMExpressionExporter/Editor/convert_csv_to_excel.py";
            }
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            EditorGUILayout.LabelField("CSV to Excel 変換ツール", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox(
                "CSVファイルを画像が埋め込まれたExcelファイルに変換します。\n" +
                "事前にPythonと必要なライブラリ（openpyxl, Pillow）をインストールしてください。",
                MessageType.Info);
            
            EditorGUILayout.Space();
            
            // 入力ファイル設定
            EditorGUILayout.LabelField("入力設定", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            csvFilePath = EditorGUILayout.TextField("CSVファイル:", csvFilePath);
            if (GUILayout.Button("選択", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFilePanel("CSVファイルを選択", "Assets", "csv");
                if (!string.IsNullOrEmpty(path))
                {
                    // Assetsフォルダからの相対パスに変換
                    if (path.StartsWith(Application.dataPath))
                    {
                        csvFilePath = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        csvFilePath = path;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // 出力ファイル設定
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("出力設定", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            outputPath = EditorGUILayout.TextField("出力ファイル (オプション):", outputPath);
            if (GUILayout.Button("選択", GUILayout.Width(60)))
            {
                string path = EditorUtility.SaveFilePanel("出力先を選択", "Assets", "VRMExpressionList_with_images", "xlsx");
                if (!string.IsNullOrEmpty(path))
                {
                    outputPath = path;
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("空欄の場合、CSVファイルと同じ場所に「_with_images.xlsx」として保存されます。", MessageType.None);
            
            // Python設定
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Python設定", EditorStyles.boldLabel);
            
            // Pythonスクリプトパスを表示（デバッグ用）
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Pythonスクリプト:", pythonScriptPath);
            EditorGUI.EndDisabledGroup();
            
            useCustomPython = EditorGUILayout.Toggle("カスタムPythonパスを使用", useCustomPython);
            if (useCustomPython)
            {
                EditorGUILayout.BeginHorizontal();
                pythonExecutable = EditorGUILayout.TextField("Pythonパス:", pythonExecutable);
                if (GUILayout.Button("選択", GUILayout.Width(60)))
                {
                    string path = EditorUtility.OpenFilePanel("Pythonを選択", "/usr/local/bin", "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        pythonExecutable = path;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            
            // インストール手順
            EditorGUILayout.Space();
            if (GUILayout.Button("必要なライブラリのインストール方法を表示"))
            {
                ShowInstallationInstructions();
            }
            
            EditorGUILayout.Space();
            
            // 実行ボタン
            GUI.enabled = File.Exists(csvFilePath) && File.Exists(pythonScriptPath);
            if (GUILayout.Button("Excelファイルに変換", GUILayout.Height(30)))
            {
                ConvertToExcel();
            }
            GUI.enabled = true;
            
            // ステータス表示
            if (!File.Exists(csvFilePath))
            {
                EditorGUILayout.HelpBox("CSVファイルが見つかりません。", MessageType.Warning);
            }
            
            if (!File.Exists(pythonScriptPath))
            {
                EditorGUILayout.HelpBox($"Pythonスクリプトが見つかりません: {pythonScriptPath}", MessageType.Error);
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void ConvertToExcel()
        {
            try
            {
                // 絶対パスに変換
                string absoluteCsvPath = Path.GetFullPath(csvFilePath);
                string absoluteScriptPath = Path.GetFullPath(pythonScriptPath);
                
                // Pythonスクリプトを実行
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = useCustomPython ? pythonExecutable : "python3",
                    Arguments = $"\"{absoluteScriptPath}\" \"{absoluteCsvPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                // 出力パスが指定されている場合
                if (!string.IsNullOrEmpty(outputPath))
                {
                    startInfo.Arguments += $" -o \"{outputPath}\"";
                }
                
                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    
                    if (process.ExitCode == 0)
                    {
                        UnityEngine.Debug.Log($"Excel変換成功:\n{output}");
                        EditorUtility.DisplayDialog("成功", "Excelファイルの作成が完了しました。", "OK");
                        
                        // 出力ファイルを開く
                        string excelPath = string.IsNullOrEmpty(outputPath) 
                            ? absoluteCsvPath.Replace(".csv", "_with_images.xlsx")
                            : outputPath;
                        
                        if (File.Exists(excelPath))
                        {
                            EditorUtility.RevealInFinder(excelPath);
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"Excel変換エラー:\n{error}");
                        EditorUtility.DisplayDialog("エラー", 
                            $"変換中にエラーが発生しました。\n\n{error}", "OK");
                    }
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Excel変換例外: {e.Message}");
                EditorUtility.DisplayDialog("エラー", 
                    $"Pythonスクリプトの実行に失敗しました。\n\n{e.Message}\n\n" +
                    "Pythonがインストールされているか確認してください。", "OK");
            }
        }
        
        private void ShowInstallationInstructions()
        {
            string message = "以下のコマンドを実行して必要なライブラリをインストールしてください：\n\n" +
                "1. ターミナル（Terminal）を開く\n" +
                "2. 以下のコマンドを実行:\n\n" +
                "pip install openpyxl Pillow\n\n" +
                "または:\n\n" +
                "pip3 install openpyxl Pillow\n\n" +
                "注意: Pythonがインストールされていない場合は、\n" +
                "https://www.python.org/ からインストールしてください。";
                
            EditorUtility.DisplayDialog("インストール手順", message, "OK");
        }
    }
}