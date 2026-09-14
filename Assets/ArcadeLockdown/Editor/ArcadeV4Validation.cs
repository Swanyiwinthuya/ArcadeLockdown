#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace ArcadeLockdown.Editor
{
    // These batch entry points test the running game, including animated geometry.
    [InitializeOnLoad]
    public static class ArcadeV4Validation
    {
        private const string ActiveKey = "ArcadeV4.Validation";
        private static double next;
        private static int stage;
        private static RenderTexture target;
        private static Vector3 movingStart;
        private static string output;
        private static int errors;
        private static int requiredFrame = 12;
        private static string[] tokenOrder = { "Red token", "Blue token", "Yellow token" };

        static ArcadeV4Validation()
        {
            if (SessionState.GetBool(ActiveKey,false))
            {
                EditorApplication.update += Tick;
                Application.logMessageReceived += CaptureError;
                next=EditorApplication.timeSinceStartup+4;
                output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../deliverables/Arcade-v4-Previews"));
            }
        }

        public static void PrepareAssets()
        {
            ModelImporter characterImporter=(ModelImporter)AssetImporter.GetAtPath("Assets/ArcadeLockdown/Resources/Characters/Quaternius/Smooth_Male_Casual.fbx");
            characterImporter.isReadable=true;characterImporter.SaveAndReimport();
            string directory="Assets/ArcadeLockdown/Resources/PBR/PolyHaven/";
            foreach(string asset in new[]{"painted_plaster_wall","wood_cabinet_worn_long","metal_plate"})
            {
                string path=directory+asset+"_rough_1k.jpg";
                TextureImporter importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.isReadable=true;importer.SaveAndReimport();
                Texture2D rough=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Color[] colors=rough.GetPixels();
                float metal=asset=="metal_plate"?.75f:0f;
                for(int i=0;i<colors.Length;i++) colors[i]=new Color(metal,metal,metal,1-colors[i].r);
                Texture2D packed=new Texture2D(rough.width,rough.height,TextureFormat.RGBA32,false,true);
                packed.SetPixels(colors);packed.Apply();
                File.WriteAllBytes(directory+asset+"_metallic_smoothness.png",packed.EncodeToPNG());
                Object.DestroyImmediate(packed);
                importer.isReadable=false;importer.SaveAndReimport();
            }
            AssetDatabase.Refresh();
            Debug.Log("V4 PREPARED: 12 Poly Haven source maps and three Unity metallic/smoothness masks.");
        }

        public static void RunBuiltIn()
        {
            PrepareAssets();
            GraphicsSettings.defaultRenderPipeline=null;
            QualitySettings.renderPipeline=null;
            Run();
        }

        public static void RunURP()
        {
            Type rendererType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEngine.Rendering.Universal.UniversalRendererData")).FirstOrDefault(t=>t!=null);
            Type pipelineType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset")).FirstOrDefault(t=>t!=null);
            if(rendererType==null||pipelineType==null)throw new InvalidOperationException("Install URP in this validation project.");
            const string folder="Assets/ArcadeLockdown/Rendering";
            Directory.CreateDirectory(folder);
            ScriptableObject renderer=AssetDatabase.LoadAssetAtPath<ScriptableObject>(folder+"/ArcadeRenderer.asset");
            if(renderer==null)
            {
                renderer=ScriptableObject.CreateInstance(rendererType);
                renderer.name="Arcade Renderer";
                AssetDatabase.CreateAsset(renderer,folder+"/ArcadeRenderer.asset");
            }
            RenderPipelineAsset pipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(folder+"/ArcadeURP.asset");
            if(pipeline==null)
            {
                MethodInfo factory=pipelineType.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static).FirstOrDefault(m=>m.Name=="Create"&&m.GetParameters().Length==1&&m.GetParameters()[0].ParameterType.IsAssignableFrom(rendererType));
                pipeline = factory!=null?(RenderPipelineAsset)factory.Invoke(null,new object[]{renderer}):(RenderPipelineAsset)ScriptableObject.CreateInstance(pipelineType);
                SerializedObject settings=new SerializedObject(pipeline);
                SerializedProperty renderers=settings.FindProperty("m_RendererDataList");
                renderers.arraySize=1;renderers.GetArrayElementAtIndex(0).objectReferenceValue=renderer;
                SetInt(settings,"m_AdditionalLightsRenderingMode",1);
                SetInt(settings,"m_AdditionalLightsPerObjectLimit",8);
                SetInt(settings,"m_AdditionalLightsShadowmapResolution",2048);
                SerializedProperty shadows=settings.FindProperty("m_AdditionalLightShadowsSupported");if(shadows!=null)shadows.boolValue=true;
                settings.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(pipeline,folder+"/ArcadeURP.asset");
            }
            GraphicsSettings.defaultRenderPipeline=pipeline;
            QualitySettings.renderPipeline=pipeline;
            AssetDatabase.SaveAssets();
            Run();
        }

        private static void SetInt(SerializedObject settings,string field,int value)
        {
            SerializedProperty p=settings.FindProperty(field);if(p!=null)p.intValue=value;
        }

        private static void Run()
        {
            SessionState.SetBool(ActiveKey,true);
            EditorSceneManager.OpenScene("Assets/ArcadeLockdown/Scenes/ArcadeLockdown.unity");
            EditorApplication.isPlaying=true;
        }

        private static void CaptureError(string message,string stack,LogType type)
        {
            if((type==LogType.Error||type==LogType.Exception||type==LogType.Assert) && !stack.Contains("UnityEditor.Search."))errors++;
        }

        private static void Tick()
        {
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.timeSinceStartup<next||Time.frameCount<requiredFrame)return;
            try
            {
                ArcadeGameManager manager=ArcadeGameManager.Instance;
                ThirdPersonController player=Object.FindFirstObjectByType<ThirdPersonController>();
                if(manager==null||player==null)throw new Exception("The game did not start.");
                Camera camera=player.viewCamera;
                if(stage==0)
                {
                    manager.SendMessage("StartPlaying");
                    Bounds local=CharacterMeasurements.Measure(player.visualRoot);
                    float height=local.size.y*player.visualRoot.lossyScale.y;
                    Debug.Log("V4 CHARACTER actual animated height="+height+"m; controller="+player.GetComponent<CharacterController>().height+"m");
                    if(height<1.65f||height>1.87f)throw new Exception("Player is not human-sized: "+height);
                    foreach(Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                        foreach(Material material in renderer.sharedMaterials)
                            if(material==null||material.shader==null||!material.shader.isSupported||material.shader.name.Contains("InternalError"))throw new Exception("Invalid material on "+renderer.name);
                    int shadowLights=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Count(l=>l.shadows!=LightShadows.None);
                    if(shadowLights>3)throw new Exception("Excessive shadow lights: "+shadowLights);
                    movingStart=Object.FindFirstObjectByType<DisplayMachineAnimator>().movingPart.localPosition;
                    Directory.CreateDirectory(output);
                    target=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32);
                    camera.targetTexture=target;
                    next=EditorApplication.timeSinceStartup+2;
                }
                else if(stage==1)
                {
                    Save(camera,"01-player-and-arcade");
                    Bounds animated=CharacterMeasurements.Measure(player.visualRoot);
                    float height=animated.size.y*player.visualRoot.lossyScale.y;
                    if(height>1.90f)throw new Exception("Animation changed player scale.");
                    Debug.Log("V4 animated player height after several frames="+height);
                    player.enabled=false;
                    camera.transform.SetPositionAndRotation(new Vector3(-6.1f,1.60f,4.05f),Quaternion.LookRotation(new Vector3(-7.6f,1.26f,6.4f)-new Vector3(-6.1f,1.6f,4.05f)));
                    next=EditorApplication.timeSinceStartup+1;
                }
                else if(stage==2)
                {
                    Save(camera,"02-cabinet-closeup");
                    camera.transform.position=new Vector3(6.9f,1.65f,1.8f);
                    camera.transform.LookAt(new Vector3(9f,1.10f,5.4f));
                    next=EditorApplication.timeSinceStartup+1;
                }
                else if(stage==3)
                {
                    Save(camera,"03-claw-and-room");
                    camera.transform.position=new Vector3(-1.8f,1.75f,-4.0f);
                    camera.transform.LookAt(new Vector3(-5.4f,1.05f,-1.3f));
                    next=EditorApplication.timeSinceStartup+1;
                }
                else if(stage==4)
                {
                    Save(camera,"04-attractions");
                    ApproachToken(player,0);
                    next=EditorApplication.timeSinceStartup+.8f;
                }
                else if(stage==5 || stage==6)
                {
                    if(GameObject.Find(tokenOrder[stage-5])!=null)throw new Exception("Auto pickup failed for "+tokenOrder[stage-5]);
                    ApproachToken(player,stage-4);
                    next=EditorApplication.timeSinceStartup+.8f;
                }
                else if(stage==7)
                {
                    if(Object.FindObjectsByType<TokenPickup>(FindObjectsSortMode.None).Length!=0)throw new Exception("Tokens were not collected.");
                    if(errors>0)throw new Exception("Runtime logged "+errors+" errors.");
                    Debug.Log("V4 PLAY TEST PASSED: normal-sized animated player, three reachable and collected tokens, valid shaders, limited shadows, four rendered previews. Pipeline="+(GraphicsSettings.currentRenderPipeline==null?"Built-in":"URP"));
                    SessionState.SetBool(ActiveKey,false);
                    if(Array.IndexOf(Environment.GetCommandLineArgs(),"-arcadeExport")>=0)
                        ArcadePrefabExport.Export();
                    camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);
                    EditorApplication.Exit(0);return;
                }
                stage++;
                requiredFrame=Time.frameCount+6;
            }
            catch(Exception e)
            {
                SessionState.SetBool(ActiveKey,false);
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        private static void ApproachToken(ThirdPersonController player,int index)
        {
            TokenPickup token=GameObject.Find(tokenOrder[index]).GetComponent<TokenPickup>();
            Vector3 near=token.transform.position+Vector3.back*.8f;near.y=.02f;
            CharacterController body=player.GetComponent<CharacterController>();body.enabled=false;player.transform.position=near;body.enabled=true;
            if(!token.IsReachable(player.transform,1.35f))throw new Exception("Token cannot be reached: "+token.name);
            // Let the real TokenPickup.Update collect it on the next game frame.
        }

        private static void Save(Camera camera,string name)
        {
            RenderTexture previous=RenderTexture.active;RenderTexture.active=target;
            Texture2D image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
            string suffix=GraphicsSettings.currentRenderPipeline==null?"-builtin":"-urp";
            File.WriteAllBytes(Path.Combine(output,name+suffix+".png"),image.EncodeToPNG());
            Object.DestroyImmediate(image);RenderTexture.active=previous;
        }
    }
}
#endif
