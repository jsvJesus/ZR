package ui.components
{
   import com.greensock.*;
   import com.greensock.easing.*;
   import flash.display.*;
   import flash.events.*;
   
   public class Shield extends Sprite
   {
      private var holder:Shape;
      
      private var backgroundBitmap:BitmapData;
      
      private var needDraw:Boolean = false;
      
      private var prealpha:Number = 0.5;
      
      public function Shield()
      {
         super();
         this.holder = new Shape();
      }
      
      protected function onResize(param1:Event) : void
      {
         this.doResize();
      }
      
      private function doResize() : void
      {
         this.width = Base.stage.stageWidth;
         this.height = Base.stage.stageHeight;
      }
      
      public function show(param1:Number = 0.5) : void
      {
         this.prealpha = param1;
         this.holder.alpha = 0;
         TweenMax.to(this.holder,1,{
            "alpha":1,
            "ease":Expo.easeOut,
            "onComplete":null
         });
         this.needDraw = true;
         this.addEventListener(Event.ENTER_FRAME,this.onEnterFrame);
         this.addChild(this.holder);
         this.visible = true;
      }
      
      protected function onEnterFrame(param1:Event) : void
      {
         if(this.needDraw)
         {
            this.draw();
         }
      }
      
      public function hide() : void
      {
         if(Base.navigator.dialogs.numChildren <= 1)
         {
            TweenMax.to(this.holder,0.5,{
               "alpha":0,
               "ease":Expo.easeOut,
               "onComplete":this.hideComplete
            });
         }
      }
      
      private function hideComplete() : void
      {
         this.needDraw = false;
         this.removeEventListener(Event.ENTER_FRAME,this.onEnterFrame);
         this.visible = false;
         this.removeChild(this.holder);
      }
      
      private function draw() : void
      {
         this.holder.graphics.clear();
         this.holder.graphics.beginFill(1118481,this.prealpha);
         this.holder.graphics.drawRect(0,0,Base.stage.stageWidth,Base.stage.stageHeight);
         this.holder.graphics.endFill();
      }
   }
}

