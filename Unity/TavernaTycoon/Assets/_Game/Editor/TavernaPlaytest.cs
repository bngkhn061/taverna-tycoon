using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Taverna.EditorTools
{
    /// <summary>
    /// Oyun modunda otomatik test: temiz kayıtla başlar, PlaytestBot oynar, rapor + ekran görüntüsü üretir.
    /// Komut satırı (batchmode, -quit OLMADAN): -executeMethod Taverna.EditorTools.TavernaPlaytest.Run
    /// </summary>
    public static class TavernaPlaytest
    {
        const string Flag = "taverna_playtest";
        const string PanelPath = TavernaImport.Root + "/UI/PanelSettings.asset";

        [MenuItem("Taverna/Otomatik Oyun Testi")]
        public static void Run()
        {
            EditorSceneManager.OpenScene(TavernaSceneBuilder.GameScene, OpenSceneMode.Single);
            PlayerPrefs.DeleteKey("taverna_save_v1");
            PlayerPrefs.Save();
            SessionState.SetBool(Flag, true);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        static void Hook()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (!SessionState.GetBool(Flag, false)) return;
                if (state == PlayModeStateChange.EnteredPlayMode)
                {
                    var bot = new GameObject("PlaytestBot").AddComponent<PlaytestBot>();
                    bot.onFinished = _ => EditorApplication.ExitPlaymode();
                }
                else if (state == PlayModeStateChange.EnteredEditMode)
                {
                    SessionState.SetBool(Flag, false);
                    var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
                    ps.targetTexture = null;
                    ps.clearColor = false;
                    EditorUtility.SetDirty(ps);
                    AssetDatabase.SaveAssets();
                    PlayerPrefs.DeleteKey("taverna_save_v1");
                    if (Application.isBatchMode) EditorApplication.Exit(0);
                }
            };
        }
    }
}
