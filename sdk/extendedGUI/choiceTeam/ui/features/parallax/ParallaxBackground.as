package ui.features.parallax
{
   import com.greensock.TweenMax;
   import com.greensock.easing.*;
   import flash.display.Bitmap;
   import flash.display.BitmapData;
   import flash.display.Shape;
   import flash.display.Sprite;
   import flash.events.Event;
   import flash.events.MouseEvent;
   import flash.geom.Rectangle;
   
   public class ParallaxBackground extends Sprite
   {
      protected var originalWidth:uint = 1920;
      
      protected var originalHeight:uint = 1200;
      
      protected var pValue:Number = 0.1;
      
      private var layers:Vector.<ParallaxBitmapLayer>;
      
      protected var layersHolder:Sprite;
      
      protected var cover:Shape;
      
      protected var coverBitmap:BitmapData;
      
      protected var _isDirect:Boolean = false;
      
      protected var showScaleFinished:Boolean = false;
      
      public function ParallaxBackground()
      {
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         super();
         this.create();
      }
      
      public static function get USE_FILTERS() : Boolean
      {
         return StalkerBase.USE_FILTERS;
      }
      
      public function get isDirect() : Boolean
      {
         return this._isDirect;
      }
      
      public function set isDirect(value:Boolean) : void
      {
         this._isDirect = value;
      }
      
      protected function get direction() : Number
      {
         return this.isDirect ? -1 : 1;
      }
      
      private function create() : void
      {
         this.initLayers();
      }
      
      public function destroy() : void
      {
         this.removeEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         this.stage.removeEventListener(MouseEvent.MOUSE_MOVE,this.onMouseMove);
         this.stage.removeEventListener(Event.RESIZE,this.onStageResize);
      }
      
      private function initLayers() : void
      {
         this.layersHolder = new Sprite();
         this.addChild(this.layersHolder);
         this.layers = new Vector.<ParallaxBitmapLayer>();
         this.layers.push(this.addBitmapLayer(new Bitmap(new layer_4_png() as BitmapData),0.1));
         this.layers.push(this.addBitmapLayer(new Bitmap(new layer_3_png() as BitmapData),0.2));
         this.layers.push(this.addBitmapLayer(new Bitmap(new layer_2_png() as BitmapData),0.3));
         this.layers.push(this.addBitmapLayer(new Bitmap(new layer_1_png() as BitmapData),0.4));
         this.layers.push(this.addBitmapLayer(new Bitmap(new layer_0_png() as BitmapData),0.7));
         this.coverBitmap = new linegrid_png() as BitmapData;
         this.cover = new Shape();
         this.addChild(this.cover);
      }
      
      private function addBitmapLayer(bitmap:Bitmap, depth:Number) : ParallaxBitmapLayer
      {
         var layer:ParallaxBitmapLayer = new ParallaxBitmapLayer(bitmap,depth);
         this.layersHolder.addChild(layer);
         return layer;
      }
      
      protected function onAddedToStage(event:Event) : void
      {
         this.removeEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         this.stage.addEventListener(MouseEvent.MOUSE_MOVE,this.onMouseMove);
         this.stage.addEventListener(Event.RESIZE,this.onStageResize);
         this.doStageResize();
         this.show();
      }
      
      private function doRotate() : void
      {
      }
      
      private function show() : void
      {
         var layer:ParallaxBitmapLayer = null;
         var startScale:Number = NaN;
         this.doRotate();
         for each(layer in this.layers)
         {
            TweenMax.killTweensOf(layer);
            startScale = 1 + 1 * layer.depth;
            if(!USE_FILTERS)
            {
               TweenMax.fromTo(layer,2,{
                  "scaleX":startScale,
                  "scaleY":startScale,
                  "alpha":0
               },{
                  "scaleX":1,
                  "scaleY":1,
                  "delay":1,
                  "alpha":1,
                  "ease":Expo.easeInOut,
                  "onComplete":this.onCompleteShowScale
               });
            }
         }
      }
      
      protected function onCompleteShowScale() : void
      {
         this.showScaleFinished = true;
      }
      
      protected function onStageResize(event:Event) : void
      {
         this.doStageResize();
      }
      
      private function doStageResize() : void
      {
         this.layersHolder.scaleX = this.layersHolder.scaleY = Math.max(StalkerBase.stage.stageWidth / this.originalWidth,StalkerBase.stage.stageHeight / this.originalHeight) + this.pValue / 2;
         this.layersHolder.x = StalkerBase.stage.stageWidth / 2;
         this.layersHolder.y = StalkerBase.stage.stageHeight / 2;
         this.doParallax();
         this.scrollRect = new Rectangle(0,0,StalkerBase.stage.stageWidth,StalkerBase.stage.stageHeight);
      }
      
      private function drawCover() : void
      {
         if(Boolean(this.coverBitmap))
         {
            this.cover.graphics.clear();
            this.cover.graphics.beginBitmapFill(this.coverBitmap,null,true,false);
            this.cover.graphics.drawRect(0,0,StalkerBase.stage.stageWidth,StalkerBase.stage.stageHeight);
            this.cover.graphics.endFill();
         }
      }
      
      protected function onMouseMove(event:MouseEvent) : void
      {
         this.doParallax();
      }
      
      protected function doParallax() : void
      {
         var layer:ParallaxBitmapLayer = null;
         var xDiff:Number = StalkerBase.stage.mouseX / (StalkerBase.stage.stageWidth / 2) - 1;
         var yDiff:Number = StalkerBase.stage.mouseY / (StalkerBase.stage.stageHeight / 2) - 1;
         for each(layer in this.layers)
         {
            if(this.showScaleFinished)
            {
               TweenMax.killTweensOf(layer);
            }
            new TweenMax(layer,2,{
               "x":xDiff * layer.width * this.layersHolder.scaleX * this.pValue / 2 * layer.depth,
               "y":yDiff * layer.height * this.layersHolder.scaleY * this.pValue / 2 * layer.depth,
               "ease":Expo.easeOut
            });
         }
      }
   }
}

