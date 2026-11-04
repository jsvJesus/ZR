package ui
{
   import flash.events.Event;
   import flash.events.MouseEvent;
   import flash.geom.Rectangle;
   import logging.Logger;
   
   public class UIScreen extends UIControl implements IScreen
   {
      public static const VERSION:String = "1.0.0";
      
      public var id:String;
      
      private var _owner:ScreenNavigator;
      
      public var horizontalFit:Number = 0;
      
      public var verticalFit:Number = 0;
      
      public function UIScreen(id:String = null)
      {
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         this.id = id != null ? id : this.id;
         this.preinit();
         Logger.LogToChannel(Logger.DEBUG,"Construct screen: [",id,"]; version:",VERSION);
         super();
      }
      
      public function get screenID() : String
      {
         return this.id;
      }
      
      public function set screenID(value:String) : void
      {
         this.id = value;
      }
      
      public function get owner() : ScreenNavigator
      {
         return this._owner;
      }
      
      public function set owner(value:ScreenNavigator) : void
      {
         this._owner = value;
      }
      
      public function get fit() : Rectangle
      {
         var fitRect:Rectangle = new Rectangle();
         if(this.horizontalFit <= 1)
         {
            fitRect.width = this.width * this.horizontalFit;
         }
         else
         {
            fitRect.width = this.width - this.horizontalFit;
         }
         if(this.verticalFit <= 1)
         {
            fitRect.height = this.height * this.verticalFit;
         }
         else
         {
            fitRect.height = this.height - this.verticalFit;
         }
         fitRect.x = (this.width - fitRect.width) / 2;
         fitRect.y = (this.height - fitRect.height) / 2;
         return fitRect;
      }
      
      protected function onAddedToStage(event:Event) : void
      {
         this.removeEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         this.addEventListener(Event.REMOVED_FROM_STAGE,this.onRemovedFromStage);
         this.stage.addEventListener(MouseEvent.MOUSE_MOVE,this.onMouseMove,false,0,true);
         this.init();
         this.draw();
      }
      
      protected function onRemovedFromStage(event:Event) : void
      {
         this.stage.removeEventListener(MouseEvent.MOUSE_MOVE,this.onMouseMove);
      }
      
      protected function onMouseMove(event:MouseEvent) : void
      {
      }
      
      protected function init() : void
      {
         trace(this,"init");
      }
      
      protected function preinit() : void
      {
         trace(this,"preinit");
      }
      
      override protected function draw() : void
      {
         trace(this,"draw");
      }
   }
}

