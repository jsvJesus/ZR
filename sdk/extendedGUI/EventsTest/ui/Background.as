package ui
{
   import flash.display.BitmapData;
   import flash.display.Sprite;
   import flash.events.Event;
   import flash.geom.Matrix;
   
   public class Background extends Sprite
   {
      private var bd:BitmapData;
      
      public function Background()
      {
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         this.bd = new mainmenu_back() as BitmapData;
         super();
      }
      
      protected function onAddedToStage(event:Event) : void
      {
         this.removeEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         Base.stage.addEventListener(Base.STAGE_RESIZE,this.onResize);
         this.invalidate();
      }
      
      protected function onResize(event:Event) : void
      {
         this.invalidate();
      }
      
      protected function invalidate() : void
      {
         this.addEventListener(Event.ENTER_FRAME,this.onEnterFrame);
      }
      
      protected function onEnterFrame(event:Event) : void
      {
         this.removeEventListener(Event.ENTER_FRAME,this.onEnterFrame);
         this.draw();
      }
      
      protected function draw() : void
      {
         var ratio:Number = NaN;
         var matrix:Matrix = new Matrix();
         this.graphics.clear();
         if(Base.stage.stageWidth / Base.stage.stageHeight < this.bd.width / this.bd.height)
         {
            ratio = Base.stage.stageHeight / this.bd.height;
            matrix.scale(ratio,ratio);
            matrix.tx = (Base.stage.stageWidth - this.bd.width * ratio) / 2;
         }
         else
         {
            ratio = Base.stage.stageWidth / this.bd.width;
            matrix.scale(ratio,ratio);
            matrix.ty = (Base.stage.stageHeight - this.bd.height * ratio) / 2;
         }
         this.graphics.beginBitmapFill(this.bd,matrix,true,true);
         this.graphics.drawRect(0,0,Base.stage.stageWidth,Base.stage.stageHeight);
         this.graphics.endFill();
      }
   }
}

