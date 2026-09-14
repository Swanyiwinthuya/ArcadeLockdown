#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ArcadeLockdown.Editor
{
    public static class ArcadePrefabExport
    {
        const string Folder="Assets/ArcadeLockdown/Prefabs";
        static readonly Dictionary<Object,Object> Saved=new Dictionary<Object,Object>();
        static int counter;

        public static void Export()
        {
            Directory.CreateDirectory(Folder+"/Meshes");
            Directory.CreateDirectory(Folder+"/Materials");
            Directory.CreateDirectory(Folder+"/Textures");
            AssetDatabase.Refresh();
            if(GameObject.Find("ARCADE LOCKDOWN - Generated 3D World")==null)
            {
                GameObject managerObject=new GameObject("Prefab export manager");
                ArcadeGameManager manager=managerObject.AddComponent<ArcadeGameManager>();
                ArcadeWorldBuilder.Build(manager);
            }
            string[] names={"Champion machine","Galaxy cabinet","Turbo cabinet","Ninja cabinet",
                "PAC-RAT powered cabinet","SPACE RAID powered cabinet","RACE 199X powered cabinet","BLOCK DROP powered cabinet",
                "Working claw machine - steel and glass","Air hockey table - stainless rails",
                "Basketball challenge - cage and return ramp","Motorized prize wheel","Vending machine - refrigerated drinks",
                "Manager desk", "Manager chair - upholstered swivel"};
            foreach(string name in names)
            {
                GameObject original=GameObject.Find(name);
                if(original==null)throw new System.Exception("Prefab source missing: "+name);
                GameObject copy=Object.Instantiate(original);
                copy.name=name;copy.transform.SetParent(null);copy.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                DisplayMachineAnimator motion=copy.GetComponent<DisplayMachineAnimator>();
                if(motion!=null && motion.movingPart!=null)
                {
                    if(name.StartsWith("Working claw"))motion.movingPart.localPosition=new Vector3(0,1.86f,0);
                    else if(name.StartsWith("Air hockey"))motion.movingPart.localPosition=new Vector3(0,.906f,0);
                    else if(name.StartsWith("Basketball"))motion.movingPart.localPosition=new Vector3(0,1.12f,-.76f);
                    else if(name.StartsWith("Motorized"))motion.movingPart.localPosition=new Vector3(0,1.39f,-.16f);
                    motion.movingPart.localRotation=Quaternion.identity;
                }
                Persist(copy);
                PrefabUtility.SaveAsPrefabAsset(copy,Folder+"/"+Safe(name)+".prefab");
                Object.DestroyImmediate(copy);
            }
            GameObject rooms=new GameObject("Connected arcade rooms");
            foreach(string name in new[]{"ROOMS - Connected Level","ARCHITECTURAL FINISHES - wall panels ceiling and services","LIGHTING - Practical fixtures, neon and reflections"})
            {
                GameObject copy=Object.Instantiate(GameObject.Find(name));
                copy.name=name;copy.transform.SetParent(rooms.transform,true);
            }
            Persist(rooms);PrefabUtility.SaveAsPrefabAsset(rooms,Folder+"/ConnectedArcadeRooms.prefab");Object.DestroyImmediate(rooms);
            foreach(bool female in new[]{false,true})
            {
                string name=female?"Maya":"Leo";
                GameObject character=new GameObject(name+" - dressed animated character");
                ArcadeWorldBuilder.CreateTeenCharacter(character.transform,female);
                Persist(character);
                PrefabUtility.SaveAsPrefabAsset(character,Folder+"/"+name+"-Character.prefab");
                Object.DestroyImmediate(character);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.ExportPackage("Assets/ArcadeLockdown",Path.GetFullPath(Path.Combine(Application.dataPath,"../../ArcadeLockdown-TwoRoutes-v5.unitypackage")),ExportPackageOptions.Recurse);
            Debug.Log("V5 PREFABS EXPORTED: 13 machines, office desk/chair, two dressed characters, connected-room prefab.");
        }

        private static void Persist(GameObject root)
        {
            foreach(SkinnedMeshRenderer skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                skin.sharedMesh=(Mesh)Save(skin.sharedMesh,"Meshes");
            foreach(MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
                filter.sharedMesh=(Mesh)Save(filter.sharedMesh,"Meshes");
            foreach(Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    Material original=materials[i];
                    if(original==null)continue;
                    if(!Saved.TryGetValue(original,out Object saved))
                    {
                        Material copy=Object.Instantiate(original);
                        if(renderer.GetComponent<TextMesh>()!=null)copy.mainTexture=null;
                        else foreach(string property in copy.GetTexturePropertyNames())
                        {
                            Texture texture=copy.GetTexture(property);
                            if(texture is Texture2D && !AssetDatabase.Contains(texture))
                                copy.SetTexture(property,(Texture)Save(texture,"Textures"));
                        }
                        saved=Write(copy,"Materials");Saved[original]=saved;
                    }
                    materials[i]=(Material)saved;
                }
                renderer.sharedMaterials=materials;
            }
        }

        private static Object Save(Object asset,string subfolder)
        {
            if(asset==null||AssetDatabase.Contains(asset))return asset;
            if(Saved.TryGetValue(asset,out Object saved))return saved;
            saved=Write(Object.Instantiate(asset),subfolder);Saved[asset]=saved;return saved;
        }
        private static Object Write(Object asset,string subfolder)
        {
            string path=Folder+"/"+subfolder+"/"+(counter++).ToString("D4")+"-"+Safe(asset.name)+".asset";
            Object existing=AssetDatabase.LoadMainAssetAtPath(path);
            if(existing!=null) {EditorUtility.CopySerialized(asset,existing);Object.DestroyImmediate(asset);return existing;}
            AssetDatabase.CreateAsset(asset,path);return asset;
        }
        private static string Safe(string name)=>Regex.Replace(name,@"[^a-zA-Z0-9_-]+","-").Trim('-');
    }
}
#endif
