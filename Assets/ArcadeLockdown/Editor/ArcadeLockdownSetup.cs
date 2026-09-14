#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArcadeLockdown.Editor
{
    /// <summary>Convenience menu items included with the importable package.</summary>
    public static class ArcadeLockdownSetup
    {
        private const string ScenePath = "Assets/ArcadeLockdown/Scenes/ArcadeLockdown.unity";

        [MenuItem("Arcade Lockdown/1 - Open Game Scene", priority = 1)]
        public static void OpenGameScene()
        {
            EditorSceneManager.OpenScene(ScenePath);
            Debug.Log("ARCADE LOCKDOWN is ready. Press the Play button.");
        }

        [MenuItem("Arcade Lockdown/2 - Add Scene To Build", priority = 2)]
        public static void AddSceneToBuild()
        {
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("ARCADE LOCKDOWN scene added to Build Settings.");
        }

        [MenuItem("Arcade Lockdown/3 - Validate Realistic Assets", priority = 3)]
        public static void ValidateRealisticAssets()
        {
            string[] models =
            {
                "Characters/Quaternius/Smooth_Male_Casual",
                "Models/KenneyArcade/arcade-machine",
                "Models/KenneyArcade/claw-machine",
                "Models/KenneyArcade/vending-machine",
                "Models/KenneyArcade/basketball-game",
                "Models/KenneyArcade/air-hockey",
                "Models/KenneyArcade/prize-wheel"
            };
            foreach (string model in models)
            {
                if (Resources.Load<GameObject>(model) == null)
                    throw new InvalidOperationException("Missing required Arcade Lockdown model: " + model);
            }

            AnimationClip[] clips = Resources.LoadAll<AnimationClip>(models[0]);
            if (clips.Length < 3)
                throw new InvalidOperationException("The young-man FBX did not import its animation clips.");
            bool hasIdle = false;
            bool hasWalk = false;
            bool hasRun = false;
            foreach (AnimationClip clip in clips)
            {
                hasIdle |= clip.name.IndexOf("Idle", StringComparison.OrdinalIgnoreCase) >= 0;
                hasWalk |= clip.name.IndexOf("Walk", StringComparison.OrdinalIgnoreCase) >= 0;
                hasRun |= clip.name.IndexOf("Run", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!clip.legacy)
                    throw new InvalidOperationException("Character clip must be imported as Legacy: " + clip.name);
            }
            if (!hasIdle || !hasWalk || !hasRun)
                throw new InvalidOperationException("The character needs Idle, Walk and Run animation clips.");

            GameObject arcadePrefab = Resources.Load<GameObject>(models[1]);
            bool hasColormap = false;
            foreach (Renderer renderer in arcadePrefab.GetComponentsInChildren<Renderer>(true))
                foreach (Material material in renderer.sharedMaterials)
                    if (material != null && material.mainTexture != null) hasColormap = true;
            if (!hasColormap)
                throw new InvalidOperationException("The imported arcade cabinet is missing its Kenney colormap texture.");

            string[] textures =
            {
                "PBR/Carpet012/Carpet012_1K-JPG_Color",
                "PBR/Carpet012/Carpet012_1K-JPG_NormalGL",
                "PBR/Concrete034/Concrete034_1K-JPG_Color",
                "PBR/Concrete034/Concrete034_1K-JPG_NormalGL"
            };
            foreach (string texture in textures)
            {
                if (Resources.Load<Texture2D>(texture) == null)
                    throw new InvalidOperationException("Missing required Arcade Lockdown texture: " + texture);
            }

            Debug.Log("ARCADE LOCKDOWN validation passed: " + models.Length + " imported models, " +
                      clips.Length + " character clips and " + textures.Length + " PBR maps are ready.");
        }

        public static void SmokeTestGeneratedWorld()
        {
            const string worldName = "ARCADE LOCKDOWN - Generated 3D World";
            GameObject previousWorld = GameObject.Find(worldName);
            if (previousWorld != null)
                UnityEngine.Object.DestroyImmediate(previousWorld);

            GameObject managerObject = new GameObject("Arcade Lockdown Smoke Test Manager");
            try
            {
                ArcadeGameManager manager = managerObject.AddComponent<ArcadeGameManager>();
                ArcadeWorldBuilder.Build(manager);

                GameObject generatedWorld = GameObject.Find(worldName);
                if (generatedWorld == null)
                    throw new InvalidOperationException("The generated arcade world was not created.");

                ImportedCharacterAnimator character = UnityEngine.Object.FindFirstObjectByType<ImportedCharacterAnimator>();
                if (character == null || !character.IsReady)
                    throw new InvalidOperationException("The imported young-man character was not configured with animation.");

                DisplayMachineAnimator[] machineAnimations = UnityEngine.Object.FindObjectsByType<DisplayMachineAnimator>(FindObjectsSortMode.None);
                if (machineAnimations.Length < 4)
                    throw new InvalidOperationException("Expected at least four animated arcade attractions.");

                int rendererCount = generatedWorld.GetComponentsInChildren<Renderer>(true).Length;
                if (rendererCount < 100)
                    throw new InvalidOperationException("The generated world appears incomplete (renderer count: " + rendererCount + ").");

                Debug.Log("ARCADE LOCKDOWN generated-world smoke test passed: animated character, " +
                          machineAnimations.Length + " animated attractions and " + rendererCount + " renderers are ready.");
            }
            finally
            {
                GameObject generatedWorld = GameObject.Find(worldName);
                if (generatedWorld != null)
                    UnityEngine.Object.DestroyImmediate(generatedWorld);
                UnityEngine.Object.DestroyImmediate(managerObject);
            }
        }

        [MenuItem("Arcade Lockdown/Reset Saved Best Time", priority = 20)]
        public static void ResetBestTime()
        {
            PlayerPrefs.DeleteKey("ArcadeLockdownBestTime");
            PlayerPrefs.Save();
            Debug.Log("ARCADE LOCKDOWN best time reset.");
        }
    }
}
#endif
