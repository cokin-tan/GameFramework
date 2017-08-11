using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using ICSharpCode.SharpZipLib.Zip;
using System;

public class BuildAssetConfig
{
    public bool isUpdate = false;
    public string packagePath = string.Empty;
    public string updatePackagePath = string.Empty;
    public Action<string> successCallback = null;
}

public static class AssetBundleBuilder
{
    private const string VERFILE_CONF_PATH = "data/conf/verfile.conf";
    private const string UPDATELIST_CONF_PATH = "data/conf/updatelist.conf";

    private const string VERFILE_JSON_PATH = "data/conf/verfile.json";
    private const string UPDATELIST_JSON_PATH = "data/conf/updatelist.json";

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

    public static string UpdateBuildPath
    {
        get
        {
            string path = Path.Combine(Application.dataPath, "../UpdateBuildPath");
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
        new BuildItemConfig(){ name = AssetExt.TEXTURES, isFolder = true, filter = "*.*", recursion = true },
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

        using (StreamWriter sw = new StreamWriter(Path.Combine(Application.dataPath, path)))
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

    private static ResourceConfig LoadFileFromPackage(string packagePath, string fileName, HashSet<string> files = null)
    {
        if (string.IsNullOrEmpty(packagePath) || !File.Exists(packagePath))
        {
            Logger.LogError("the package path is invalid");
            return null;
        }

        ResourceConfig result = null;
        byte[] fileData = null;

        if (!packagePath.EndsWith(".conf"))
        {
            using(FileStream fileStream = File.Open(packagePath, FileMode.Open))
            {
                fileStream.Seek(0, SeekOrigin.Begin);
                using(ZipInputStream zipStream = new ZipInputStream(fileStream))
                {
                    ZipEntry zipEntry = null;
                    while(null != (zipEntry = zipStream.GetNextEntry()))
                    {
                        if (null != files && zipEntry.IsFile)
                        {
                            files.Add(zipEntry.Name);
                        }

                        if (zipEntry.Name.EndsWith(fileName))
                        {
                            fileData = new byte[zipEntry.Size];
                            zipStream.Read(fileData, (int)zipEntry.Offset, (int)zipEntry.Size);
                            if (null == files)
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }
        else
        {
            fileData = File.ReadAllBytes(packagePath);
        }

        AssetBundle bundle = null;
        try
        {
            bundle = AssetBundle.LoadFromMemory(fileData);
            string fileText = bundle.LoadAsset<TextAsset>(bundle.GetAllAssetNames()[0]).text;

            Logger.LogError(fileText);

            result = new ResourceConfig();
            result.Initialize(fileText);
        }
        finally
        {
            if(null != bundle)
            {
                bundle.Unload(false);
                bundle = null;
            }
        }

        return result;
    }

    private static void CopyAsset(string fromDirectory, string toDirectory)
    {
        EditorUtility.DisplayProgressBar("Copy Resources", "Copy files...", 0);
        if (Directory.Exists(toDirectory))
        {
            Logger.LogError("delete ===" + toDirectory);
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

    private static void GenerateBundles(BuildItemConfig[] buildItemConfig, string rootPath)
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

            if (!asset.EndsWith(".field"))
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
        SerializeJsonFile(resource.resource, VERFILE_JSON_PATH);
        #endregion

        #region generate update file list
        ResourceConfig updateList = new ResourceConfig();
        SerializeJsonFile(updateList.resource, UPDATELIST_JSON_PATH);
        #endregion

        string tempDirectory = Path.Combine(Application.dataPath, "../" + FileUtil.GetUniqueTempPathInProject());
        if (!Directory.Exists(tempDirectory))
        {
            Directory.CreateDirectory(tempDirectory);
        }
        AssetBundleBuild[] buildMap = new AssetBundleBuild[] 
        {
            new AssetBundleBuild(){ assetBundleName = VERFILE_CONF_PATH, assetNames = new string[]{ "Assets/data/conf/verfile.json" } },
            new AssetBundleBuild(){ assetBundleName = UPDATELIST_CONF_PATH, assetNames = new string[]{ "Assets/data/conf/updatelist.json" } },
        };
        BuildPipeline.BuildAssetBundles(tempDirectory, buildMap, BuildAssetBundleOptions.ChunkBasedCompression, CurrentBuildTarget);

        CopyFileEx(Path.Combine(tempDirectory, VERFILE_CONF_PATH), Path.Combine(BuildOutputPath, VERFILE_CONF_PATH));
        CopyFileEx(Path.Combine(tempDirectory, UPDATELIST_CONF_PATH), Path.Combine(BuildOutputPath, UPDATELIST_CONF_PATH));

        EditorUtility.ClearProgressBar();
    }

    private static void GenerateUpdatePackages(BuildAssetConfig assetConfig)
    {
        ResourceConfig oldConfig = LoadFileFromPackage(assetConfig.packagePath, VERFILE_CONF_PATH);
        ResourceConfig oldUpdateConfig = null;
        if (string.IsNullOrEmpty(assetConfig.updatePackagePath))
        {
            oldUpdateConfig = new ResourceConfig();
        }
        else
        {
            oldUpdateConfig = LoadFileFromPackage(assetConfig.updatePackagePath, UPDATELIST_CONF_PATH);
        }

        // old files md5
        Dictionary<string, string> oldFileMd5 = new Dictionary<string, string>();
        foreach(var item in oldConfig.resource.patchLst)
        {
            oldFileMd5[item.file] = item.md5;
        }
        foreach(var item in oldUpdateConfig.resource.patchLst)
        {
            oldFileMd5[item.file] = item.md5;
        }

        // load new verfile and caculate new verfile md5
        AssetBundle newVerfileBundle = null;
        ResourceConfig verfileConfig = null;
        string verfileHashValue = string.Empty;
        try
        {
            byte[] verfileBytes = File.ReadAllBytes(Path.Combine(BuildOutputPath, VERFILE_CONF_PATH));
            newVerfileBundle = AssetBundle.LoadFromMemory(verfileBytes);
            string verfileText = newVerfileBundle.LoadAsset<TextAsset>(newVerfileBundle.GetAllAssetNames()[0]).text;
            verfileConfig.Initialize(verfileText);
            ResourceConfig tempVersionJson = new ResourceConfig();
            tempVersionJson.Initialize(verfileText);
            tempVersionJson.resource.patchLst.RemoveAll((item) => { return item.file.Contains("verfile") || item.file.Contains("updatelist"); });
            verfileHashValue = Utils.HashToMd5(Pathfinding.Serialization.JsonFx.JsonWriter.Serialize(tempVersionJson));
        }
        finally
        {
            if (null != newVerfileBundle)
            {
                newVerfileBundle.Unload(false);
                newVerfileBundle = null;
            }
        }

        ResourceConfig updateConfig = new ResourceConfig();
        updateConfig.resource.hashValue = verfileHashValue;

        ConfResourceItem verfileItem = null;
        foreach(var newItem in verfileConfig.resource.patchLst)
        {
            if (newItem.file.Contains("updatelist"))
            {
                continue;
            }

            if (newItem.file.Contains("verfile"))
            {
                newItem.md5 = Utils.FileToMd5(Path.Combine(BuildOutputPath, VERFILE_CONF_PATH));
                verfileItem = newItem;
                continue;
            }

            var oldItemMd5 = string.Empty;
            oldFileMd5.TryGetValue(newItem.file, out oldItemMd5);
            // add or modify
            if (string.IsNullOrEmpty(oldItemMd5) || oldItemMd5 != newItem.md5)
            {
                updateConfig.resource.patchLst.Add(newItem);
            }
        }

        if (updateConfig.resource.patchLst.Count <= 0)
        {
            updateConfig.resource.hashValue = oldUpdateConfig.resource.hashValue;
        }
        else
        {
            updateConfig.resource.patchLst.Add(verfileItem);
        }

        // set need copy asset map
        Dictionary<string, string> needCopyMap = new Dictionary<string, string>();
        foreach(var item in updateConfig.resource.patchLst)
        {
            needCopyMap.Add(item.file, Path.Combine(BuildOutputPath, item.file));
        }
        needCopyMap[UPDATELIST_CONF_PATH] = Path.Combine(BuildOutputPath, UPDATELIST_CONF_PATH);

        // add old update list to new, but don't need to copy
        foreach(var item in oldUpdateConfig.resource.patchLst)
        {
            // this is the old update list
            if (item.file.Contains("updatelist"))
            {
                continue;
            }

            if (updateConfig.resource.patchLst.FindAll(fileItem =>{ return fileItem.file == item.file; }).Count <= 0)
            {
                updateConfig.resource.patchLst.Add(item);
            }
        }

        string updateListConfigText = SerializeJsonFile(updateConfig.resource, UPDATELIST_JSON_PATH);

        string tempDirectory = Path.Combine(Application.dataPath, "../" + FileUtil.GetUniqueTempPathInProject());
        if (!Directory.Exists(tempDirectory))
        {
            Directory.CreateDirectory(tempDirectory);
        }
        AssetBundleBuild[] buildMap = new AssetBundleBuild[] 
        {
            new AssetBundleBuild(){ assetBundleName = UPDATELIST_CONF_PATH, assetNames = new string[]{ "Assets/data/conf/updatelist.json" } },
        };
        BuildPipeline.BuildAssetBundles(tempDirectory, buildMap, BuildAssetBundleOptions.ChunkBasedCompression, CurrentBuildTarget);
        CopyFileEx(Path.Combine(tempDirectory, UPDATELIST_CONF_PATH), Path.Combine(BuildOutputPath, UPDATELIST_CONF_PATH));

        using (MemoryStream compressed = new MemoryStream())
        {
            using (ZipOutputStream compressor = new ZipOutputStream(compressed))
            {
                int count = 0;
                try
                {
                    compressor.SetComment(updateListConfigText);
                }
                catch
                {
                    compressor.SetComment(string.Format("hash:{0}", updateConfig.resource.hashValue));
                }

                foreach (var fileKV in needCopyMap)
                {
                    string file = fileKV.Value;
                    ZipEntry entry = new ZipEntry(fileKV.Key);
                    entry.DateTime = new System.DateTime();
                    entry.DosTime = 0;
                    compressor.PutNextEntry(entry);

                    if (Directory.Exists(file))
                    {
                        continue;
                    }

                    byte[] bytes = File.ReadAllBytes(file);
                    int offset = 0;
                    compressor.Write(bytes, offset, bytes.Length - offset);
                    EditorUtility.DisplayProgressBar("Generate update files", string.Format("Add:\t{0}", file), (++count % needCopyMap.Count));
                }

                compressor.Finish();
                compressor.Flush();

                byte[] fileBytes = new byte[compressed.Length];
                Array.Copy(compressed.GetBuffer(), fileBytes, compressed.Length);
                string fileName = string.Format("{0}_{1}_{2}_{3}.zip",
                                                Path.GetFileNameWithoutExtension(assetConfig.packagePath),
                                                DateTime.Now.ToString("yyyy.MM.dd_HH.mm.s"),
                                                Utils.HashToMd5(fileBytes),
                                                updateConfig.resource.hashValue);
                string filePath = Path.Combine(UpdateBuildPath, fileName);
                File.WriteAllBytes(filePath, fileBytes);

                if (null != assetConfig.successCallback)
                {
                    assetConfig.successCallback(filePath);
                }
            }
        }
    }

    private static void BuildAssetsBundle(BuildAssetConfig assetConfig, BuildItemConfig[] buildItemConfig, string rootPath)
    {
        RemoveAllAssetBundleNames();
        GenerateBundles(buildItemConfig, rootPath);
        if (assetConfig.isUpdate)
        {
            GenerateUpdatePackages(assetConfig);
        }
        else
        {
            Logger.LogError("copy assets");
            CopyAsset(BuildOutputPath, Application.streamingAssetsPath);
            AssetDatabase.ImportAsset("Assets/StreamingAssets/data", ImportAssetOptions.ForceUpdate);
        }
    }

    public static bool BuildAssetsBundle(BuildAssetConfig assetConfig = null)
    {
        if (null == assetConfig)
        {
            assetConfig = new BuildAssetConfig() { };
        }
        BuildAssetsBundle(assetConfig, buildItemConfig, Path.Combine(Application.dataPath, "data"));
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

    private int currentMode = 0; // 0:制作资源包 1:制作更新包
    private int CurrentMode
    {
        get
        {
            currentMode = EditorPrefs.GetInt("Bundle_Builder.Select_Mode", 0);
            return currentMode;
        }
        set
        {
            if (currentMode != value)
            {
                EditorPrefs.SetInt("Bundle_Builder.Select_Mode", value);
                currentMode = value;
            }
        }
    }

    private string packagePath = null;
    private string PackagePath
    {
        get
        {
            if (null == packagePath)
            {
                packagePath = EditorPrefs.GetString("Bundle_Builder.Package_Path", string.Empty);
            }
            return packagePath;
        }
        set
        {
            if (packagePath != value)
            {
                EditorPrefs.SetString("Bundle_Builder.Package_Path", value);
                packagePath = value;
            }
        }
    }

    private string updatePackagePath = null;
    private string UpdatePackagePath
    {
        get
        {
            if(null == updatePackagePath)
            {
                updatePackagePath = EditorPrefs.GetString("Bundle_Builder.Update_Package_Path", string.Empty);
            }
            return updatePackagePath;
        }
        set
        {
            if (updatePackagePath != value)
            {
                EditorPrefs.SetString("Bundle_Builder.Update_Package_Path", value);
                updatePackagePath = value;
            }
        }
    }

    [MenuItem("AssetBundle/Builder")]
    public static void CreateWindow()
    {
        var window = EditorWindow.GetWindow<AssetBundleBuilderWindow>();
        window.minSize = new Vector2(400, 600);
    }

    private string OpenFilePanelWidthPanel(string label, string[] filter)
    {
        string path = EditorUtility.OpenFilePanelWithFilters(label, EditorPrefs.GetString("Bundle_Builder.Select_Diretory"), filter);

        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            EditorPrefs.SetString("Bundle_Builder.Select_Diretory", path);
        }

        return path;
    }

    private string DrawPathPanel(string label, string filePath, string[] filter)
    {
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        filePath = EditorGUI.TextField(EditorGUILayout.GetControlRect(), label, filePath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string path = OpenFilePanelWidthPanel(label, filter);
            if (File.Exists(path))
            {
                filePath = path;
            }
        }
        EditorGUILayout.EndHorizontal();

        return filePath;
    }

    private string[] GetPlatformPackageFilter()
    {
        switch(AssetBundleBuilder.CurrentBuildTarget)
        {
            case BuildTarget.Android:
                return new string[] { "package", "apk", "verfile", "conf", "obb", "obb" };
            case BuildTarget.iOS:
                return new string[] { "package", "ipa", "verfile", "conf" };
            default:
                return new string[] { "package", "zip", "verfile", "conf" };
        }
    }

    private void BuildUpdatePackage()
    {
        AssetBundleBuilder.BuildAssetsBundle(new BuildAssetConfig() 
        {
            isUpdate = true,
            packagePath = PackagePath,
            updatePackagePath = UpdatePackagePath
        });
    }

    void OnGUI()
    {
        EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Platform:", AssetBundleBuilder.CurrentBuildTarget.ToString());
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.LabelField("DefineSymbols:", DefineSymbols);
            EditorGUI.EndDisabledGroup();

            CurrentMode = GUILayout.Toolbar(CurrentMode, new string[] { "Resource Package", "Update Package" });

            if (0 == currentMode)
            {
                EditorGUILayout.Space();
                if (GUILayout.Button("Build Asset Bundle"))
                {
                    AssetBundleBuilder.BuildAssetsBundle();
                }
            }
            else
            {
                string currentPackagePath = PackagePath;
                PackagePath = DrawPathPanel("Package path:", currentPackagePath, GetPlatformPackageFilter());
                string currentUpdatePackagePath = UpdatePackagePath;
                UpdatePackagePath = DrawPathPanel("Update Package path:", currentUpdatePackagePath, new string[] { "package", "zip", "updatelist", "conf" });

                EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(PackagePath));
                if (GUILayout.Button("Buila Update Package"))
                {
                    BuildUpdatePackage();
                }
                EditorGUI.EndDisabledGroup();
            }
        EditorGUILayout.EndVertical();
    }
}

