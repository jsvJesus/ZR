package ui
{
   import com.dvalimona.components.Label;
   import com.dvalimona.components.LabelShadowed;
   import communication.Api;
   import events.ApiEvent;
   import flash.display.BitmapData;
   import flash.display.Shape;
   import flash.display.Sprite;
   import flash.events.Event;
   import flash.utils.setTimeout;
   import logging.Logger;
   
   public class Footer extends Sprite
   {
      private var background:Shape = new Shape();
      
      private var backgroundBitmap:BitmapData = new noised_half_black_png() as BitmapData;
      
      private var version:LabelShadowed;
      
      public function Footer()
      {
         this.addChild(this.background);
         Api.self.addEventListener(Api.GET_CLIENT_VERSION,this.onGetClientVersionHandler);
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         super();
      }
      
      protected function onAddedToStage(event:Event) : void
      {
         this.removeEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         this.addEventListener(Event.REMOVED_FROM_STAGE,this.onRemovedFromStage);
         Base.stage.addEventListener(Base.STAGE_RESIZE,this.onResize);
         this.version = new LabelShadowed(this);
         this.version.align = Label.RIGHT;
         this.version.width = 300;
         this.version.autoSize = false;
         this.version.debug = false;
         this.version.size = 16;
         this.version.color = 8947848;
         this.version.text = "LOADING..";
         this.onResize();
         setTimeout(this.onResize,50);
         Api.call(Api.GET_CLIENT_VERSION);
      }
      
      protected function onGetClientVersionHandler(event:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.WARNING,"onGetClientVersionHandler",event.data.answer.result);
         this.version.text = event.data.answer.result + "";
         this.draw();
      }
      
      protected function onResize(event:Event = null) : void
      {
         this.draw();
      }
      
      protected function draw() : void
      {
         this.drawBackground();
         this.version.x = Base.stage.stageWidth - this.version.width - 10;
         this.version.y = (Navigator.FOOTER_HEIGHT - this.version.height) / 2 + 2;
      }
      
      private function drawBackground() : void
      {
         this.background.graphics.clear();
         this.background.graphics.beginFill(0,0);
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

