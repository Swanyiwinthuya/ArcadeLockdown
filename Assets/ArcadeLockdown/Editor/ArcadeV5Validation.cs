#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace ArcadeLockdown.Editor
{
    [InitializeOnLoad]
    public static class ArcadeV5Validation
    {
        const string Key="ArcadeV5.Run";
        static int stage,run,frame,errors;
        static double next,deadline;
        static float frozen;
        static Vector3[] roomPositions;
        static string output;
        static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static ArcadeV5Validation()
        {
            if(SessionState.GetBool(Key,false))Attach();
        }
        static void Attach()
        {
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;
            Application.logMessageReceived-=Log;Application.logMessageReceived+=Log;
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../deliverables/Arcade-v5-Previews"));
            Directory.CreateDirectory(output);next=EditorApplication.timeSinceStartup+3;deadline=EditorApplication.timeSinceStartup+240;
        }
        public static void Run()
        {
            foreach(bool female in new[]{false,true})
            {
                string key=new EscapeRoute(female).BestTimeKey;
                SessionState.SetBool(key+".Had",PlayerPrefs.HasKey(key));
                SessionState.SetFloat(key+".Previous",PlayerPrefs.GetFloat(key));
                PlayerPrefs.DeleteKey(key);
            }
            SessionState.SetBool(Key,true);
            EditorSceneManager.OpenScene("Assets/ArcadeLockdown/Scenes/ArcadeLockdown.unity");
            EditorApplication.isPlaying=true;
        }
        static void Log(string text,string stack,LogType type)
        {
            if((type==LogType.Error||type==LogType.Exception||type==LogType.Assert)&&!stack.Contains("UnityEditor.Search.")) errors++;
        }
        static void Assert(bool condition,string text){if(!condition)throw new Exception(text);}
        static void Call(ArcadeGameManager manager,string method,params object[] args)=>typeof(ArcadeGameManager).GetMethod(method,Private).Invoke(manager,args);
        static void Set(ArcadeGameManager manager,string field,object value)=>typeof(ArcadeGameManager).GetField(field,Private).SetValue(manager,value);
        static object Get(ArcadeGameManager manager,string field)=>typeof(ArcadeGameManager).GetField(field,Private).GetValue(manager);
        static void Use(ArcadeGameManager m,InteractionKind kind)
        {
            Interactable item=Object.FindObjectsByType<Interactable>().First(i=>i.kind==kind);
            m.HandleInteraction(item);
        }
        static void Code(ArcadeGameManager m,string code){Set(m,"enteredCode",code);Call(m,"SubmitCode");}
        static void Close(ArcadeGameManager m){if(m.StateName=="Modal")Call(m,"CloseModal",false);}
        static void Tick()
        {
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            if(EditorApplication.timeSinceStartup>deadline){Fail(new Exception("Validation timed out"));return;}
            if(EditorApplication.timeSinceStartup<next||Time.frameCount<frame)return;
            try
            {
                ArcadeGameManager m=ArcadeGameManager.Instance;
                ThirdPersonController p=Object.FindAnyObjectByType<ThirdPersonController>();
                Assert(m!=null&&p!=null,"Runtime bootstrap failed");
                if(stage==0)
                {
                    Assert(m.StateName=="MainMenu","Retry must return to main menu");
                    Assert(m.RemainingTime==900,"Menu must not spend the 15 minute allowance");
                    Transform rooms=GameObject.Find("ROOMS - Connected Level").transform;
                    Vector3[] positions=rooms.GetComponentsInChildren<Transform>().Select(t=>t.position).ToArray();
                    if(run==0)roomPositions=positions;
                    else Assert(positions.SequenceEqual(roomPositions),"Route changed room geometry");
                    if(run<2)
                    {
                        m.ChooseCharacter(run==1);
                        Assert(m.StateName=="Playing","Character choice did not start run");
                        string clues=string.Join("\n",Object.FindObjectsByType<TextMesh>().Select(t=>t.text))+
                            string.Join("\n",Object.FindObjectsByType<Interactable>().Select(t=>t.information));
                        Assert(clues.Contains(run==0?"MAX":"AVA")&&clues.Contains(run==0?"1994":"1997"),"Champion clue missing");
                        Assert(clues.Contains(run==0?"NO. 07":"NO. 12")&&clues.Contains(run==0?"1992":"1993"),"Computer clues inconsistent");
                        Assert(clues.Contains(run==0?"BLUE\nRED\nYELLOW\nRED":"YELLOW\nRED\nBLUE\nRED"),"Token clue inconsistent");
                        Assert(clues.Contains(run==0?"SPACE\nRACE\nBLOCK\nPAC":"BLOCK\nPAC\nSPACE\nRACE"),"Exit order inconsistent");
                    }
                    else m.ChooseCharacter(true);
                }
                else if(stage==1)
                {
                    Assert(m.RemainingTime<900&&m.RemainingTime>880,"Countdown not running");
                    Bounds bounds=CharacterMeasurements.Measure(p.visualRoot);
                    Debug.Log("V5 CHARACTER "+m.Route.Name+" animated bounds "+bounds);
                    Assert(bounds.size.y>1.45f&&bounds.size.y<1.95f,"Character size invalid: "+bounds.size.y);
                    Animator a=p.teenCharacterAnimator.animator;
                    Assert(a.isHuman&&a.runtimeAnimatorController!=null,"Missing humanoid animation");
                    Assert(a.GetCurrentAnimatorStateInfo(0).IsName("Idle"),"Idle animation not playing");
                    foreach(Renderer r in Object.FindObjectsByType<Renderer>())foreach(Material mat in r.sharedMaterials)
                        Assert(mat!=null&&mat.shader!=null&&mat.shader.isSupported&&!mat.shader.name.Contains("InternalError"),"Invalid material "+r.name);
                    if(run<2)
                    {
                        p.enabled=false;
                        PlaceCamera(p.viewCamera,p.transform.position+new Vector3(1.45f,1.28f,2.4f),p.transform.position+Vector3.up*.96f);
                    }
                    else
                    {
                        Call(m,"Pause");frozen=m.RemainingTime;
                        Assert(!m.CanControlPlayer,"Paused movement remains enabled");
                    }
                }
                else if(stage==2)
                {
                    if(run<2)
                    {
                        Save(p.viewCamera,m.Route.Name+"-character");
                        Portrait(p,m.Route.Name=="LEO"?"Leo":"Maya");
                        p.teenCharacterAnimator.SetMovement(1,false);
                        PlaceCamera(p.viewCamera,new Vector3(13.2f,1.9f,2.9f),new Vector3(15.65f,1.13f,4.66f));
                    }
                    else
                    {
                        Assert(m.RemainingTime==frozen,"Pause did not freeze timer");Call(m,"Resume");
                        Call(m,"OpenJournal");frozen=m.RemainingTime;
                    }
                }
                else if(stage==3)
                {
                    if(run<2)
                    {
                        Save(p.viewCamera,m.Route.Name+"-office");
                        Assert(p.teenCharacterAnimator.animator.GetCurrentAnimatorStateInfo(0).IsName("Walk"),"Walk blend failed");
                        p.teenCharacterAnimator.SetMovement(1,true);
                        Use(m,InteractionKind.LockerKeypad);
                        Code(m,run==0?"1997":"1994");Assert(m.StateName=="Modal"&&!(bool)Get(m,"lockerSolved"),"Other route locker code accepted");
                        Code(m,m.Route.Locker);Assert((bool)Get(m,"lockerSolved"),"Locker failed");
                        Use(m,InteractionKind.OfficeDoor);Assert((bool)Get(m,"officeOpened"),"Office key failed");
                        Use(m,InteractionKind.OfficeComputer);Code(m,m.Route.Computer);Assert((bool)Get(m,"computerSolved"),"Computer failed");Close(m);
                        Approach(p,"Red token");
                    }
                    else
                    {
                        Assert(m.RemainingTime<frozen,"Reading journal should spend time");
                        Set(m,"elapsedTime",899.95f);
                    }
                }
                else if(stage==4)
                {
                    if(run<2)
                    {
                        Assert(GameObject.Find("Red token")==null,"Red token auto pickup failed");
                        Assert(p.teenCharacterAnimator.animator.GetCurrentAnimatorStateInfo(0).IsName("Run"),"Run blend failed");
                        Approach(p,"Blue token");
                    }
                    else
                    {
                        Assert(m.StateName=="GameOver"&&m.RemainingTime==0&&!m.CanControlPlayer,"Timeout failed inside modal");
                        Call(m,"CloseModal",false);
                        Assert(m.StateName=="GameOver","Modal can revive timed-out game");
                        ScreenCapture.CaptureScreenshot(Path.Combine(output,"Time-up.png"));
                    }
                }
                else if(stage==5)
                {
                    if(run<2){Assert(GameObject.Find("Blue token")==null,"Blue token auto pickup failed");Approach(p,"Yellow token");}
                    else {Call(m,"RestartGame");run=3;stage=-1;}
                }
                else if(stage==6)
                {
                    Assert(GameObject.Find("Yellow token")==null,"Yellow token auto pickup failed");
                    Use(m,InteractionKind.PrizeKeypad);Code(m,m.Route.Prize);Assert((bool)Get(m,"prizeSolved"),"Token puzzle failed");
                    Use(m,InteractionKind.StorageDoor);Use(m,InteractionKind.Box42);Close(m);
                    Use(m,InteractionKind.PowerConsole);int[] wires=(int[])Get(m,"wireRotations");Array.Copy(m.Route.Circuit,wires,4);Call(m,"CheckCircuit");Assert((bool)Get(m,"powerRestored"),"Circuit failed");
                    Use(m,InteractionKind.ExitKeypad);Code(m,m.Route.Exit);
                    Assert(m.StateName=="Escaped"&&(int)Get(m,"puzzlesSolved")==6,"Escape route failed");
                    Assert(PlayerPrefs.GetFloat(m.Route.BestTimeKey)>0,"Completion record not saved");
                    if(run==0)Assert(!PlayerPrefs.HasKey(new EscapeRoute(true).BestTimeKey),"Leo incorrectly completed Maya route");
                    else Assert(PlayerPrefs.GetFloat(new EscapeRoute(false).BestTimeKey)>0,"Maya erased Leo completion");
                    Debug.Log("V5 ROUTE PASSED "+m.Route.Name+": 6 puzzles; wrong route rejected; all 3 auto pickups; idle/walk/run; same rooms.");
                }
                else if(stage==7)
                {
                    Call(m,"RestartGame");run++;stage=-1;
                }
                if(run==3&&stage==0)
                {
                    Set(m,"elapsedTime",899.95f);stage=10;
                }
                else if(run==3&&stage==11)
                {
                    Assert(m.StateName=="GameOver"&&m.RemainingTime==0,"Timeout failed while playing");
                    Assert(errors==0,"Runtime errors: "+errors);
                    Debug.Log("V5 ALL TESTS PASSED: two routes, independent records, room identity, countdown, pause, modal/playing timeout, restart, character rigs and shaders.");
                    RestoreRecords();SessionState.SetBool(Key,false);EditorApplication.Exit(0);return;
                }
                stage++;frame=Time.frameCount+15;next=EditorApplication.timeSinceStartup+.7;
            }
            catch(Exception ex){Fail(ex);}
        }
        static void RestoreRecords()
        {
            foreach(bool female in new[]{false,true})
            {
                string key=new EscapeRoute(female).BestTimeKey;
                if(SessionState.GetBool(key+".Had",false))PlayerPrefs.SetFloat(key,SessionState.GetFloat(key+".Previous",0));
                else PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();
        }
        static void Fail(Exception ex){RestoreRecords();SessionState.SetBool(Key,false);Debug.LogException(ex);EditorApplication.Exit(1);}
        static void Approach(ThirdPersonController p,string name)
        {
            TokenPickup token=GameObject.Find(name).GetComponent<TokenPickup>();
            CharacterController c=p.GetComponent<CharacterController>();c.enabled=false;
            p.transform.position=new Vector3(token.transform.position.x,.02f,token.transform.position.z-.8f);c.enabled=true;
            Assert(token.IsReachable(p.transform,1.35f),name+" unreachable");
        }
        static void PlaceCamera(Camera c,Vector3 pos,Vector3 at){c.transform.position=pos;c.transform.LookAt(at);}
        static void Portrait(ThirdPersonController p,string name)
        {
            Camera camera=p.viewCamera;Vector3 position=camera.transform.position;Quaternion rotation=camera.transform.rotation;
            float fov=camera.fieldOfView;
            GameObject lightObject=new GameObject("Temporary portrait lighting");Light light=lightObject.AddComponent<Light>();
            light.type=LightType.Directional;light.intensity=1.6f;light.shadows=LightShadows.None;light.transform.rotation=Quaternion.Euler(30,205,0);
            camera.fieldOfView=36;PlaceCamera(camera,p.transform.position+new Vector3(.35f,1.04f,2.85f),p.transform.position+Vector3.up*.91f);
            string dir="Assets/ArcadeLockdown/Resources/Characters/Portraits";Directory.CreateDirectory(dir);
            string previousOutput=output;output=Path.GetFullPath(dir);Save(camera,name,640,800);output=previousOutput;
            camera.fieldOfView=fov;camera.transform.SetPositionAndRotation(position,rotation);Object.Destroy(lightObject);
        }
        static void Save(Camera camera,string name,int width=1600,int height=1000)
        {
            RenderTexture rt=new RenderTexture(width,height,24);camera.targetTexture=rt;
            // URP renders the camera in the normal player loop; this fallback uses the last frame screenshot elsewhere.
            camera.Render();
            RenderTexture previous=RenderTexture.active;RenderTexture.active=rt;
            Texture2D image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
        }
    }
}
#endif
