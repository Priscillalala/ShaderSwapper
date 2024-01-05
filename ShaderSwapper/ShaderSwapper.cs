using BepInEx;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using System.Linq;

namespace ShaderSwapper
{
    public static class ShaderSwapper
    {
        const string PREFIX = "Stubbed";
        const int PREFIX_LENGTH = 7;

        private static Material[] _ = Array.Empty<Material>();

        public static IEnumerator UpgradeStubbedShadersAsync(this AssetBundle assetBundle)
        {
            AssetBundleRequest loadMaterials = assetBundle.LoadAllAssetsAsync<Material>();
            if (!loadMaterials.isDone)
            {
                yield return loadMaterials;
            }
            Debug.Log($"Loaded {loadMaterials.allAssets.Length} materials:");
            foreach (Material material in loadMaterials.allAssets)
            {
                Debug.Log(material.name);
                string cachedShaderName = material.shader.name;
                if (!cachedShaderName.StartsWith(PREFIX))
                {
                    continue;
                }
                string key = cachedShaderName.Substring(PREFIX_LENGTH) + ".shader";
                Debug.Log(key);
                AsyncOperationHandle<IList<IResourceLocation>> loadResourceLocations = Addressables.LoadResourceLocationsAsync(key, typeof(Shader));
                if (!loadResourceLocations.IsDone)
                {
                    yield return loadResourceLocations;
                }
                Debug.Log($"locations count: {loadResourceLocations.Result.Count}");
                if (loadResourceLocations.Result.Count <= 0)
                {
                    continue;
                }
                AsyncOperationHandle<Shader> loadShader = Addressables.LoadAssetAsync<Shader>(loadResourceLocations.Result[0]);
                if (!loadShader.IsDone)
                {
                    yield return loadShader;
                }
                Debug.Log($"set shader: {loadShader.Result.name}");
                material.shader = loadShader.Result;
                //_.Add(material);
            }
        }

        public static IEnumerator UpgradeStubbedShadersAsyncNew(this AssetBundle assetBundle)
        {
            AssetBundleRequest loadMaterials = assetBundle.LoadAllAssetsAsync<Material>();
            if (!loadMaterials.isDone)
            {
                yield return loadMaterials;
            }
            //List<Material> materials = new List<Material>();
            List<AsyncOperationHandle> loadResourceLocationsOperations = new List<AsyncOperationHandle>();
            int startIndex = _?.Length ?? 0;
            Debug.Log($"startIndex: {startIndex}");
            Array.Resize(ref _, startIndex + loadMaterials.allAssets.Length);
            int stubbedMaterialsCount = 0;
            foreach (Material material in loadMaterials.allAssets)
            {
                string cachedShaderName = material.shader.name;
                if (cachedShaderName.StartsWith(PREFIX))
                {
                    string key = cachedShaderName.Substring(PREFIX_LENGTH) + ".shader";
                    loadResourceLocationsOperations.Add(Addressables.LoadResourceLocationsAsync(key, typeof(Shader)));
                    _[startIndex + stubbedMaterialsCount++] = material;
                }
            }
            Array.Resize(ref _, startIndex + stubbedMaterialsCount);
            Debug.Log($"stubbedMaterialsCount: {stubbedMaterialsCount}");
            Debug.Log($"loadResourceLocationsOperations count: {loadResourceLocationsOperations.Count}");

            AsyncOperationHandle<IList<AsyncOperationHandle>> loadResourceLocationsGroup = Addressables.ResourceManager.CreateGenericGroupOperation(loadResourceLocationsOperations);
            if (!loadResourceLocationsGroup.IsDone)
            {
                yield return loadResourceLocationsGroup;
            }
            Debug.Log($"allResourceLocations count: {loadResourceLocationsGroup.Result.Count}");
            List<IResourceLocation> resourceLocations = new List<IResourceLocation>();
            resourceLocations.Add(new ResourceLocationBase("test", "test", "test", typeof(Shader)));
            foreach (AsyncOperationHandle handle in loadResourceLocationsGroup.Result)
            {
                IList<IResourceLocation> result = (IList<IResourceLocation>)handle.Result;
                if (result.Count > 0)
                {
                    Debug.Log($"added location: {result[0].PrimaryKey}");
                    resourceLocations.Add(result[0]);
                }
            }
            AsyncOperationHandle<IList<Shader>> loadShaders = Addressables.LoadAssetsAsync<Shader>(resourceLocations, null, false);
            if (!loadShaders.IsDone)
            {
                yield return loadShaders;
            }
            Debug.Log(loadShaders.Result != null);
            Debug.Log(loadShaders.Result.Count);
            int j = 0;
            for (int i = 0; i < loadResourceLocationsGroup.Result.Count; i++)
            {
                if (((IList<IResourceLocation>)loadResourceLocationsGroup.Result[i].Result).Count > 0)
                {
                    Debug.Log($"set real shader on mat {_[startIndex + i].name} to {loadShaders.Result[j]?.name ?? "null"}");
                    _[startIndex + i].shader = loadShaders.Result[j++];
                }
                else
                {
                    _[startIndex + i] = null;
                }
            }
        }

