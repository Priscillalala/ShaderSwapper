using System;
using BepInEx;
using UnityEngine;

namespace ShaderSwapper
{
    [BepInPlugin("com.groovesalad.TestPlugin", "TestPlugin", "1.0.0")]
    public class TestPlugin : BaseUnityPlugin
    {
        public AssetBundle testBundle;
        public void OnEnable()
        {
            Logger.LogInfo("Awake!");
            testBundle ??= AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "testassets"));
            StartCoroutine(testBundle.UpgradeStubbedShadersAsync());
            //Material test1 = testBundle.LoadAsset<Material>("matTest4");
            //StartCoroutine(ShaderSwapper.UpgradeStubbedShaderAsync(test1));
            //testBundle.UpgradeStubbedShaders();
        }
    }
}
