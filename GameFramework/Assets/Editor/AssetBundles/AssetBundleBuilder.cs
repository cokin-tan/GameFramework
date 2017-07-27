using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public static class AssetBundleBuilder
{
    public class BuildItemConfig
    {
        public string name = string.Empty;
        public bool isFolder = true;
        public string filter = string.Empty;
        public bool recursion = false;
    }

    public static BuildTarget CurrentBuildTarget
    {
        get
        {
            return EditorUserBuildSettings.activeBuildTarget;
        }
    }

    public static string BuildOutputPath
    {
        get
        {
            string path = Path.Combine(Application.dataPath, "../BuildOutputPath");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            path = Path.Combine(path, CurrentBuildTarget.ToString());
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }
    }

    public static BuildItemConfig[] buildItemConfig = new BuildItemConfig[] 
    {
        new BuildItemConfig(){ name = AssetExt.FIELD, isFolder = true, filter = "*.unity", recursion = false },
        new BuildItemConfig(){ name = AssetExt.TEXTURES, isFolder = true, filter = "*.png", recursion = false },
        new BuildItemConfig(){ name = AssetExt.UI, isFolder = true, filter = "*.prefab", recursion = false }
    };

    public static void RemoveAllAssetBundleNames()
    {
        string[] bundleNames = AssetDatabase.GetAllAssetBundleNames();
        foreach (string name in bundleNames)
        {
            AssetDatabase.RemoveAssetBundleName(name, true);
        }
    }

    public static void GenerateBundles(BuildItemConfig[] buildItemConfig, string rootPath)
    {
        EditorUtility.DisplayProgressBar("Generate Resource", "Gather resources...", 1);

        List<AssetBundleBuild> resourcesLst = new List<AssetBundleBuild>();
        foreach (var item in buildItemConfig)
        {
            string bundleRootPath = Path.Combine(rootPath, item.name);
            string[] entries = item.isFolder ?
                Directory.GetDirectories(bundleRootPath, item.filter, item.recursion ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly) :
                Directory.GetFiles(bundleRootPath, item.filter, item.recursion ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            foreach (var tempEntry in entries)
            {
                string entry = tempEntry.Replace("\\", "/");
                if (!item.isFolder)
                {
                    string extension = Path.GetExtension(entry).ToLower();
                    if (extension == ".meta" || extension == ".ds_store")
                    {
                        continue;
                    }
                }

                string path = entry;
                int subIndex = path.IndexOf("Assets");
                path = path.Substring(subIndex + 7);
                if (!item.isFolder)
                {
                    path = path.Substring(0, path.LastIndexOf('.'));
                }

                List<string> assetLst = new List<string>();
                if (!item.isFolder)
                {
                    assetLst.Add(entry.Substring(subIndex));
                }
                else
                {
                    string[] files = Directory.GetFiles(entry);
                    foreach (var file in files)
                    {
                        string extension = Path.GetExtension(file).ToLower();
                        if (extension == ".meta" || extension == ".ds_store")
                        {
                            continue;
                        }
                        assetLst.Add(file.Substring(subIndex).Replace("\\", "/"));
                    }
                    assetLst.Sort((lhs, rhs) => { return lhs.CompareTo(rhs); });
                }

                resourcesLst.Add(new AssetBundleBuild()
                {
                     assetBundleName = string.Format("{0}.{1}", path, item.name),
                     assetNames = assetLst.ToArray()
                });
            }
        }

        EditorUtility.DisplayProgressBar("Generate resource", "Build resources...", 1);
        resourcesLst.Sort((lhs, rhs) => { return lhs.assetBundleName.CompareTo(rhs.assetBundleName); });
        BuildPipeline.BuildAssetBundles(BuildOutputPath, resourcesLst.ToArray(), BuildAssetBundleOptions.ChunkBasedCompression, CurrentBuildTarget);

        AssetBundle manifestBundle = null;
        AssetBundleManifest manifest = null;
        try
        {
            byte[] bytes = File.ReadAllBytes(Path.Combine(BuildOutputPath, CurrentBuildTarget.ToString()));
        }
        catch(System.Exception e)
        {

        }
        finally
        {
            if (null != manifestBundle)
            {
                manifestBundle.Unload(false);
                manifestBundle = null;
            }
        }
    }

    public static void BuildAsetsBundle(BuildItemConfig[] buildItemConfig, string rootPath)
    {
        RemoveAllAssetBundleNames();
        GenerateBundles(buildItemConfig, rootPath);
    }

    public static bool BuildAssetsBundle()
    {
        BuildAsetsBundle(buildItemConfig, Path.Combine(Application.dataPath, "data"));
        return true;
    }
}

public class AssetBundleBuilderWindow : EditorWindow
{
    #region attributes
    public static string DefineSymbols
    {
        get
        {
            return PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        }
        set
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, value);
        }
    }
    #endregion

    [MenuItem("AssetBundle/Builder")]
    public static void CreateWindow()
    {
        var window = EditorWindow.GetWindow<AssetBundleBuilderWindow>();
        window.minSize = new Vector2(400, 600);
    }

    void OnGUI()
    {
        EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Platform:", AssetBundleBuilder.CurrentBuildTarget.ToString());
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.LabelField("DefineSymbols:", DefineSymbols);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();
            if (GUILayout.Button("Build Asset Bundle"))
            {
                AssetBundleBuilder.BuildAssetsBundle();
            }
        EditorGUILayout.EndVertical();
    }
}

