using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class DetectiveRenameTool : EditorWindow
{
    private string rootFolder = "Assets";
    private bool includeSubfolders = true;
    private bool backupOriginals = true;
    private bool renameFiles = true;
    private bool dryRun = true;

    private Vector2 scroll;
    private List<FileChange> changes = new List<FileChange>();
    private string lastRunSummary = "";

    // Replacement rules (ordered)
    private static readonly (string pattern, string replacement, bool isRegex)[] rules = new[]
    {
        // specific namespace mappings first
        ("namespace Eduzo.Games.DetectiveGame.UI", "namespace Eduzo.Games.DetectiveGame.UI", false),
        ("namespace Eduzo.Games.DetectiveGame.Data", "namespace Eduzo.Games.DetectiveGame.Data", false),
        ("namespace Eduzo.Games.DetectiveGame", "namespace Eduzo.Games.DetectiveGame", true), // regex to allow word boundary variants
        ("using Eduzo.Games.DetectiveGame.UI", "using Eduzo.Games.DetectiveGame.UI", false),
        ("using Eduzo.Games.DetectiveGame.Data", "using Eduzo.Games.DetectiveGame.Data", false),
        // generic identifier replacement with word boundaries
        (@"\bCarToll\b", "DetectiveGame", true)
    };

    [MenuItem("Tools/Detective Rename Tool")]
    public static void ShowWindow() => GetWindow<DetectiveRenameTool>("Detective Rename");

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("Replaces 'DetectiveGame' → 'DetectiveGame' across scripts. Use Preview before Apply. Renaming files will use AssetDatabase.MoveAsset to preserve .meta GUIDs. The tool will also ensure the class name matches the file name and set the namespace to Eduzo.Games.DetectiveGame.", MessageType.Info);

        EditorGUILayout.BeginVertical("box");
        rootFolder = EditorGUILayout.TextField("Root Folder", rootFolder);
        includeSubfolders = EditorGUILayout.Toggle("Include Subfolders", includeSubfolders);
        backupOriginals = EditorGUILayout.Toggle("Backup Originals", backupOriginals);
        renameFiles = EditorGUILayout.Toggle("Rename Files (DetectiveGame -> DetectiveGame)", renameFiles);
        dryRun = EditorGUILayout.Toggle("Dry Run (no writes)", dryRun);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Scan / Preview")) { ScanAndPreview(); }
        if (GUILayout.Button("Apply Changes")) { ApplyChanges(); }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Preview - {changes.Count} file(s) to change", EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(250));
        foreach (var c in changes)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(c.RelativePath, EditorStyles.boldLabel);
            if (c.WillRenameFile)
                EditorGUILayout.LabelField($"Rename: {c.FileName} -> {c.NewFileName}");
            EditorGUILayout.LabelField($"Text changes: {(c.WillChangeContent ? "Yes" : "No")}");
            EditorGUILayout.LabelField($"Preview snippet:");
            EditorGUILayout.TextArea(c.PreviewSnippet, GUILayout.Height(60));
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(lastRunSummary, MessageType.None);

        if (!dryRun)
        {
            EditorGUILayout.HelpBox("Applying changes will modify files. If renaming files, AssetDatabase.MoveAsset is used to preserve .meta GUIDs but class renames can still break serialized component links. Backup option is recommended.", MessageType.Warning);
        }
    }

    private void ScanAndPreview()
    {
        changes.Clear();
        lastRunSummary = "";

        if (!Directory.Exists(rootFolder))
        {
            EditorUtility.DisplayDialog("Error", $"Root folder does not exist: {rootFolder}", "OK");
            return;
        }

        string[] csFiles = Directory.GetFiles(rootFolder, "*.cs", includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

        foreach (var file in csFiles)
        {
            try
            {
                string original = File.ReadAllText(file, Encoding.UTF8);
                string updated = ApplyRules(original);

                bool willChangeContent = !string.Equals(original, updated, StringComparison.Ordinal);
                string fileName = Path.GetFileName(file);
                bool willRenameFile = renameFiles && fileName.Contains("DetectiveGame", StringComparison.Ordinal);

                   var preview = BuildPreviewSnippet(original, updated);

                if (willChangeContent || willRenameFile)
                {
                    changes.Add(new FileChange
                    {
                        FullPath = file,
                        RelativePath = ToProjectRelativePath(file),
                        OriginalContent = original,
                        UpdatedContent = updated,
                        WillChangeContent = willChangeContent,
                        FileName = fileName,
                        NewFileName = willRenameFile ? fileName.Replace("DetectiveGame", "DetectiveGame") : fileName,
                        WillRenameFile = willRenameFile,
                        PreviewSnippet = preview
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error reading {file}: {ex.Message}");
            }
        }

        lastRunSummary = $"Scan complete. {changes.Count} file(s) would be modified.";
        Debug.Log(lastRunSummary);
    }

    private void ApplyChanges()
    {
        if (changes.Count == 0)
        {
            EditorUtility.DisplayDialog("No Changes", "No files to change. Run Scan / Preview first.", "OK");
            return;
        }

        if (dryRun)
        {
            EditorUtility.DisplayDialog("Dry Run", "Dry-run is enabled. No files will be changed. Uncheck Dry Run to apply.", "OK");
            return;
        }

        string backupFolder = "";
        if (backupOriginals)
        {
            backupFolder = CreateBackupFolder();
            Debug.Log($"Backup will be saved to: {backupFolder}");
        }

        int modifiedCount = 0;
        int renamedCount = 0;

        try
        {
            foreach (var change in changes)
            {
                // Backup original content if requested
                if (backupOriginals)
                {
                    string dest = Path.Combine(backupFolder, change.RelativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    File.Copy(change.FullPath, dest, true);
                }

                // Write updated content if needed
                if (change.WillChangeContent)
                {
                    File.WriteAllText(change.FullPath, change.UpdatedContent, Encoding.UTF8);
                    modifiedCount++;
                }

                // Rename file using AssetDatabase to preserve meta GUID if requested
                if (change.WillRenameFile)
                {
                    string assetPath = change.RelativePath.Replace('\\', '/');
                    string newAssetPath = Path.Combine(Path.GetDirectoryName(assetPath) ?? "", change.NewFileName).Replace('\\', '/');

                    // Use AssetDatabase.MoveAsset to preserve .meta
                    string error = AssetDatabase.MoveAsset(assetPath, newAssetPath);
                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.LogWarning($"Failed to move asset {assetPath} -> {newAssetPath}: {error}");
                    }
                    else
                    {
                        renamedCount++;
                    }
                }
            }

            // Refresh AssetDatabase to pick up changes & moved assets
            AssetDatabase.Refresh();

            lastRunSummary = $"Applied changes. Modified contents: {modifiedCount}, Renamed files: {renamedCount}. Backup: {(backupOriginals ? backupFolder : "none")}";
            Debug.Log(lastRunSummary);
            EditorUtility.DisplayDialog("Done", lastRunSummary, "OK");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error applying changes: {ex.Message}");
            EditorUtility.DisplayDialog("Error", $"Error applying changes: {ex.Message}", "OK");
        }
    }

    private static string ApplyRules(string input)
    {
        string result = input;
        foreach (var (pattern, replacement, isRegex) in rules)
        {
            if (isRegex)
            {
                result = Regex.Replace(result, pattern, replacement);
            }
            else
            {
                result = result.Replace(pattern, replacement);
            }
        }
        return result;
    }

    private static string BuildPreviewSnippet(string original, string updated, int context = 3)
    {
        if (original == updated) return "(no text changes)";

        // Find first diff index
        int idx = FirstDiffIndex(original, updated);
        if (idx < 0) idx = 0;

        // Get context window
        int start = Math.Max(0, idx - 100);
        int len = Math.Min(400, updated.Length - start);

        string snippet = updated.Substring(start, len);
        return snippet.Replace("\r\n", "\n");
    }

    private static int FirstDiffIndex(string a, string b)
    {
        int len = Math.Min(a.Length, b.Length);
        for (int i = 0; i < len; i++)
            if (a[i] != b[i]) return i;
        if (a.Length != b.Length) return len;
        return -1;
    }

    private string CreateBackupFolder()
    {
        string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string backupRoot = Path.Combine(Application.dataPath, "..", "DetectiveRenameBackup", ts);
        backupRoot = Path.GetFullPath(backupRoot);
        Directory.CreateDirectory(backupRoot);
        return backupRoot;
    }

    private static string ToProjectRelativePath(string fullPath)
    {
        fullPath = Path.GetFullPath(fullPath);
        string proj = Path.GetFullPath(Directory.GetCurrentDirectory());
        if (fullPath.StartsWith(proj))
        {
            return fullPath.Substring(proj.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace(Path.DirectorySeparatorChar, '/');
        }
        return fullPath;
    }

    private class FileChange
    {
        public string FullPath;
        public string RelativePath;
        public string OriginalContent;
        public string UpdatedContent;
        public bool WillChangeContent;
        public bool WillRenameFile;
        public string FileName;
        public string NewFileName;
        public string PreviewSnippet;
    }
}
