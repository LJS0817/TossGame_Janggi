using UnityEditor;
using UnityEngine;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;

namespace Janggi.EditorScripts
{
    [InitializeOnLoad]
    public class LocalizationUpdater
    {
        static LocalizationUpdater()
        {
            EditorApplication.delayCall += UpdateTable;
        }

        static void UpdateTable()
        {
            if (SessionState.GetBool("BtnPauseAdded_4", false)) return;
            SessionState.SetBool("BtnPauseAdded_4", true);

            // Get the collection
            var collection = LocalizationEditorSettings.GetStringTableCollection("JanggiStringTable");
            if (collection != null)
            {
                // 1. Add Btn_Pause
                var entryPause = collection.SharedData.GetEntry("Btn_Pause");
                if (entryPause == null)
                {
                    collection.SharedData.AddKey("Btn_Pause");
                    
                    var koTable = collection.StringTables[0]; // Backup, will find specifically below
                    var enTable = collection.StringTables[1];

                    foreach (var table in collection.StringTables)
                    {
                        var t = table as StringTable;
                        if (t == null) continue;

                        if (t.LocaleIdentifier.Code.StartsWith("ko"))
                        {
                            t.AddEntry("Btn_Pause", "일시정지");
                            EditorUtility.SetDirty(t);
                        }
                        else if (t.LocaleIdentifier.Code.StartsWith("en"))
                        {
                            t.AddEntry("Btn_Pause", "Pause");
                            EditorUtility.SetDirty(t);
                        }
                    }
                    
                    EditorUtility.SetDirty(collection.SharedData);
                }

                // 2. Add or Update Header_Title
                var entryHeader = collection.SharedData.GetEntry("Header_Title");
                if (entryHeader == null)
                {
                    collection.SharedData.AddKey("Header_Title");
                }
                
                foreach (var table in collection.StringTables)
                {
                    var t = table as StringTable;
                    if (t == null) continue;

                    if (t.LocaleIdentifier.Code.StartsWith("ko"))
                    {
                        t.AddEntry("Header_Title", "장기 아케이드");
                        EditorUtility.SetDirty(t);
                    }
                    else if (t.LocaleIdentifier.Code.StartsWith("en"))
                    {
                        t.AddEntry("Header_Title", "Janggi Arcade");
                        EditorUtility.SetDirty(t);
                    }
                }
                
                EditorUtility.SetDirty(collection.SharedData);

                // 3. Add Msg_Ad_Loading
                var entryAdLoading = collection.SharedData.GetEntry("Msg_Ad_Loading");
                if (entryAdLoading == null)
                {
                    collection.SharedData.AddKey("Msg_Ad_Loading");
                    foreach (var table in collection.StringTables)
                    {
                        var t = table as StringTable;
                        if (t == null) continue;

                        if (t.LocaleIdentifier.Code.StartsWith("ko"))
                        {
                            t.AddEntry("Msg_Ad_Loading", "광고 로딩중...");
                            EditorUtility.SetDirty(t);
                        }
                        else if (t.LocaleIdentifier.Code.StartsWith("en"))
                        {
                            t.AddEntry("Msg_Ad_Loading", "Loading Ad...");
                            EditorUtility.SetDirty(t);
                        }
                    }
                    EditorUtility.SetDirty(collection.SharedData);
                }

                // 4. Add Side_Cho_Short, Side_Han_Short, Format_Full_Piece
                AddOrUpdateKey(collection, "Side_Cho_Short", "초(楚)", "Cho");
                AddOrUpdateKey(collection, "Side_Han_Short", "한(漢)", "Han");
                AddOrUpdateKey(collection, "Format_Full_Piece", "{0} {1}", "{0} {1}");

                AssetDatabase.SaveAssets();
                Debug.Log("[LocalizationUpdater] Keys added to JanggiStringTable assets successfully!");
            }
        }

        private static void AddOrUpdateKey(StringTableCollection collection, string key, string koVal, string enVal)
        {
            var entry = collection.SharedData.GetEntry(key);
            if (entry == null)
            {
                collection.SharedData.AddKey(key);
            }

            foreach (var table in collection.StringTables)
            {
                var t = table as StringTable;
                if (t == null) continue;

                if (t.LocaleIdentifier.Code.StartsWith("ko"))
                {
                    t.AddEntry(key, koVal);
                    EditorUtility.SetDirty(t);
                }
                else if (t.LocaleIdentifier.Code.StartsWith("en"))
                {
                    t.AddEntry(key, enVal);
                    EditorUtility.SetDirty(t);
                }
            }
            EditorUtility.SetDirty(collection.SharedData);
        }
    }
}
