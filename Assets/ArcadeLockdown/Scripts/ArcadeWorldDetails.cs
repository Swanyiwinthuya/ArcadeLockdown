using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcadeLockdown
{
    public static partial class ArcadeWorldBuilder
    {
        private static readonly Dictionary<string, Mesh> RoundedMeshes = new Dictionary<string, Mesh>();

        private static void CreateDetailedMaterials()
        {
            Materials["Wall"] = Surface("Warm gray painted plaster", "painted_plaster_wall", new Color(.62f,.65f,.63f), new Vector2(3,1), 0, .24f);
            Materials["Wall2"] = Surface("Office cream plaster", "painted_plaster_wall", new Color(.85f,.78f,.65f), new Vector2(3,1), 0, .23f);
            Materials["Wood"] = Surface("Real walnut veneer", "wood_cabinet_worn_long", new Color(.72f,.56f,.41f), Vector2.one, 0, .38f);
            Materials["CabinetWood"] = Surface("Dark stained cabinet plywood", "wood_cabinet_worn_long", new Color(.60f,.59f,.57f), new Vector2(1,2), 0, .4f);
            Materials["Wainscot"] = Surface("Walnut wall panels", "wood_cabinet_worn_long", new Color(.62f,.52f,.41f), new Vector2(3,1), 0, .32f);
            Materials["ServiceFloor"] = Surface("Workshop steel tread plate", "metal_plate", new Color(.5f,.55f,.58f), new Vector2(3,3), .75f, .35f);
            Materials["Brass"] = MakeMaterial("Satin brass", new Color(.58f,.4f,.16f), false, .75f, .52f);
            Materials["Rubber"] = MakeMaterial("Rubber edge molding", new Color(.028f,.032f,.039f), false, 0, .23f);
            Materials["CeilingTile"] = Surface("Acoustic ceiling tile", "painted_plaster_wall", new Color(.58f,.61f,.62f), Vector2.one, 0, .1f);
            Materials["CeilingGrid"] = MakeMaterial("Ceiling aluminum grid", new Color(.22f,.25f,.26f), false, .65f, .35f);
            Materials["SignFace"] = MakeMaterial("Printed charcoal sign", new Color(.028f,.043f,.051f), false, .1f, .34f);
            Materials["ClearGlass"] = MakeGlass();
            Materials["FloorA"].color = new Color(.65f,.72f,.86f);
            SetMaterialColor(Materials["FloorA"], new Color(.65f,.72f,.86f));
            SetMaterialColor(Materials["FloorB"], new Color(.72f,.65f,.66f));
        }

        private static Material Surface(string name, string asset, Color tint, Vector2 tiling, float metallic, float smoothness)
        {
            string path = "PBR/PolyHaven/" + asset;
            Material material = MakePbrMaterial(name, tint, path + "_diff_1k", path + "_nor_gl_1k", path + "_ao_1k", tiling, metallic, smoothness);
            Texture mask = Resources.Load<Texture2D>(path + "_metallic_smoothness");
            if (mask != null)
            {
                material.SetTexture("_MetallicGlossMap", mask);
                material.EnableKeyword(material.HasProperty("_Surface") ? "_METALLICSPECGLOSSMAP" : "_METALLICGLOSSMAP");
                if (material.HasProperty("_GlossMapScale")) material.SetFloat("_GlossMapScale", 1f);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 1f);
            }
            if (material.HasProperty("_BumpScale")) material.SetFloat("_BumpScale", .35f);
            return material;
        }

        private static Material MakeGlass()
        {
            Material glass = MakeMaterial("Clear display glass - pipeline compatible", new Color(.65f,.82f,.86f,.12f), false, .05f, .92f);
            bool urp = glass.HasProperty("_Surface");
            if (urp) glass.SetFloat("_Surface", 1);
            else if (glass.HasProperty("_Mode")) glass.SetFloat("_Mode", 3);
            glass.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            glass.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            glass.SetInt("_ZWrite", 0);
            glass.SetOverrideTag("RenderType", "Transparent");
            glass.EnableKeyword(urp ? "_SURFACE_TYPE_TRANSPARENT" : "_ALPHABLEND_ON");
            glass.renderQueue = 3000;
            return glass;
        }

        // Real dimensions are stored in the mesh so bevels retain their width.
        private static GameObject DetailBox(string name, Transform parent, Vector3 position, Vector3 size, Material material, float radius = .012f, bool collider = false)
        {
            string key = size.ToString("F4") + radius.ToString("F4");
            if (!RoundedMeshes.TryGetValue(key, out Mesh mesh) || mesh == null)
            {
                mesh = RoundedBoxMesh(size, radius);
                RoundedMeshes[key] = mesh;
            }
            GameObject part = new GameObject(name);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (collider) part.AddComponent<BoxCollider>().size = size;
            return part;
        }

        private static Mesh RoundedBoxMesh(Vector3 size, float radius)
        {
            Vector3 half = size * .5f;
            radius = Mathf.Min(radius, Mathf.Min(half.x, Mathf.Min(half.y, half.z)) * .95f);
            Vector3 inner = half - Vector3.one * radius;
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();
            Vector3[] directions = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            const int n = 7;
            foreach (Vector3 normal in directions)
            {
                Vector3 u = Mathf.Abs(normal.y) > .5f ? Vector3.right : Vector3.Cross(Vector3.up, normal);
                Vector3 v = Vector3.Cross(normal, u);
                float hu = Vector3.Dot(Abs(u), half);
                float hv = Vector3.Dot(Abs(v), half);
                int start = vertices.Count;
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float px = EdgeCoordinate(x, hu, radius);
                        float py = EdgeCoordinate(y, hv, radius);
                        Vector3 point = Vector3.Scale(normal, half) + u * px + v * py;
                        Vector3 clamped = new Vector3(Mathf.Clamp(point.x,-inner.x,inner.x), Mathf.Clamp(point.y,-inner.y,inner.y), Mathf.Clamp(point.z,-inner.z,inner.z));
                        Vector3 outward = (point - clamped).normalized;
                        vertices.Add(clamped + outward * radius);
                        normals.Add(outward);
                        uvs.Add(new Vector2(px / (hu * 2) + .5f, py / (hv * 2) + .5f));
                    }
                for (int y = 0; y < n - 1; y++)
                    for (int x = 0; x < n - 1; x++)
                    {
                        int a = start + y*n+x, b = a+1, c = a+n, d = c+1;
                        triangles.AddRange(new[] { a,b,c,b,d,c });
                    }
            }
            Mesh result = new Mesh { name = "Beveled solid " + size.ToString("F2") };
            result.SetVertices(vertices); result.SetNormals(normals); result.SetUVs(0, uvs); result.SetTriangles(triangles, 0);
            result.RecalculateBounds(); result.RecalculateTangents();
            return result;
        }

        private static float EdgeCoordinate(int i, float half, float radius)
        {
            switch (i)
            {
                case 0: return -half;
                case 1: return -half + radius * .45f;
                case 2: return -half + radius;
                case 3: return 0;
                case 4: return half - radius;
                case 5: return half - radius * .45f;
                default: return half;
            }
        }

        private static Vector3 Abs(Vector3 p) => new Vector3(Mathf.Abs(p.x), Mathf.Abs(p.y), Mathf.Abs(p.z));

        private static Transform PartRoot(string name, Transform parent, Vector3 position, float yaw = 0)
        {
            Transform result = new GameObject(name).transform;
            result.SetParent(parent, false);
            result.localPosition = position;
            result.localRotation = Quaternion.Euler(0, yaw, 0);
            return result;
        }

        private static void Rod(string name, Transform parent, Vector3 from, Vector3 to, float diameter, Material material)
        {
            Vector3 delta = to - from;
            GameObject rod = Primitive(name, PrimitiveType.Cylinder, parent, (from+to)*.5f, new Vector3(diameter,delta.magnitude*.5f,diameter), material, false);
            rod.transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta);
        }

        private static void Screw(Transform parent, Vector3 position)
        {
            DetailBox("Recessed steel fastener", parent, position, new Vector3(.023f,.023f,.006f), Materials["Metal"], .007f);
            Cube("Fastener slot", parent, position + Vector3.back*.004f, new Vector3(.015f,.003f,.003f), Materials["Black"], false);
        }

        private static GameObject BuildRealisticCabinet(Transform parent, string name, Vector3 position, float yaw, Material accent, string marquee, string screenText)
        {
            Transform root = PartRoot(name, parent, position, yaw);
            Color color = accent.HasProperty("_BaseColor") ? accent.GetColor("_BaseColor") : accent.color;
            Material paint = MakeMaterial(marquee + " enamel", Color.Lerp(color, new Color(.08f,.09f,.1f), .72f), false, .28f, .48f);
            Material marqueeMaterial = MakeMaterial(marquee + " printed marquee", new Color(.025f,.04f,.055f), false, .08f, .46f);
            DetailBox("Plywood lower cabinet",root,new Vector3(0,.57f,0),new Vector3(.9f,1.12f,.82f),Materials["CabinetWood"],.026f);
            DetailBox("Rear screen enclosure",root,new Vector3(0,1.47f,.20f),new Vector3(.91f,.87f,.50f),Materials["CabinetWood"],.026f);
            DetailBox("Overhanging marquee hood",root,new Vector3(0,1.96f,.03f),new Vector3(.99f,.29f,.9f),Materials["CabinetWood"],.025f);
            for (int side = -1; side <= 1; side += 2)
            {
                DetailBox("Enamel side panel",root,new Vector3(side*.474f,.70f,.03f),new Vector3(.045f,1.34f,.87f),paint,.017f);
                DetailBox("Upper side cheek",root,new Vector3(side*.474f,1.51f,.25f),new Vector3(.045f,.71f,.50f),paint,.018f);
                Rod("Rubber T molding",root,new Vector3(side*.49f,.04f,-.41f),new Vector3(side*.49f,1.13f,-.41f),.024f,Materials["Rubber"]);
                Rod("Sloping T molding",root,new Vector3(side*.49f,1.14f,-.41f),new Vector3(side*.49f,1.79f,.0f),.024f,Materials["Rubber"]);
                DetailBox("Side graphic stripe",root,new Vector3(side*.5f,.77f,-.1f),new Vector3(.005f,.78f,.09f),accent,.002f).transform.localRotation = Quaternion.Euler(20f,0,0);
                DetailBox("Rubber adjustable foot",root,new Vector3(side*.34f,.035f,-.25f),new Vector3(.12f,.07f,.12f),Materials["Rubber"],.015f);
            }
            Transform display = PartRoot("Sloped CRT assembly", root, new Vector3(0,1.49f,-.19f));
            display.localRotation = Quaternion.Euler(12f,0,0);
            DetailBox("Recessed molded CRT bezel",display,Vector3.zero,new Vector3(.83f,.65f,.18f),Materials["Rubber"],.045f);
            GameObject screen = DetailBox("Screen",root,display.localPosition + display.localRotation * new Vector3(0,0,-.102f),new Vector3(.687f,.49f,.028f),Materials["Glass"],.04f);
            screen.transform.localRotation = display.localRotation;
            ArcadeScreenAnimator attract = screen.AddComponent<ArcadeScreenAnimator>();
            attract.accent = color;
            attract.variant = marquee.Contains("RACE") || marquee.Contains("TURBO") ? 1 : marquee.Contains("STAR") || marquee.Contains("GALAXY") || marquee.Contains("SPACE") ? 0 : 2;
            TextMesh label = CreateText(screenText,root,display.localPosition + display.localRotation * new Vector3(0,-.17f,-.127f),display.localRotation,.014f,Color.white,64);

            Transform controls = PartRoot("Sloped control deck",root,new Vector3(0,1.10f,-.38f));
            controls.localRotation = Quaternion.Euler(-8,0,0);
            DetailBox("Metal control deck",controls,Vector3.zero,new Vector3(.99f,.10f,.43f),Materials["DarkMetal"],.023f);
            DetailBox("Control deck edge stripe",controls,new Vector3(0,0,-.216f),new Vector3(.9f,.025f,.008f),accent,.003f);
            Transform stick = PartRoot("Animated joystick",controls,new Vector3(-.23f,.08f,-.01f));
            Primitive("Rubber joystick boot",PrimitiveType.Cylinder,stick,Vector3.zero,new Vector3(.10f,.017f,.10f),Materials["Rubber"],false);
            Rod("Chrome joystick shaft",stick,Vector3.zero,new Vector3(0,.105f,0),.022f,Materials["Metal"]);
            Primitive("Ball top",PrimitiveType.Sphere,stick,new Vector3(0,.115f,0),Vector3.one*.078f,paint,false);
            GameObject firstButton = null;
            for(int row=0;row<2;row++) for(int col=0;col<3;col++)
            {
                Vector3 p = new Vector3(.06f+col*.098f,.07f,-.09f+row*.10f);
                Primitive("Button retaining ring",PrimitiveType.Cylinder,controls,p,new Vector3(.075f,.012f,.075f),Materials["Rubber"],false);
                GameObject button = Primitive("Concave action button",PrimitiveType.Sphere,controls,p+Vector3.up*.018f,new Vector3(.057f,.025f,.057f),row==0?paint:Materials["White"],false);
                if (firstButton==null) firstButton=button;
            }
            DetailBox("Printed marquee face",root,new Vector3(0,1.965f,-.428f),new Vector3(.9f,.215f,.014f),marqueeMaterial,.01f);
            Cube("Marquee pinstripe",root,new Vector3(0,1.88f,-.44f),new Vector3(.83f,.009f,.004f),accent,false);
            CreateText(marquee,root,new Vector3(0,1.984f,-.442f),Quaternion.identity,.018f,new Color(.92f,.95f,.92f),64);
            DetailBox("Recessed coin door frame",root,new Vector3(0,.54f,-.427f),new Vector3(.43f,.55f,.028f),Materials["Rubber"],.018f);
            DetailBox("Coin door steel",root,new Vector3(0,.54f,-.445f),new Vector3(.385f,.50f,.016f),Materials["DarkMetal"],.013f);
            for(int slot=-1;slot<=1;slot+=2)
            {
                DetailBox("Coin mechanism plate",root,new Vector3(slot*.09f,.665f,-.46f),new Vector3(.11f,.15f,.015f),Materials["Metal"],.007f);
                Cube("Coin insertion slot",root,new Vector3(slot*.09f,.69f,-.47f),new Vector3(.064f,.009f,.006f),Materials["Black"],false);
                DetailBox("Coin return flap",root,new Vector3(slot*.09f,.39f,-.46f),new Vector3(.09f,.073f,.016f),Materials["Black"],.008f);
            }
            CreateText("25c  /  PLAY",root,new Vector3(0,.85f,-.432f),Quaternion.identity,.010f,new Color(.7f,.72f,.7f),48);
            for(int i=0;i<9;i++)
                Cube("Speaker grille slot",root,new Vector3((i-4)*.037f,1.80f,-.264f),new Vector3(.012f,.06f,.012f),Materials["Black"],false);
            Screw(root,new Vector3(-.41f,1.96f,-.447f)); Screw(root,new Vector3(.41f,1.96f,-.447f));
            Screw(root,new Vector3(-.16f,.74f,-.458f)); Screw(root,new Vector3(.16f,.34f,-.458f));
            AddBoxCollider(root.gameObject,new Vector3(0,1.05f,0),new Vector3(1.01f,2.10f,.97f));
            MachineAnimator animation = root.gameObject.AddComponent<MachineAnimator>();
            animation.joystick = stick; animation.pushButton = firstButton.transform; animation.screenText = label.transform;
            animation.glowColor = color; animation.speed = .7f;
            attract.cabinet = animation;
            return root.gameObject;
        }

        private static void BuildArchitecturalDetails()
        {
            Transform details = PartRoot("ARCHITECTURAL FINISHES - wall panels ceiling and services",world,Vector3.zero);
            foreach (Transform wall in world.GetComponentsInChildren<Transform>())
            {
                if (!wall.name.StartsWith("Main ") && !wall.name.StartsWith("Office ") && !wall.name.StartsWith("Prize ") &&
                    !wall.name.StartsWith("Storage ") && !wall.name.StartsWith("Power ")) continue;
                BoxCollider shape = wall.GetComponent<BoxCollider>();
                if (shape == null || wall.localScale.y < 3f) continue;
                Vector3 s = wall.localScale;
                bool horizontal = s.x > s.z;
                Vector3 p = wall.position;
                Vector3 panelSize = horizontal ? new Vector3(s.x, .85f, .255f) : new Vector3(.255f,.85f,s.z);
                DetailBox("Timber wainscot",details,new Vector3(p.x,.425f,p.z),panelSize,Materials["Wainscot"],.008f);
                DetailBox("Satin metal skirting",details,new Vector3(p.x,.075f,p.z),horizontal?new Vector3(s.x,.15f,.28f):new Vector3(.28f,.15f,s.z),Materials["DarkMetal"],.008f);
                DetailBox("Chair rail",details,new Vector3(p.x,.90f,p.z),horizontal?new Vector3(s.x,.045f,.29f):new Vector3(.29f,.045f,s.z),Materials["Brass"],.009f);
                float length = horizontal ? s.x : s.z;
                for(float offset=-length*.5f+.5f;offset<length*.5f-.15f;offset+=1.2f)
                {
                    Vector3 center = new Vector3(p.x,.44f,p.z) + (horizontal?Vector3.right:Vector3.forward)*offset;
                    DetailBox("Panel joint",details,center,horizontal?new Vector3(.018f,.74f,.261f):new Vector3(.261f,.74f,.018f),Materials["Rubber"],.003f);
                }
            }
            CeilingGrid(details,Vector3.zero,new Vector2(22,16));
            CeilingGrid(details,new Vector3(15,0,3),new Vector2(8,10));
            CeilingGrid(details,new Vector3(-15,0,3),new Vector2(8,10));
            CeilingGrid(details,new Vector3(-15,0,-7),new Vector2(8,10));
            CeilingGrid(details,new Vector3(0,0,-11),new Vector2(10,6));
            for(int side=-1;side<=1;side+=2)
            {
                for(int index=0;index<2;index++)
                {
                    Vector3 center=new Vector3(side*6.2f,2.55f,index==0?7.83f:-7.83f);
                    Transform vent=PartRoot("Wall ventilation grille",details,center,index==0?0:180);
                    DetailBox("Vent frame",vent,Vector3.zero,new Vector3(.75f,.28f,.08f),Materials["CeilingGrid"],.018f);
                    for(int i=0;i<6;i++) Cube("Angled vent louver",vent,new Vector3(0,-.1f+i*.04f,-.045f),new Vector3(.66f,.015f,.018f),Materials["Black"],false);
                }
            }
            DetailBox("Storage washable floor",details,new Vector3(-15,.003f,-7),new Vector3(7.75f,.006f,9.75f),Materials["ServiceFloor"],.002f);
            DetailBox("Power room service floor",details,new Vector3(0,.003f,-11),new Vector3(9.75f,.006f,5.75f),Materials["ServiceFloor"],.002f);
            for(int i=0;i<3;i++)
                Rod("Exposed overhead conduit",details,new Vector3(-4.65f+i*.12f,3.05f,-8.3f),new Vector3(-4.65f+i*.12f,3.05f,-13.6f),.035f,Materials["Metal"]);
        }

        private static void CeilingGrid(Transform parent, Vector3 center, Vector2 size)
        {
            int nx=Mathf.RoundToInt(size.x/2), nz=Mathf.RoundToInt(size.y/2);
            for(int x=0;x<nx;x++) for(int z=0;z<nz;z++)
            {
                Vector3 p=center+new Vector3(-size.x/2+1+x*2,3.30f,-size.y/2+1+z*2);
                DetailBox("Acoustic ceiling panel",parent,p,new Vector3(1.97f,.04f,1.97f),Materials["CeilingTile"],.008f);
            }
            for(int x=0;x<=nx;x++) DetailBox("Ceiling T grid",parent,center+new Vector3(-size.x/2+x*2,3.27f,0),new Vector3(.027f,.03f,size.y),Materials["CeilingGrid"],.003f);
            for(int z=0;z<=nz;z++) DetailBox("Ceiling T grid",parent,center+new Vector3(0,3.27f,-size.y/2+z*2),new Vector3(size.x,.03f,.027f),Materials["CeilingGrid"],.003f);
        }
    }
}
