using UnityEngine;

namespace FrameWork.Assets
{
    public class AssetBundlePathResolver
    {
        public static string GetAssetRelativeFolder(string fileName)
        {
            string extension = fileName.Substring(fileName.LastIndexOf(".") + 1);
            return string.Format("data/{0}/", extension);
        }

        public static string GetAssetName(string fileName)
        {
            string pathName = fileName.Substring(0, fileName.LastIndexOf("."));
            string extension = fileName.Substring(fileName.LastIndexOf(".") + 1);
            switch(extension)
            {
                case AssetExt.CONF:
                    return pathName + ".json";
                case AssetExt.FIELD:
                    return pathName + ".unity";
                case AssetExt.TEXTURES:
                    return pathName + ".png";
                case AssetExt.UI:
                    return pathName + ".prefab";
            }

            return string.Empty;
        }

        public static string GetABModePath(string fileName)
        {
            if (fileName.Contains("data/"))
                return fileName;

            return GetAssetRelativeFolder(fileName) + fileName.ToLower();
        }

        public static string GetAssetPath(string fileName)
        {
#if UNITY_EDITOR && !AB_MODE
            return GetAssetRelativeFolder(fileName) + GetAssetName(fileName);
#else
            return GetABModePath(fileName);
#endif
        }
    }
}
