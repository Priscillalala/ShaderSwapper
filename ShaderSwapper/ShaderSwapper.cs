using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace ShaderSwapper
{
    /// <summary>
    /// A static library for upgrading stubbed shaders to actual shaders at runtime.
    /// </summary>
    public static class ShaderSwapper
    {
        const string PREFIX = "Stubbed";
        const int PREFIX_LENGTH = 7;

        private static UnityEngine.Object[] _ = Array.Empty<UnityEngine.Object>();

        /// <summary>
        /// Asynchronously upgrade all stubbed shaders in an asset bundle to real shaders.
        /// </summary>
        /// <remarks>
        /// See the mod page for example usage.
        /// </remarks>
        /// <returns>An <see cref="IEnumerator"/> which can be started as a <see cref="Coroutine"/> or yielded in an <see cref="RoR2.ContentManagement.IContentPackProvider"/>.</returns>
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
                    materialCount--;
                    for (int j = i; j < materialCount; j++)
                    {
                        allMaterials[j] = allMaterials[j + 1];
                    }
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
                    materialCount--;
                    for (int j = materialCount - i; j < materialCount; j++)
                    {
                        allMaterials[j] = allMaterials[j + 1];
                    }
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

        /// <summary>
        /// Asynchronously convert the stubbed shader of a single material to a real shader.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> which can be started as a <see cref="Coroutine"/> or yielded in an <see cref="RoR2.ContentManagement.IContentPackProvider"/>.</returns>
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

        /// <summary>
        /// Immediately upgrade all stubbed shaders in an asset bundle to real shaders.
        /// </summary>
        /// <remarks>
        /// This method will block the main thread until completed. <see cref="UpgradeStubbedShadersAsync(AssetBundle)"/> should be used instead.
        /// </remarks>
        [Obsolete($"The asynchronous method {nameof(UpgradeStubbedShadersAsync)} is heavily preferred.", false)]
        public static void UpgradeStubbedShaders(this AssetBundle assetBundle)
        {
            if (assetBundle == null)
            {
                throw new ArgumentNullException(nameof(assetBundle));
            }
            foreach (Material material in assetBundle.LoadAllAssets<Material>())
            {
                UpgradeStubbedShader(material);
            }
        }

        /// <summary>
        /// Immediately convert the stubbed shader of a single material to a real shader.
        /// </summary>
        /// <remarks>
        /// This method will block the main thread until completed. <see cref="UpgradeStubbedShaderAsync(Material)"/> should be used instead.
        /// </remarks>
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
    }
}
