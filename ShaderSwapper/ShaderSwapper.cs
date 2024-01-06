using BepInEx;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
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
            for (int i = materialCount - 1; i >= 0; i--)
            {
                string cachedShaderName = ((Material)allMaterials[i]).shader.name;
                if (cachedShaderName.StartsWith(PREFIX))
                {
                    loadResourceLocationsOperations.Add(Addressables.LoadResourceLocationsAsync(cachedShaderName.Substring(PREFIX_LENGTH) + ".shader", typeof(Shader)));
                }
                else
                {
                    ArrayRemoveAtNoCleanup(allMaterials, i, ref materialCount);
                }
            }
            if (materialCount <= 0)
            {
                yield break;
            }

            AsyncOperationHandle<IList<AsyncOperationHandle>> loadResourceLocationsGroup = Addressables.ResourceManager.CreateGenericGroupOperation(loadResourceLocationsOperations);
            if (!loadResourceLocationsGroup.IsDone)
            {
                yield return loadResourceLocationsGroup;
            }

            List<IResourceLocation> resourceLocations = new List<IResourceLocation>(materialCount);
            for (int i = materialCount - 1; i >= 0; i--)
            {
                IList<IResourceLocation> result = (IList<IResourceLocation>)loadResourceLocationsGroup.Result[i].Result;
                if (result.Count > 0)
                {
                    resourceLocations.Add(result[0]);
                }
                else
                {
                    ArrayRemoveAtNoCleanup(allMaterials, materialCount - 1 - i, ref materialCount);
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
            int startIndex = _.Length;
            Array.Resize(ref _, startIndex + materialCount);
            for (int i = 0; i < materialCount; i++)
            {
                ((Material)allMaterials[i]).shader = loadShaders.Result[i];
                _[startIndex + i] = allMaterials[i];
            }
        }

        [Obsolete($"The asynchronous method {nameof(UpgradeStubbedShadersAsync)} is heavily preferred.", false)]
        public static void UpgradeStubbedShaders(this AssetBundle assetBundle)
        {
            if (assetBundle == null)
            {
                throw new ArgumentNullException(nameof(assetBundle));
            }
            Material[] allMaterials = assetBundle.LoadAllAssets<Material>();
            int materialCount = allMaterials.Length;
            if (materialCount <= 0)
            {
                return;
            }
            for (int i = materialCount - 1; i >= 0; i--)
            {
                string cachedShaderName = allMaterials[i].shader.name;
                if (cachedShaderName.StartsWith(PREFIX))
                {
                    IList<IResourceLocation> resourceLocations = Addressables.LoadResourceLocationsAsync(cachedShaderName.Substring(PREFIX_LENGTH) + ".shader", typeof(Shader)).WaitForCompletion();
                    if (resourceLocations.Count > 0)
                    {
                        allMaterials[i].shader = Addressables.LoadAssetAsync<Shader>(resourceLocations[0]).WaitForCompletion();
                        continue;
                    }
                }
                ArrayRemoveAtNoCleanup(allMaterials, i, ref materialCount);
            }
            if (materialCount <= 0)
            {
                return;
            }
            int startIndex = _.Length;
            Array.Resize(ref _, startIndex + materialCount);
            for (int i = 0; i < materialCount; i++)
            {
                _[startIndex + i] = allMaterials[i];
            }
        }

        public static IEnumerator UpgradeStubbedShaderAsync(Material material)
        {
            if (material == null)
            {
                throw new ArgumentNullException(nameof(material));
            }
            string cachedShaderName = material.shader.name;
            if (!cachedShaderName.StartsWith(PREFIX))
            {
                yield break;
            }

            AsyncOperationHandle<IList<IResourceLocation>> loadResourceLocations = Addressables.LoadResourceLocationsAsync(cachedShaderName.Substring(PREFIX_LENGTH) + ".shader", typeof(Shader));
            if (!loadResourceLocations.IsDone)
            {
                yield return loadResourceLocations;
            }
            if (loadResourceLocations.Result.Count <= 0)
            {
                yield break;
            }

            AsyncOperationHandle<Shader> loadShader = Addressables.LoadAssetAsync<Shader>(loadResourceLocations.Result[0]);
            if (!loadShader.IsDone)
            {
                yield return loadShader;
            }
            material.shader = loadShader.Result;
            Array.Resize(ref _, _.Length + 1);
            _[_.Length - 1] = material;
        }

        [Obsolete($"The asynchronous method {nameof(UpgradeStubbedShaderAsync)} is heavily preferred.", false)]
        public static void UpgradeStubbedShader(Material material)
        {
            if (material == null)
            {
                throw new ArgumentNullException(nameof(material));
            }
            string cachedShaderName = material.shader.name;
            if (!cachedShaderName.StartsWith(PREFIX)) 
            {
                return;
            }
            IList<IResourceLocation> resourceLocations = Addressables.LoadResourceLocationsAsync(cachedShaderName.Substring(PREFIX_LENGTH) + ".shader", typeof(Shader)).WaitForCompletion();
            if (resourceLocations.Count > 0)
            {
                material.shader = Addressables.LoadAssetAsync<Shader>(resourceLocations[0]).WaitForCompletion();
                Array.Resize(ref _, _.Length + 1);
                _[_.Length - 1] = material;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ArrayRemoveAtNoCleanup<T>(T[] array, int index, ref int length)
        {
            length--;
            for (int i = index; i < length; i++)
            {
                array[i] = array[i + 1];
            }
        }
    }
}
