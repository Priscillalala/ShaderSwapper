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
using System.Runtime.CompilerServices;

namespace ShaderSwapper
{
    public static class ShaderSwapper
    {
        const string PREFIX = "Stubbed";
        const int PREFIX_LENGTH = 7;

        private static UnityEngine.Object[] _ = Array.Empty<UnityEngine.Object>();

        public static IEnumerator UpgradeStubbedShadersAsync(this AssetBundle assetBundle)
        {
            if (assetBundle == null)
            {
                throw new ArgumentNullException(nameof(assetBundle));
            }
            AssetBundleRequest loadMaterials = assetBundle.LoadAllAssetsAsync<Material>();
            if (!loadMaterials.isDone)
            {
                yield return loadMaterials;
            }
            UnityEngine.Object[] allMaterials = loadMaterials.allAssets;
            int materialCount = allMaterials.Length;
            if (materialCount <= 0)
            {
                yield break;
            }
            List<AsyncOperationHandle> loadResourceLocationsOperations = new List<AsyncOperationHandle>(materialCount);
            /*foreach (Material material in loadMaterials.allAssets)
            {
                string cachedShaderName = material.shader.name;
                if (cachedShaderName.StartsWith(PREFIX))
                {
                    materials.Add(material);
                    string key = cachedShaderName.Substring(PREFIX_LENGTH) + ".shader";
                    loadResourceLocationsOperations.Add(Addressables.LoadResourceLocationsAsync(key, typeof(Shader)));
                }
            }*/
            for (int i = materialCount - 1; i >= 0; i--)
            {
                string cachedShaderName = ((Material)allMaterials[i]).shader.name;
                if (cachedShaderName.StartsWith(PREFIX))
                {
                    string key = cachedShaderName.Substring(PREFIX_LENGTH) + ".shader";
                    loadResourceLocationsOperations.Add(Addressables.LoadResourceLocationsAsync(key, typeof(Shader)));
                }
                else
                {
                    ArrayRemoveAt(allMaterials, i, ref materialCount);
                }
            }
            if (materialCount <= 0)
            {
                yield break;
            }
            Debug.Log($"loadResourceLocationsOperations count: {loadResourceLocationsOperations.Count}");

            AsyncOperationHandle<IList<AsyncOperationHandle>> loadResourceLocationsGroup = Addressables.ResourceManager.CreateGenericGroupOperation(loadResourceLocationsOperations);
            if (!loadResourceLocationsGroup.IsDone)
            {
                yield return loadResourceLocationsGroup;
            }
            Debug.Log($"allResourceLocations count: {loadResourceLocationsGroup.Result.Count}");
            List<IResourceLocation> resourceLocations = new List<IResourceLocation>(materialCount);
            /*int j = 0;
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
            }*/

            for (int i = materialCount - 1; i >= 0; i--)
            {
                IList<IResourceLocation> result = (IList<IResourceLocation>)loadResourceLocationsGroup.Result[i].Result;
                if (result.Count > 0)
                {
                    Debug.Log($"added location: {result[0].PrimaryKey}");
                    resourceLocations.Add(result[0]);
                }
                else
                {
                    ArrayRemoveAt(allMaterials, materialCount - 1 - i, ref materialCount);
                }
            }
            if (materialCount <= 0)
            {
                yield break;
            }
            AsyncOperationHandle<IList<Shader>> loadShaders = Addressables.LoadAssetsAsync<Shader>(resourceLocations, null, false);
            if (!loadShaders.IsDone)
            {
                yield return loadShaders;
            }
            Debug.Log(loadShaders.Result != null);
            Debug.Log(loadShaders.Result.Count);
            int startIndex = _.Length;
            Array.Resize(ref _, startIndex + materialCount);
            for (int i = 0; i < materialCount; i++)
            {
                ((Material)allMaterials[i]).shader = loadShaders.Result[i];
                _[startIndex + i] = allMaterials[i];
                Debug.Log($"set real shader on mat {((Material)allMaterials[i]).name} to {loadShaders.Result[i]?.name ?? "null"}");
            }
            Debug.Log($"_ length: {_.Length}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ArrayRemoveAt<T>(T[] array, int index, ref int length)
        {
            length--;
            for (int i = index; i < length; i++)
            {
                array[i] = array[i + 1];
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
            StartCoroutine(testBundle.UpgradeStubbedShadersAsync());
        }
    }
}
