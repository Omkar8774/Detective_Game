using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class ClassNameSyncTool : EditorWindow
{
    private string rootFolder = "Assets";
    private bool includeSubfolders = true;
    private bool backupOriginals = true;
    private bool updateReferences = true;
    private bool dryRun = true;

    private Vector2 scroll;
    private List<ClassChange> changes = new List<ClassChange>();
    private string lastRunSummary = "";

    [MenuItem("Tools/Class Name Sync")]
    public static void ShowWindow() => GetWindow<ClassNameSyncTool>("Class Name Sync");

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("Ensure each C# file's primary top-level type name matches the file name. When 'Update References' is enabled the tool will also attempt to update identifier references across .cs files (heuristic: replaces only identifiers outside comments/strings). Preview carefully; replacements can still be imperfect.", MessageType.Info);

        EditorGUILayout.BeginVertical("box");
        rootFolder = EditorGUILayout.TextField("Root Folder", rootFolder);
        includeSubfolders = EditorGUILayout.Toggle("Include Subfolders", includeSubfolders);
        backupOriginals = EditorGUILayout.Toggle("Backup Originals", backupOriginals);
        updateReferences = EditorGUILayout.Toggle("Update References Across Project", updateReferences);
        dryRun = EditorGUILayout.Toggle("Dry Run (no writes)", dryRun);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Scan / Preview")) { ScanAndPreview(); }
        if (GUILayout.Button("Apply Changes")) { ApplyChanges(); }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Preview - {changes.Count} file(s) with changes", EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(300));
        foreach (var c in changes)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(c.RelativePath, EditorStyles.boldLabel);
            if (!string.IsNullOrEmpty(c.PrimaryOldName))
            {
                EditorGUILayout.LabelField($"Primary type rename: {c.PrimaryOldName} -> {c.PrimaryNewName}");
            }
            if (c.ReferenceReplacements != null && c.ReferenceReplacements.Count > 0)
            {
                EditorGUILayout.LabelField($"Reference replacements: {c.ReferenceReplacements.Count} occurrence(s)");
            }
            EditorGUILayout.LabelField($"Will change file content: {(c.WillChangeContent ? "Yes" : "No")}");
            EditorGUILayout.LabelField($"Preview snippet:");
            EditorGUILayout.TextArea(c.PreviewSnippet, GUILayout.Height(60));
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(lastRunSummary, MessageType.None);

        if (!dryRun)
        {
            EditorGUILayout.HelpBox("Applying changes will modify files. Changing class names and updating references can break serialized links or cause compile errors if replacements are not comprehensive. Use version control and backups.", MessageType.Warning);
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

        // First pass: detect primary-type renames
        var primaryRenames = new List<(string filePath, string oldName, string newName, string ns, string updatedContent, string originalContent)>();
        foreach (var file in csFiles)
        {
            try
            {
                string original = File.ReadAllText(file, Encoding.UTF8);
                string fileName = Path.GetFileNameWithoutExtension(file);
                var (updated, originalTypeName, changed, ns) = ReplaceFirstTopLevelTypeNameWithNamespace(original, fileName);

                if (changed)
                {
                    primaryRenames.Add((file, originalTypeName, fileName, ns, updated, original));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error reading {file}: {ex.Message}");
            }
        }

        // Build mapping dictionary (old -> new) including qualified names
        var mapping = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pr in primaryRenames)
        {
            if (string.IsNullOrEmpty(pr.oldName)) continue;
            if (!mapping.ContainsKey(pr.oldName))
                mapping[pr.oldName] = pr.newName;

            if (!string.IsNullOrEmpty(pr.ns))
            {
                string qualifiedOld = pr.ns + "." + pr.oldName;
                string qualifiedNew = pr.ns + "." + pr.newName;
                if (!mapping.ContainsKey(qualifiedOld))
                    mapping[qualifiedOld] = qualifiedNew;
            }
        }

        // Second pass: create change entries for primary files and for any file that will have reference replacements
        var fileUpdates = new Dictionary<string, string>(StringComparer.Ordinal); // path -> new content
        var fileOriginals = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in csFiles)
        {
            try
            {
                string original = File.ReadAllText(file, Encoding.UTF8);
                fileOriginals[file] = original;
                string updated = original;

                // If file is a primary rename target, start from its precomputed updated content
                var prMatch = primaryRenames.Find(p => p.filePath == file);
                if (prMatch.filePath != null)
                {
                    updated = prMatch.updatedContent;
                }

                // Apply reference replacements if requested, but only outside strings/comments
                if (updateReferences && mapping.Count > 0)
                {
                    updated = ReplaceIdentifiersOutsideStringsAndComments(updated, mapping);
                }

                if (!string.Equals(original, updated, StringComparison.Ordinal))
                {
                    fileUpdates[file] = updated;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing {file}: {ex.Message}");
            }
        }

        // Build preview entries
        foreach (var kv in fileUpdates)
        {
            string file = kv.Key;
            string updated = kv.Value;
            string original = fileOriginals[file];
            var pr = primaryRenames.Find(p => p.filePath == file);
            var preview = BuildPreviewSnippet(original, updated);
            var cc = new ClassChange
            {
                FullPath = file,
                RelativePath = ToProjectRelativePath(file),
                OriginalContent = original,
                UpdatedContent = updated,
                FileName = Path.GetFileName(file),
                WillChangeContent = true,
                PreviewSnippet = preview,
                ReferenceReplacements = new List<(string oldName, string newName)>()
            };

            if (pr.filePath != null)
            {
                cc.PrimaryOldName = pr.oldName;
                cc.PrimaryNewName = pr.newName;
            }

            // detect which mappings affected this file (only count occurrences outside comments/strings)
            foreach (var map in mapping)
            {
                if (ContainsIdentifierOutsideStringsAndComments(original, map.Key))
                {
                    // Avoid listing the primary mapping as a reference replacement for the same file (already captured)
                    if (cc.PrimaryOldName == map.Key) continue;
                    cc.ReferenceReplacements.Add((map.Key, map.Value));
                }
            }

            changes.Add(cc);
        }

        lastRunSummary = $"Scan complete. {changes.Count} file(s) would be modified (including reference updates).";
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

                // Write updated content
                if (change.WillChangeContent)
                {
                    File.WriteAllText(change.FullPath, change.UpdatedContent, Encoding.UTF8);
                    modifiedCount++;
                }
            }

            AssetDatabase.Refresh();

            lastRunSummary = $"Applied changes. Modified contents: {modifiedCount}. Backup: {(backupOriginals ? backupFolder : "none")}";
            Debug.Log(lastRunSummary);
            EditorUtility.DisplayDialog("Done", lastRunSummary, "OK");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error applying changes: {ex.Message}");
            EditorUtility.DisplayDialog("Error", $"Error applying changes: {ex.Message}", "OK");
        }
    }

    // Replaces the first top-level type declaration's identifier to match desiredName and returns namespace if found.
    // Returns (updatedContent, originalTypeName, changed, namespace)
    private static (string updated, string originalTypeName, bool changed, string ns) ReplaceFirstTopLevelTypeNameWithNamespace(string source, string desiredName)
    {
        // Find namespace (first occurrence)
        string ns = null;
        var nsMatch = Regex.Match(source, @"\bnamespace\s+([A-Za-z0-9_.]+)", RegexOptions.Multiline);
        if (nsMatch.Success)
        {
            ns = nsMatch.Groups[1].Value;
        }

        // Find top-level type declaration
        var pattern = @"(^|\r?\n)([ \t]*)(?:public|internal|protected|private|static|sealed|abstract|partial|\s)*\b(class|struct|interface)\b\s+([A-Za-z_][A-Za-z0-9_]*)";
        var rx = new Regex(pattern, RegexOptions.Multiline);
        var matches = rx.Matches(source);
        if (matches.Count == 0) return (source, null, false, ns);

        // Choose the match with the smallest indentation length (likely top-level)
        Match chosen = null;
        int bestIndent = int.MaxValue;
        foreach (Match m in matches)
        {
            string indent = m.Groups[2].Value ?? "";
            int indentLen = indent.Length;
            if (indentLen < bestIndent)
            {
                chosen = m;
                bestIndent = indentLen;
            }
        }

        if (chosen == null) return (source, null, false, ns);

        string originalName = chosen.Groups[4].Value;
        if (string.Equals(originalName, desiredName, StringComparison.Ordinal)) return (source, originalName, false, ns);

        int idStart = chosen.Groups[4].Index;
        int idLen = chosen.Groups[4].Length;

        var sb = new StringBuilder();
        sb.Append(source.Substring(0, idStart));
        sb.Append(desiredName);
        sb.Append(source.Substring(idStart + idLen));

        return (sb.ToString(), originalName, true, ns);
    }

    // Replace identifiers only outside of comments and string literals.
    private static string ReplaceIdentifiersOutsideStringsAndComments(string source, Dictionary<string, string> mapping)
    {
        if (mapping == null || mapping.Count == 0) return source;

        // Find spans of strings and comments so we can skip them.
        var tokenPattern = new Regex(@"""([^""\\]|\\.)*""|@""(""""|[^""])*""|//.*?$|/\*[\s\S]*?\*/", RegexOptions.Singleline | RegexOptions.Multiline);
        var keys = mapping.Keys.OrderByDescending(k => k.Length).ToList();

        var sb = new StringBuilder();
        int lastIndex = 0;
        foreach (Match m in tokenPattern.Matches(source))
        {
            // process segment before token
            if (m.Index > lastIndex)
            {
                string segment = source.Substring(lastIndex, m.Index - lastIndex);
                segment = ReplaceIdentifiersInSegment(segment, keys, mapping);
                sb.Append(segment);
            }

            // append token unchanged
            sb.Append(m.Value);
            lastIndex = m.Index + m.Length;
        }

        // process tail
        if (lastIndex < source.Length)
        {
            string segment = source.Substring(lastIndex);
            segment = ReplaceIdentifiersInSegment(segment, keys, mapping);
            sb.Append(segment);
        }

        return sb.ToString();
    }

    // Helper: replace identifiers in a non-comment/string segment.
    private static string ReplaceIdentifiersInSegment(string segment, List<string> keysOrderedByLengthDesc, Dictionary<string, string> mapping)
    {
        if (string.IsNullOrEmpty(segment)) return segment;

        // For each key, do a regex whole-identifier replacement.
        // Use negative/positive lookarounds so '.' in qualified names is handled safely.
        foreach (var key in keysOrderedByLengthDesc)
        {
            string escaped = Regex.Escape(key);
            // identifier boundary: not preceded by letter/digit/underscore/dot, and not followed by letter/digit/underscore
            string pattern = @"(?<![A-Za-z0-9_\.])" + escaped + @"(?![A-Za-z0-9_])";
            segment = Regex.Replace(segment, pattern, mapping[key]);
        }

        return segment;
    }

    // Check if identifier exists outside strings/comments
    private static bool ContainsIdentifierOutsideStringsAndComments(string source, string key)
    {
        if (string.IsNullOrEmpty(key)) return false;

        var tokenPattern = new Regex(@"""([^""\\]|\\.)*""|@""(""""|[^""])*""|//.*?$|/\*[\s\S]*?\*/", RegexOptions.Singleline | RegexOptions.Multiline);
        int lastIndex = 0;
        foreach (Match m in tokenPattern.Matches(source))
        {
            if (m.Index > lastIndex)
            {
                string segment = source.Substring(lastIndex, m.Index - lastIndex);
                if (Regex.IsMatch(segment, @"(?<![A-Za-z0-9_\.])" + Regex.Escape(key) + @"(?![A-Za-z0-9_])"))
                    return true;
            }
            lastIndex = m.Index + m.Length;
        }

        if (lastIndex < source.Length)
        {
            string segment = source.Substring(lastIndex);
            if (Regex.IsMatch(segment, @"(?<![A-Za-z0-9_\.])" + Regex.Escape(key) + @"(?![A-Za-z0-9_])"))
                return true;
        }

        return false;
    }

    private static string BuildPreviewSnippet(string original, string updated, int context = 3)
    {
        if (original == updated) return "(no text changes)";

        int idx = FirstDiffIndex(original, updated);
        if (idx < 0) idx = 0;

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
        string backupRoot = Path.Combine(Application.dataPath, "..", "ClassNameSyncBackup", ts);
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

    private class ClassChange
    {
        public string FullPath;
        public string RelativePath;
        public string FileName;
        public string OriginalContent;
        public string UpdatedContent;
        public bool WillChangeContent;
        public string PrimaryOldName;
        public string PrimaryNewName;
        public List<(string oldName, string newName)> ReferenceReplacements;
        public string PreviewSnippet;
    }
}
