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

    private static BuildItemConfig[] buildItemConfig = new BuildItemConfig[] 
    {
        new BuildItemConfig(){ name = AssetExt.FIELD, isFolder = false, filter = "*.unity", recursion = true },
        new BuildItemConfig(){ name = AssetExt.TEXTURES, isFolder = true, filter = "*.png", recursion = false },
        new BuildItemConfig(){ name = AssetExt.UI, isFolder = false, filter = "*.prefab", recursion = false },
        new BuildItemConfig(){ name = AssetExt.CONF, isFolder = false, filter = "*.*", recursion = false }
    };

    private static void RemoveAllAssetBundleNames()
    {
        string[] bundleNames = AssetDatabase.GetAllAssetBundleNames();
        foreach (string name in bundleNames)
        {
            AssetDatabase.RemoveAssetBundleName(name, true);
        }
    }

    private static string SerializeJsonFile(ResourceConfig.ResourceData json, string path)
    {
        string jsonText = string.Empty;

        using (StreamWriter sw = new StreamWriter(Application.dataPath + path))
        {
            jsonText = Pathfinding.Serialization.JsonFx.JsonWriter.Serialize(json);
            sw.Write(jsonText);
        }
        AssetDatabase.ImportAsset("Assets" + path, ImportAssetOptions.ForceUpdate);

        return jsonText;
    }

    private static void CopyFileEx(string from, string to)
    {
        string directory = Path.GetDirectoryName(to);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.Copy(from, to, true);
    }

    private static AssetBundleManifest LoadManifest()
    {
        AssetBundle manifestBundle = null;
        AssetBundleManifest manifest = null;
        try
        {
            byte[] bytes = File.ReadAllBytes(Path.Combine(BuildOutputPath, CurrentBuildTarget.ToString()));
            manifestBundle = AssetBundle.LoadFromMemory(bytes);
            manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.ToString());
        }
        finally
        {
            if (null != manifestBundle)
            {
                manifestBundle.Unload(false);
                manifestBundle = null;
            }
        }
        Debug.Assert(null != manifest);

        return manifest;
    }

    private static void CopyAsset(string fromDirectory, string toDirectory)
    {
        EditorUtility.DisplayProgressBar("Copy Resources", "Copy files...", 0);
        if (Directory.Exists(toDirectory))
        {
            Directory.Delete(toDirectory, true);
        }

        AssetBundleManifest manifest = LoadManifest();
        string[] assets = manifest.GetAllAssetBundles();
        for (int index = 0; index < assets.Length; ++index)
        {
            string asset = assets[index];
            CopyFileEx(Path.Combine(fromDirectory, asset), Path.Combine(toDirectory, asset));
            EditorUtility.DisplayProgressBar("Copy Resources", asset, (float)index / assets.Length);
        }
        EditorUtility.ClearProgressBar();
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

        AssetBundleManifest manifest = LoadManifest();

        string[] assets = manifest.GetAllAssetBundles();
        EditorUtility.DisplayProgressBar("Generate resource", "Create verfile...", 1);

        ResourceConfig resource = new ResourceConfig();
        foreach (var asset in assets)
        {
            ConfResourceItem resItem = new ConfResourceItem();
            string fullPath = Path.Combine(BuildOutputPath, asset);
            FileInfo fileInfo = new FileInfo(fullPath);
            int fileSize = 0;

            if (fileInfo.Exists)
            {
                fileSize = (int)fileInfo.Length / 1024;
            }
            else
            {
                Debug.LogError("the file is not exists file = " + asset);
            }

            resItem.file = asset;
            resItem.size = fileSize;
            resItem.md5 = Utils.FileToMd5(fullPath);
            resItem.assetType = AssetBundleExportType.Alone;

            if (asset.EndsWith(".field"))
            {
                string[] dependencies = manifest.GetAllDependencies(asset);
                foreach (var depend in dependencies)
                {
                    resItem.dependencies.Add(depend);
                }
            }

            resource.resource.patchLst.Add(resItem);
        }

        #region generate verfile
        SerializeJsonFile(resource.resource, "/data/conf/verfile.json");
        #endregion

        #region generate update file list
        #endregion

        string tempDirectory = Path.Combine(Application.dataPath, "../" + FileUtil.GetUniqueTempPathInProject());
        if (!Directory.Exists(tempDirectory))
        {
            Directory.CreateDirectory(tempDirectory);
        }
        AssetBundleBuild[] buildMap = new AssetBundleBuild[] 
        {
            new AssetBundleBuild(){ assetBundleName = "data/conf/verfile.conf", assetNames = new string[]{ "Assets/data/conf/verfile.json" } }
        };
        BuildPipeline.BuildAssetBundles(tempDirectory, buildMap, BuildAssetBundleOptions.ChunkBasedCompression, CurrentBuildTarget);

        CopyFileEx(Path.Combine(tempDirectory, "data/conf/verfile.conf"), Path.Combine(BuildOutputPath, "data/conf/verfile.conf"));

        EditorUtility.ClearProgressBar();
    }

    public static void BuildAsetsBundle(BuildItemConfig[] buildItemConfig, string rootPath)
    {
        RemoveAllAssetBundleNames();
        GenerateBundles(buildItemConfig, rootPath);

        {
            CopyAsset(BuildOutputPath, Application.streamingAssetsPath);
            AssetDatabase.ImportAsset("Assets/StreamingAssets/data", ImportAssetOptions.ForceUpdate);
        }
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

