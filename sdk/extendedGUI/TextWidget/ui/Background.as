package ui
{
   import flash.display.*;
   import flash.events.*;
   import flash.geom.*;
   
   public class Background extends Sprite
   {
      private var bd:BitmapData;
      
      public function Background()
      {
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         this.bd = new mainmenu_back() as BitmapData;
         super();
      }
      
      protected function onAddedToStage(param1:Event) : void
      {
         this.removeEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         Base.stage.addEventListener(Base.STAGE_RESIZE,this.onResize);
         this.invalidate();
      }
      
      protected function onResize(param1:Event) : void
      {
         this.invalidate();
      }
      
      protected function invalidate() : void
      {
         this.addEventListener(Event.ENTER_FRAME,this.onEnterFrame);
      }
      
      protected function onEnterFrame(param1:Event) : void
      {
         this.removeEventListener(Event.ENTER_FRAME,this.onEnterFrame);
         this.draw();
      }
      
      protected function draw() : void
      {
         var _loc2_:Number = NaN;
         var _loc1_:Matrix = new Matrix();
         this.graphics.clear();
         this.graphics.lineStyle(1,16777215,0.5);
         this.graphics.moveTo(0,0);
         this.graphics.lineTo(Base.stage.stageWidth,0);
         this.graphics.lineTo(Base.stage.stageWidth,Base.stage.stageHeight);
         this.graphics.lineTo(0,Base.stage.stageHeight);
         this.graphics.lineTo(0,0);
         this.graphics.lineTo(Base.stage.stageWidth,Base.stage.stageHeight);
         this.graphics.moveTo(0,Base.stage.stageHeight);
         this.graphics.lineTo(Base.stage.stageWidth,0);
      }
   }
}

