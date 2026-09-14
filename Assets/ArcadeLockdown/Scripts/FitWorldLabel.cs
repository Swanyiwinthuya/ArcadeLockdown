using UnityEngine;
namespace ArcadeLockdown
{
    public sealed class FitWorldLabel : MonoBehaviour
    {
        public Vector2 maximum;
        private int frames;
        private void LateUpdate()
        {
            TextMesh text=GetComponent<TextMesh>();Renderer renderer=GetComponent<Renderer>();
            Bounds bounds=renderer.localBounds;
            if(bounds.size.x>.0001f&&bounds.size.y>.0001f)
            {
                float factor=Mathf.Min(1,Mathf.Min(maximum.x/bounds.size.x,maximum.y/bounds.size.y));
                if(factor<.99f)text.characterSize*=factor;
            }
            if(++frames>5)enabled=false;
        }
    }
}
