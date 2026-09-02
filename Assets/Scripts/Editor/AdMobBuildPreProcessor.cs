#if UNITY_EDITOR
using System.IO;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Janggi.Core;

namespace Janggi.Editor
{
    /// <summary>
    /// 빌드 시 로컬 시크릿 키(AdMobSecrets.json)를 AndroidManifest에 자동으로 주입하고,
    /// 빌드 완료 후에는 다시 테스트 ID로 복원하여 Git에 상용 키가 남지 않도록 방지하는 빌드 프로세서입니다.
    /// </summary>
    public class AdMobBuildPreProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private const string ManifestRelativePath = "Plugins/Android/GoogleMobileAdsPlugin.androidlib/AndroidManifest.xml";
        private const string TestAndroidAppId = "ca-app-pub-3940256099942544~3347511713";
        private const string AppIdMetaKey = "com.google.android.gms.ads.APPLICATION_ID";

        public int callbackOrder => 10; // ManifestProcessor(0) 이후에 실행되어 실제 상용 ID 주입 보장

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;

            var secrets = LoadSecrets();
            if (secrets == null || string.IsNullOrEmpty(secrets.androidAppId))
            {
                Debug.Log("[AdMobBuild] 로컬 AdMobSecrets.json이 없어 기본 테스트 App ID를 유지합니다.");
                return;
            }

            string manifestPath = Path.Combine(Application.dataPath, ManifestRelativePath);
            if (File.Exists(manifestPath))
            {
                UpdateManifestAppId(manifestPath, secrets.androidAppId);
                Debug.Log($"[AdMobBuild] Android 빌드용 상용 App ID 주입 완료: {secrets.androidAppId}");
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;

            // 빌드가 끝나면 깃허브 변경점(Git Diff) 방지를 위해 테스트 App ID로 복원
            string manifestPath = Path.Combine(Application.dataPath, ManifestRelativePath);
            if (File.Exists(manifestPath))
            {
                UpdateManifestAppId(manifestPath, TestAndroidAppId);
                Debug.Log("[AdMobBuild] 빌드 종료: Git 보호를 위해 AndroidManifest.xml을 공식 테스트 ID로 안전하게 복원했습니다.");
            }
        }

        private static AdMobSecretData LoadSecrets()
        {
            var textAsset = Resources.Load<TextAsset>("AdMobSecrets");
            if (textAsset == null || string.IsNullOrEmpty(textAsset.text)) return null;

            try
            {
                return JsonUtility.FromJson<AdMobSecretData>(textAsset.text);
            }
            catch
            {
                return null;
            }
        }

        private static void UpdateManifestAppId(string manifestPath, string appId)
        {
            try
            {
                XNamespace ns = "http://schemas.android.com/apk/res/android";
                var doc = XDocument.Load(manifestPath);
                var appElement = doc.Element("manifest")?.Element("application");
                if (appElement == null) return;

                foreach (var meta in appElement.Elements("meta-data"))
                {
                    var nameAttr = meta.Attribute(ns + "name");
                    if (nameAttr != null && nameAttr.Value == AppIdMetaKey)
                    {
                        meta.SetAttributeValue(ns + "value", appId);
                        break;
                    }
                }

                doc.Save(manifestPath);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[AdMobBuild] AndroidManifest.xml 업데이트 실패: {ex.Message}");
            }
        }
    }
}
#endif
