using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TranslationPlugin
{
    public class TranslationPlugin : EditorWindow
    {
        [MenuItem("Window/中英双语翻译")]
        public static void ShowWindow()
        {
            GetWindow(typeof(TranslationPlugin), false, "中英双语翻译");
        }

        public bool skipSingleWord = true;
        
        public TranslationSetting setting;
        
        private void OnGUI()
        {
            setting = EditorGUILayout.ObjectField(
                new GUIContent("忽略文件(可选)","可忽略此文件中的词汇 Create -> Translation Plugin Setting"),
                setting,typeof(TranslationSetting), false) as TranslationSetting;
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("忽略单个词", "是否忽略单个英文的中文翻译(只显示英文), 推荐勾选"));
            skipSingleWord = GUILayout.Toggle(skipSingleWord, "");
            GUILayout.EndHorizontal();
                
            if (GUILayout.Button("中英翻译"))
                Translate();
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("还原备份"))
                RestoreBackUp();
        }

        /// <summary>
        /// 中英翻译
        /// </summary>
        private void Translate()
        {
            if (setting == null)
            {
                setting = CreateInstance<TranslationSetting>();
            }

            setting.ignoreEnglish = setting.ignoreEnglish.Select(s => s.Trim()).ToArray();
            setting.ignoreChinese = setting.ignoreChinese.Select(s => s.Trim()).ToArray();
            
            var fullPath = GetFullPath();
                            
            if (File.Exists(fullPath))
            {
                if (!File.Exists(fullPath + ".bak"))
                {
                    File.Move(fullPath, fullPath+".bak"); // 备份原文件
                }
                               
            }  else if (!File.Exists(fullPath + ".bak"))
            {
                EditorUtility.DisplayDialog("翻译失败", "缺少中文翻译文件，请先打开Unity Hub，添加当前Editor的中文模块。", "OK");
                return;
            }
                            
                            
            var lines = File.ReadAllLines(fullPath+".bak");
                            
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                                
                if (string.IsNullOrWhiteSpace(line)) 
                    continue;
                                
                // 一些中文翻译带有引号，这里去掉是为了美观
                line = line.Replace( "“","");
                line = line.Replace("”", "");
                lines[i] = line;
                                
                if (!line.StartsWith("msgstr"))  // 只翻译 msgstr 开头的行
                    continue; 
                    
                var msgId = ExtractMsgId(lines[i - 1]);
                if (msgId == "")
                {
                    continue;
                }
                
                // 如果是一个单词，或者 ignoreEnglish == true , 则翻译为原英文
                if (skipSingleWord && msgId.All(char.IsLetter) || setting.ignoreEnglish.Contains(msgId,StringComparer.OrdinalIgnoreCase) )  
                {
                    lines[i] = string.Format($"msgstr \"{msgId}\"");
                    continue;
                }
                
                // 如果包含忽略的中文，则不翻译
                if (setting.ignoreChinese.Contains(ExtractMsgStr(line),StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }
                
                // 如果包含中文标点符号(说明是中文句子)，或者不包含中文，或者包含英文，则不翻译
                if (ContainsPunctuation(line) || !ContainsChinese(line) || line.Contains("...") )
                {
                    continue;
                }
                    
                lines[i] = line.TrimEnd('"') + string.Format($"[{msgId}]") + "\"";
            }
            File.WriteAllLines(fullPath, lines);
            ShowDialog("翻译完成", "重启编辑器后生效，是否立即重启？", "OK", "Later");
        }
    
        /// <summary>
        /// 还原备份
        /// </summary>
        private static void RestoreBackUp()
        {
            var fullPath = GetFullPath();
        
            if (File.Exists(fullPath+".bak"))
            {
                File.Delete(fullPath); // 删除当前中文文件
                File.Move(fullPath+".bak", fullPath);
                ShowDialog("还原完成", "重启编辑器后生效，是否立即重启？", "OK", "Later");
            }
            else
            {
                EditorUtility.DisplayDialog("还原完成", "没有需要还原的中文翻译", "OK");
            }
        }
    
        /// <summary>
        /// 在编辑器中显示对话框，包含两个按钮，点击第一个按钮则重启编辑器
        /// </summary>
        private static void ShowDialog(string title, string content,string ok, string cancel)
        {
            var option = EditorUtility.DisplayDialogComplex(title, content, ok, cancel, "");
            switch (option)
            {
                case 0:
                    // 重启editor
                    EditorApplication.OpenProject(Directory.GetCurrentDirectory());
                    break;
            }
        }
    
        /// <summary>
        /// 获取中文翻译的完整路径
        /// </summary>
        private static string GetFullPath()
        {
#if UNITY_EDITOR_WIN
        var path = UnityEditor.EditorApplication.applicationPath;
        path = Path.GetDirectoryName(path) + @"\Data\Localization\";
#else // 未测试 Linux 平台
            var path = EditorApplication.applicationPath + "/Contents/Localization/";
#endif

            return path + "zh-hans.po";
        }
    
        /// <summary>
        /// 是否包含中文
        /// </summary>
        private static bool ContainsChinese(string s)
        {
            return s.Any(c => c >= 0x4E00 && c <= 0x9FA5);
        }
    
        /// <summary>
        /// 是否包含中文标点符号
        /// </summary>
        private static bool ContainsPunctuation(string s)
        {
            return s.Any(c => new[] { '。', '，','、', '！','!', '?','？', '/', '{', '%', '（' }.Contains(c));
        }
    
        /// <summary>
        /// 提取 msgid 内容
        /// </summary>
        private static string ExtractMsgId(string s)
        {
            var parts = s.Split(new[] { "msgid \"" }, StringSplitOptions.None);
            return parts.Length == 2 ? parts[1].TrimEnd('"').Trim() : parts[0].Trim();
        }
        
        /// <summary>
        /// 提取 msgstr 内容
        /// </summary>
        private static string ExtractMsgStr(string s)
        {
            var parts = s.Split(new[] { "msgstr \"" }, StringSplitOptions.None);
            return parts.Length == 2 ? parts[1].TrimEnd('"').Trim() : parts[0].Trim();
        }
    }
}