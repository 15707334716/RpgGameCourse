using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace TranslationPlugin
{
    [CreateAssetMenu(fileName = "TranslationPluginSetting", menuName = "Translation Plugin Setting", order = 1)]
    public class TranslationSetting : ScriptableObject
    {
        [Header("忽略以下的英文单词，不显示其中文，忽略大小写")]
        public string[] ignoreEnglish;
        
        [Header("忽略以下的中文单词，不翻译成英文")]
        public string[] ignoreChinese;
    }
}