using UnityEngine;

namespace ArcadeLockdown
{
    // Original animated attract displays, drawn into a real texture on the CRT.
    public sealed class ArcadeScreenAnimator : MonoBehaviour
    {
        public Color accent = Color.cyan;
        public int variant;
        public MachineAnimator cabinet;
        private Texture2D texture;
        private Color32[] pixels;
        private Material screen;
        private float nextFrame;
        const int Width = 128, Height = 96;

        private void Start()
        {
            screen = GetComponent<Renderer>().material;
            texture = new Texture2D(Width,Height,TextureFormat.RGBA32,false) { name="Original animated arcade display", filterMode=FilterMode.Point, wrapMode=TextureWrapMode.Clamp };
            pixels = new Color32[Width*Height];
            screen.mainTexture=texture;
            if(screen.HasProperty("_BaseMap")) screen.SetTexture("_BaseMap",texture);
            if(screen.HasProperty("_BaseColor")) screen.SetColor("_BaseColor",Color.white);
            if(screen.HasProperty("_Color")) screen.SetColor("_Color",Color.white);
            screen.EnableKeyword("_EMISSION");
            screen.SetTexture("_EmissionMap",texture);
            screen.SetColor("_EmissionColor",Color.white*.8f);
            Draw(0);
        }

        private void Update()
        {
            if(Time.time < nextFrame) return;
            nextFrame=Time.time+.10f;
            Draw(Time.time);
        }

        private void Draw(float t)
        {
            bool powered = cabinet == null || !cabinet.requiresPower || (cabinet.powerState != null && cabinet.powerState.IsPowered);
            for(int y=0;y<Height;y++) for(int x=0;x<Width;x++)
                pixels[y*Width+x] = powered ? new Color32((byte)(y%2==0?5:3), (byte)(y%2==0?13:8), (byte)(y%2==0?24:16),255) : new Color32(3,4,5,255);
            if(powered)
            {
                Color32 ink=accent;
                if(variant==0)
                {
                    for(int i=0;i<24;i++) Rect((i*43)%Width, 20+(int)((i*29+t*8)%72),1,1,new Color32(100,125,150,255));
                    int px=55+(int)(Mathf.Sin(t*.8f)*30);
                    Rect(px,27,14,3,ink); Rect(px+4,30,6,5,Color.white); Rect(px+6,35,2,5,ink);
                    for(int row=0;row<3;row++) for(int col=0;col<7;col++)
                    { int x=10+col*15+(int)(Mathf.Sin(t)*5); int y=61+row*10; Rect(x,y,8,5,ink); Rect(x+2,y+3,4,4,ink); Rect(x+2,y+1,1,1,Color.black); Rect(x+5,y+1,1,1,Color.black); }
                    Rect(px+6,43+(int)((t*30)%15),2,4,Color.white);
                }
                else if(variant==1)
                {
                    for(int y=20;y<90;y++) {int half=10+(90-y)/2; Rect(64-half,y,2,1,ink); Rect(64+half,y,2,1,ink); if(((y+(int)(t*25))/6)%2==0) Rect(63,y,2,1,Color.white);}
                    int x=58+(int)(Mathf.Sin(t)*12);
                    Rect(x,29,12,20,ink); Rect(x+2,40,8,5,Color.white); Rect(x-2,31,2,5,Color.gray); Rect(x+12,31,2,5,Color.gray);
                }
                else
                {
                    for(int row=0;row<5;row++) for(int col=0;col<9;col++)
                        Rect(11+col*12,58+row*6,10,4,Color.Lerp(accent,Color.white,row*.14f));
                    Rect(45+(int)(Mathf.Sin(t)*25),26,30,3,Color.white);
                    Rect(58+(int)(Mathf.Sin(t*1.7f)*40),35+(int)(Mathf.Abs(Mathf.Sin(t*1.4f))*20),3,3,ink);
                }
            }
            texture.SetPixels32(pixels); texture.Apply(false);
        }

        private void Rect(int x,int y,int width,int height,Color32 color)
        {
            for(int yy=Mathf.Max(0,y);yy<Mathf.Min(Height,y+height);yy++)
                for(int xx=Mathf.Max(0,x);xx<Mathf.Min(Width,x+width);xx++) pixels[yy*Width+xx]=color;
        }

        private void OnDestroy()
        {
            if(texture!=null) Destroy(texture);
            if(screen!=null) Destroy(screen);
        }
    }
}
