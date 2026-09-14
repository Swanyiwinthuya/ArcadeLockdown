#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArcadeLockdown.Editor
{
    [InitializeOnLoad]
    public static class ArcadeCleanGameView
    {
        static ArcadeCleanGameView()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode && SceneManager.GetActiveScene().name == "ArcadeLockdown")
                    EditorApplication.delayCall += HideGameGizmos;
            };
        }

        [MenuItem("Arcade Lockdown/4 - Hide Game View Icons", priority = 4)]
        public static void HideGameGizmos()
        {
            Type gameViewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            if (gameViewType == null) return;
            PropertyInfo property = gameViewType.GetProperty("drawGizmos", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo field = gameViewType.GetField("m_Gizmos", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (UnityEngine.Object view in Resources.FindObjectsOfTypeAll(gameViewType))
            {
                if (property != null && property.CanWrite) property.SetValue(view, false);
                else if (field != null) field.SetValue(view, false);
                (view as EditorWindow)?.Repaint();
            }
            Debug.Log("Arcade Lockdown: Game view gizmos hidden. The Game view Gizmos button can also toggle these icons.");
        }
    }
}
#endif
