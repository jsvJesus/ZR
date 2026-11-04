package ui
{
   import flash.display.BitmapData;
   import flash.display.Sprite;
   import flash.events.Event;
   
   public class Background extends Sprite
   {
      private var linegrid:BitmapData;
      
      public function Background()
      {
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         this.linegrid = new linegrid_png() as BitmapData;
         super();
      }
      
      protected function onAddedToStage(event:Event) : void
      {
         this.removeEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         Base.stage.addEventListener(Base.STAGE_RESIZE,this.onResize);
         this.draw();
      }
      
      protected function onResize(event:Event) : void
      {
         this.draw();
      }
      
      protected function draw() : void
      {
         this.graphics.clear();
         this.graphics.beginBitmapFill(this.linegrid,null,true,false);
         this.graphics.drawRect(0,0,Base.stage.stageWidth,Base.stage.stageHeight);
         this.graphics.endFill();
      }
   }
}

