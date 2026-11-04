package ui.features.parallax
{
   import flash.display.Bitmap;
   import flash.display.Sprite;
   
   public class ParallaxBitmapLayer extends Sprite
   {
      public var bitmap:Bitmap;
      
      public var depth:Number;
      
      public function ParallaxBitmapLayer(bitmap:Bitmap, depth:Number)
      {
         super();
         if(bitmap != null)
         {
            this.depth = depth;
            this.bitmap = bitmap;
            this.bitmap.smoothing = true;
            this.bitmap.x = -this.bitmap.width / 2;
            this.bitmap.y = -this.bitmap.height / 2;
            this.addChild(this.bitmap);
         }
      }
   }
}

