using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace FrameWork.Assets
{
    public class AssetBundleInfo
    {
        public Action<AssetBundleInfo> onUnloaded = null;

        public AssetBundle bundle;

        public string bundleName = string.Empty;
        public ConfResourceItem bundleData = null;
        public object param;
        public byte[] bytes;
        public string text = string.Empty;

        public float minLifeTime = 5f;

        private float readyTime = 0f;

        private bool isReady = false;

        protected Object mainObject = null;

        protected Dictionary<string, Object> mainObjectDic = new Dictionary<string, Object>();

        public virtual Object[] mainObjects 
        { 
            get;
            set;
        }

        private int RetainCount
        {
            get;
            set;
        }

        public bool IsUnused
        {
            get
            {
                return isReady && RetainCount <= 0 && UpdateReference() <= 0 && (Time.unscaledTime - readyTime > minLifeTime);
            }
        }

        private HashSet<AssetBundleInfo> dependencies = new HashSet<AssetBundleInfo>();
        private List<string> dependenciesChildren = new List<string>();
        private List<WeakReference> references = new List<WeakReference>();

        public void AddDependency(AssetBundleInfo dependency)
        {
            if (dependencies.Add(dependency))
            {
                dependency.Retain();
                dependency.dependenciesChildren.Add(this.bundleName);
            }
        }

        public void ResetLifeTime()
        {
            if (isReady)
            {
                readyTime = Time.unscaledTime;
            }
        }

        public void Retain()
        {
            ++this.RetainCount;
        }

        public void Release()
        {
            --this.RetainCount;
        }

        public void Retain(Object owner)
        {
            if (null == owner)
            {
                return;
            }

            for (int index = 0; index < references.Count; ++index)
            {
                if (owner.Equals(references[index].Target))
                {
                    return;
                }
            }

            WeakReference wr = new WeakReference(owner);
            references.Add(wr);
        }

        public void Release(Object owner)
        {
            if (null != owner)
            {
                references.RemoveAll(item => item.Target.Equals(owner));
            }

        }

        public virtual GameObject Instantiate()
        {
            return Instantiate(true);
        }

        public virtual GameObject Instantiate(bool active)
        {
            if (null != MainObject)
            {
                if (MainObject is GameObject)
                {
                    GameObject obj = MainObject as GameObject;
                    obj.SetActive(active);
                    Object inst = Object.Instantiate(obj);
                    inst.name = obj.name;
                    Retain(inst);
                    return (GameObject)inst;
                }
            }

            return null;
        }

        public virtual GameObject Instantiate(Vector3 position, Quaternion rotation, bool active = true)
        {
            if (null != MainObject)
            {
                if (MainObject is GameObject)
                {
                    GameObject obj = MainObject as GameObject;
                    obj.SetActive(active);
                    Object inst = Object.Instantiate(obj, position, rotation);
                    inst.name = obj.name;
                    Retain(inst);
                    return (GameObject)inst;
                }
            }

            return null;
        }

        public Object Require(Object user)
        {
            this.Retain(user);
            return MainObject;
        }

        public string RequireText()
        {
            return text;
        }

        public Object Require(Component component, bool autoBind)
        {
            if (autoBind && component && component.gameObject)
            {
                return Require(component.gameObject);
            }
            else
            {
                return Require(component);
            }
        }

        public Object Require(Object user, string assetName)
        {
            this.Retain(user);
            return GetTargetObject(assetName);
        }

        private int UpdateReference()
        {
            references.RemoveAll(item => { Object obj = (Object)item.Target; return !obj; });
            return references.Count;
        }

        public virtual void Dispose()
        {
            var iterator1 = mainObjectDic.GetEnumerator();
            while(iterator1.MoveNext())
            {
                GameObject.Destroy(iterator1.Current.Value);
            }
            mainObjectDic.Clear();

            UnloadBundle();

            var iterator2 = dependencies.GetEnumerator();
            while (iterator2.MoveNext())
            {
                AssetBundleInfo dep = iterator2.Current;
                dep.dependenciesChildren.Remove(this.bundleName);
                dep.Release();
            }
            dependencies.Clear();
            references.Clear();

            if (null != onUnloaded)
            {
                onUnloaded(this);
            }
        }

        public bool IsReady
        {
            get
            {
                return isReady;
            }
            set
            {
                isReady = value;
            }
        }

        public virtual Object MainObject
        {
            get
            {
                if (null == mainObject && isReady)
                {
                    if (null != bundle)
                    {
                        string[] names = bundle.GetAllAssetNames();
                        mainObject = bundle.LoadAsset(names[0]);

#if UNITY_EDITOR && AB_MODE
                        GameObject gameObject = mainObject as GameObject;
                        if (null != gameObject)
                        {
                            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>(true);
                            for (int index = 0; index < renderers.Length; ++index)
                            {
                                Renderer render = renderers[index];
                                if (null != render.sharedMaterial)
                                {
                                    render.sharedMaterial.shader = Shader.Find(render.sharedMaterial.shader.name);
                                }
                            }

                            Image[] images = gameObject.GetComponentsInChildren<Image>(true);
                            for (int index = 0; index < images.Length; ++index)
                            {
                                Image image = images[index];
                                if (null != image.material)
                                {
                                    image.material.shader = Shader.Find(image.material.shader.name);
                                }
                            }

                            RawImage[] rawImages = gameObject.GetComponentsInChildren<RawImage>(true);
                            for (int index = 0; index < rawImages.Length; ++index)
                            {
                                RawImage rw = rawImages[index];
                                if (null != rw.material)
                                {
                                    rw.material.shader = Shader.Find(rw.material.shader.name);
                                }
                            }
                        }
#endif

                        // if is alone asset, unload the bundle info after get
                        if (null != bundleData && bundleData.assetType == AssetBundleExportType.Alone)
                        {
                            UnloadBundle();
                        }
                    }
                    else
                    {
                        Logger.LogError("the bundle is null bundle name = " + bundleName);
                    }
                }

                return mainObject;
            }
        }

        public virtual Object GetTargetObject(string assetName)
        {
            if (null != bundle)
            {
                return bundle.LoadAsset(assetName);
            }

            return null;
        }

        public Object RequireAsset(Object user, string assetName)
        {
            Retain(user);
            return GetAsset(assetName);
        }

        public Object GetAsset(string assetName)
        {
            Object asset = null;
            mainObjectDic.TryGetValue(assetName, out asset);
            return asset;
        }

        public void LoadAllAssets()
        {
#if AB_MODE
            Retain();

            mainObjectDic.Clear();
            Object[] objs = bundle.LoadAllAssets();
            for(int index = 0; index < objs.Length; ++index)
            {
                mainObjectDic[objs[index].name.ToLower()] = objs[index];
            }
#endif
        }

        private void UnloadBundle()
        {
            if (null != bundle)
            {
                bundle.Unload(false);
                bundle = null;
            }
        }
    }
}