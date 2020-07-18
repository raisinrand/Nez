using Nez.Textures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace Nez.UI
{
	public class MaterialDrawable : SpriteDrawable
	{
        public Material material;
        public MaterialDrawable(Sprite sprite, Material material) : base(sprite)
        {
            this.material = material;
        }

        public override void Draw(Batcher batcher, float x, float y, float width, float height, Color color)
		{
            Renderer.Active.SwapMaterial(Renderer.ActiveCam,material);
            base.Draw(batcher,x,y,width,height,color);
            Renderer.Active.SwapMaterial(Renderer.ActiveCam,null);
		}
	}
}