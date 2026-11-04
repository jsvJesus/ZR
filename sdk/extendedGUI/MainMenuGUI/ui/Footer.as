package ui
{
   import com.dvalimona.components.*;
   import communication.*;
   import events.ApiEvent;
   import flash.display.*;
   import flash.events.*;
   import flash.utils.*;
   import logging.*;
   
   public class Footer extends Sprite
   {
      private var background:Shape;
      
      private var version:LabelShadowed;
      
      private var server:LabelShadowed;
      
      public function Footer()
      {
         this.background = new Shape();
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
         this.server = new LabelShadowed(this);
         this.server.align = Label.RIGHT;
         this.server.width = 300;
         this.server.autoSize = false;
         this.server.debug = false;
         this.server.size = 16;
         this.server.color = 8947848;
         this.server.text = " ";
         this.onResize();
         setTimeout(this.onResize,1000);
         Api.call(Api.GET_CLIENT_VERSION);
      }
      
      public function set serverName(value:String) : *
      {
         this.server.text = value;
         setTimeout(this.onResize,1000);
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
         this.server.y = this.version.y;
         this.server.width = this.server.textField.textWidth + 5;
         this.server.x = this.version.x + 300 - (this.version.textField.textWidth + 12) - this.server.width;
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