        public static IEnumerator UpgradeStubbedShadersAsyncNewer(this AssetBundle assetBundle)
        {
            AssetBundleRequest loadMaterials = assetBundle.LoadAllAssetsAsync<Material>();
            if (!loadMaterials.isDone)
            {
                yield return loadMaterials;
            }
            if (loadMaterials.allAssets.Length <= 0)
            {
                yield break;
            }
            List<Material> materials = new List<Material>(loadMaterials.allAssets.Length);
            List<AsyncOperationHandle> loadResourceLocationsOperations = new List<AsyncOperationHandle>(loadMaterials.allAssets.Length);
            //int stubbedMaterialsCount = 0;
            foreach (Material material in loadMaterials.allAssets)
            {
                string cachedShaderName = material.shader.name;
                if (cachedShaderName.StartsWith(PREFIX))
                {
                    materials.Add(material);
                    string key = cachedShaderName.Substring(PREFIX_LENGTH) + ".shader";
                    loadResourceLocationsOperations.Add(Addressables.LoadResourceLocationsAsync(key, typeof(Shader)));
                }
            }
            if (materials.Count <= 0)
            {
                yield break;
            }
            //Debug.Log($"stubbedMaterialsCount: {stubbedMaterialsCount}");
            Debug.Log($"loadResourceLocationsOperations count: {loadResourceLocationsOperations.Count}");

            AsyncOperationHandle<IList<AsyncOperationHandle>> loadResourceLocationsGroup = Addressables.ResourceManager.CreateGenericGroupOperation(loadResourceLocationsOperations);
            if (!loadResourceLocationsGroup.IsDone)
            {
                yield return loadResourceLocationsGroup;
            }
            Debug.Log($"allResourceLocations count: {loadResourceLocationsGroup.Result.Count}");
            List<IResourceLocation> resourceLocations = new List<IResourceLocation>(loadResourceLocationsGroup.Result.Count);
            int j = 0;
            foreach (AsyncOperationHandle operation in loadResourceLocationsGroup.Result)
            {
                IList<IResourceLocation> result = (IList<IResourceLocation>)operation.Result;
                if (result.Count > 0)
                {
                    Debug.Log($"added location: {result[0].PrimaryKey}");
                    resourceLocations.Add(result[0]);
                    j++;
                }
                else
                {
                    materials.RemoveAt(j);
                }
            }
            if (materials.Count <= 0)
            {
                yield break;
            }
            /*for (int i = loadResourceLocationsGroup.Result.Count - 1; i >= 0; i--)
            {
                IList<IResourceLocation> result = (IList<IResourceLocation>)loadResourceLocationsGroup.Result[i].Result;
                if (result.Count > 0)
                {
                    Debug.Log($"added location: {result[0].PrimaryKey}");
                    resourceLocations.Add(result[0]);
                }
                else
                {
                    materials.RemoveAt(i);
                }
            }*/
            AsyncOperationHandle<IList<Shader>> loadShaders = Addressables.LoadAssetsAsync<Shader>(resourceLocations, null, false);
            if (!loadShaders.IsDone)
            {
                yield return loadShaders;
            }
            Debug.Log(loadShaders.Result != null);
            Debug.Log(loadShaders.Result.Count);
            int startIndex = _.Length;
            Array.Resize(ref _, startIndex + materials.Count);
            for (int i = 0; i < materials.Count; i++)
            {
                materials[i].shader = loadShaders.Result[i];
                _[startIndex + i] = materials[i];
                Debug.Log($"set real shader on mat {materials[i].name} to {loadShaders.Result[i]?.name ?? "null"}");
            }
        }
    }

    [BepInPlugin("com.groovesalad.TestPlugin", "TestPlugin", "1.0.0")]
    public class TestPlugin : BaseUnityPlugin
    {
        public AssetBundle testBundle;
        public void OnEnable()
        {
            Logger.LogInfo("Awake!");
            testBundle ??= AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "testassets"));
            StartCoroutine(testBundle.UpgradeStubbedShadersAsyncNewer());
        }
    }
}
