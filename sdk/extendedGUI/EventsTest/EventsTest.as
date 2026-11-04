package
{
   import flash.display.DisplayObject;
   import flash.display.Sprite;
   import flash.events.Event;
   import flash.events.KeyboardEvent;
   import flash.events.MouseEvent;
   import flash.geom.Point;
   import logging.Logger;
   
   [SWF(frameRate="60",width="1024",height="768",backgroundColor="0xFFFFFF")]
   public class EventsTest extends Sprite
   {
      private var shortLog:Boolean = false;
      
      public function EventsTest()
      {
         super();
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
      }
      
      protected function onAddedToStage(event:Event) : void
      {
         this.removeEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         this.reset();
      }
      
      private function reset() : void
      {
         Base.stage = this.stage;
         Base.stage.align = Base.ALIGN;
         Base.stage.scaleMode = Base.SCALEMODE;
         Logger.init(this,true);
         Logger.regPoint = new Point(0,0);
         Logger.LogToChannel(Logger.DEBUG,"EventTest started.");
         this.initListeners();
      }
      
      private function addListener(dispatcher:DisplayObject, event:String, listener:Function) : void
      {
         try
         {
            dispatcher.addEventListener(event,listener);
            Logger.LogToChannel(Logger.DEBUG,"add listener for: \'",event,"\' to",dispatcher," OK");
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,"add listener for: \'",event,"\' to",dispatcher," ERROR",error);
         }
      }
      
      private function initListeners() : void
      {
         this.stage.doubleClickEnabled = true;
         this.stage.addEventListener(KeyboardEvent.KEY_UP,this.onKeyUp);
         this.stage.addEventListener(KeyboardEvent.KEY_DOWN,this.onKeyDown);
         this.addListener(stage,MouseEvent.DOUBLE_CLICK,this.onStageMouseEvent);
         this.addListener(stage,MouseEvent.CLICK,this.onStageMouseEvent);
         this.addListener(stage,MouseEvent.MOUSE_DOWN,this.onStageMouseEvent);
         this.addListener(stage,MouseEvent.MOUSE_UP,this.onStageMouseEvent);
         this.addListener(stage,MouseEvent.RIGHT_CLICK,this.onStageMouseEvent);
         this.addListener(stage,MouseEvent.RIGHT_MOUSE_DOWN,this.onStageMouseEvent);
         this.addListener(stage,MouseEvent.RIGHT_MOUSE_UP,this.onStageMouseEvent);
         this.addListener(stage,MouseEvent.MIDDLE_CLICK,this.onStageMouseEvent);
         this.addListener(stage,MouseEvent.MIDDLE_MOUSE_DOWN,this.onStageMouseEvent);
         this.addListener(stage,MouseEvent.MIDDLE_MOUSE_UP,this.onStageMouseEvent);
         this.addListener(stage,MouseEvent.CONTEXT_MENU,this.onStageMouseEvent);
         this.addListener(stage,MouseEvent.MOUSE_WHEEL,this.onStageMouseEvent);
      }
      
      protected function onKeyDown(event:KeyboardEvent) : void
      {
         if(this.shortLog)
         {
            Logger.LogToChannel(Logger.DEBUG,event.type.toUpperCase(),event.keyCode);
         }
         else
         {
            Logger.LogToChannel(Logger.DEBUG,event.type.toUpperCase(),event);
         }
      }
      
      protected function onKeyUp(event:KeyboardEvent) : void
      {
         if(event.keyCode == 27)
         {
            this.shortLog = !this.shortLog;
         }
         if(this.shortLog)
         {
            Logger.LogToChannel(Logger.DEBUG,event.type.toUpperCase(),event.keyCode);
         }
         else
         {
            Logger.LogToChannel(Logger.DEBUG,event.type.toUpperCase(),event);
         }
         if(event.keyCode == 32)
         {
            Logger.clear();
         }
      }
      
      protected function onStageMouseEvent(event:MouseEvent) : void
      {
         if(this.shortLog)
         {
            Logger.LogToChannel(Logger.DEBUG,event.type.toUpperCase());
         }
         else
         {
            Logger.LogToChannel(Logger.DEBUG,event.type.toUpperCase(),event);
         }
      }
   }
}

