package ui.components
{
   import com.greensock.*;
   import com.greensock.easing.*;
   import communication.*;
   import flash.display.*;
   import flash.events.*;
   
   public class Shield extends Sprite
   {
      private var holder:Shape;
      
      private var backgroundBitmap:BitmapData;
      
      private var needDraw:Boolean = false;
      
      private var hiding:Boolean = false;
      
      private var prealpha:Number = 0.5;
      
      public function Shield()
      {
         super();
         this.visible = false;
         this.hiding = false;
         this.holder = new Shape();
      }
      
      protected function onResize(event:Event) : void
      {
         this.doResize();
      }
      
      private function doResize() : void
      {
         this.width = Base.stage.stageWidth;
         this.height = Base.stage.stageHeight;
      }
      
      public function show(blackAlpha:Number = 0.5) : void
      {
         this.prealpha = blackAlpha;
         this.holder.alpha = 0;
         TweenMax.to(this.holder,1,{
            "alpha":1,
            "ease":Expo.easeOut,
            "onComplete":null
         });
         this.needDraw = true;
         this.addEventListener(Event.ENTER_FRAME,this.onEnterFrame);
         this.addChild(this.holder);
         this.hiding = false;
         this.visible = true;
         Api.call("on_show_guadrscreen");
      }
      
      protected function onEnterFrame(event:Event) : void
      {
         if(this.needDraw)
         {
            this.draw();
         }
      }
      
      public function hide() : void
      {
         if(this.hiding)
         {
            return;
         }
         if(!this.visible)
         {
            return;
         }
         this.hiding = true;
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
         if(!this.hiding)
         {
            return;
         }
         this.needDraw = false;
         this.hiding = false;
         this.removeEventListener(Event.ENTER_FRAME,this.onEnterFrame);
         this.visible = false;
         this.removeChild(this.holder);
         Api.call("on_hide_guadrscreen");
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

