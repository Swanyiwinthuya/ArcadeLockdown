#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ArcadeLockdown.Editor
{
    public sealed class TeenCharacterImport : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if(!assetPath.Contains("/Characters/Universal/"))return;
            ModelImporter importer=(ModelImporter)assetImporter;
            importer.isReadable=true;
            importer.importBlendShapes=false;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            if(assetPath.EndsWith("Leo.fbx")||assetPath.EndsWith("Maya.fbx")||assetPath.EndsWith("Locomotion.fbx"))
            {
                importer.animationType=ModelImporterAnimationType.Human;
                importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation=assetPath.EndsWith("Locomotion.fbx");
            }
        }
        private void OnPreprocessTexture()
        {
            if(!assetPath.Contains("/Characters/Universal/"))return;
            TextureImporter t=(TextureImporter)assetImporter;t.maxTextureSize=1024;t.mipmapEnabled=true;t.anisoLevel=4;
        }

        public static void Prepare()
        {
            AssetDatabase.Refresh();
            const string directory="Assets/ArcadeLockdown/Resources/Characters/Universal/";
            foreach(string n in new[]{"Leo","Maya","Locomotion"})
            {
                ModelImporter imp=(ModelImporter)AssetImporter.GetAtPath(directory+n+".fbx");
                imp.SaveAndReimport();
                GameObject model=AssetDatabase.LoadAssetAtPath<GameObject>(directory+n+".fbx");
                Animator animator=model.GetComponent<Animator>();
                if(animator==null||animator.avatar==null||!animator.avatar.isValid||!animator.avatar.isHuman)
                    throw new InvalidOperationException("Humanoid avatar invalid: "+n);
            }
            AnimationClip[] clips=AssetDatabase.LoadAllAssetsAtPath(directory+"Locomotion.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
            Debug.Log("V5 AVAILABLE ANIMATIONS: "+string.Join(", ",clips.Select(c=>c.name)));
            const string controllerPath=directory+"TeenLocomotion.controller";
            AnimatorController controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            if(controller.layers.Length==0)controller.AddLayer("Base Layer");
            AnimatorStateMachine machine=controller.layers[0].stateMachine;
            foreach(ChildAnimatorState state in machine.states)machine.RemoveState(state.state);
            foreach(string name in new[]{"Idle","Walk","Run"})
            {
                string sourceName = name == "Idle" ? "Idle_Loop" : name == "Walk" ? "Walk_Loop" : "Sprint_Loop";
                AnimationClip clip=clips.FirstOrDefault(c=>c.name.EndsWith("|"+sourceName,StringComparison.OrdinalIgnoreCase));
                if(clip==null)throw new InvalidOperationException("Missing locomotion animation "+name);
                string clipPath=directory+name+".anim";
                AnimationClip saved=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if(saved==null){saved=UnityEngine.Object.Instantiate(clip);AssetDatabase.CreateAsset(saved,clipPath);}
                else EditorUtility.CopySerialized(clip,saved);
                saved.name=name;
                AnimationClipSettings settings=AnimationUtility.GetAnimationClipSettings(saved);
                settings.loopTime=true;settings.loopBlend=true;settings.keepOriginalPositionXZ=true;
                settings.keepOriginalOrientation=true;
                settings.loopBlendOrientation=true;settings.loopBlendPositionXZ=true;settings.loopBlendPositionY=true;
                AnimationUtility.SetAnimationClipSettings(saved,settings);
                AnimatorState s=machine.AddState(name);s.motion=saved;s.writeDefaultValues=true;
                if(name=="Idle")machine.defaultState=s;
                Debug.Log("V5 LOCOMOTION: "+name+" <- "+clip.name);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("V5 CHARACTER IMPORT PASSED");
        }
    }
}
#endif
