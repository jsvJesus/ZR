package ui
{
   import flash.display.BitmapData;
   import flash.display.Shape;
   import flash.display.Sprite;
   import flash.events.Event;
   
   public class Footer extends Sprite
   {
      private var background:Shape = new Shape();
      
      private var backgroundBitmap:BitmapData = new noised_half_black_png() as BitmapData;
      
      public function Footer()
      {
         this.addChild(this.background);
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         super();
      }
      
      protected function onAddedToStage(event:Event) : void
      {
         this.removeEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         this.addEventListener(Event.REMOVED_FROM_STAGE,this.onRemovedFromStage);
         Base.stage.addEventListener(Event.RESIZE,this.onResize);
         this.onResize();
      }
      
      protected function onResize(event:Event = null) : void
      {
         this.draw();
      }
      
      protected function draw() : void
      {
         this.drawBackground();
      }
      
      private function drawBackground() : void
      {
         this.background.graphics.clear();
         this.background.graphics.beginBitmapFill(this.backgroundBitmap,null,true,false);
         this.background.graphics.drawRect(0,0,Base.stage.stageWidth,Navigator.FOOTER_HEIGHT);
         this.background.graphics.endFill();
      }
      
      protected function onRemovedFromStage(event:Event) : void
      {
         this.removeEventListener(Event.REMOVED_FROM_STAGE,this.onRemovedFromStage);
         Base.stage.removeEventListener(Event.RESIZE,this.onResize);
      }
   }
}

