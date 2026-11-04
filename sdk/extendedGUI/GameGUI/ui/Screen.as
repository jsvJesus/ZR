package ui
{
   import flash.display.Sprite;
   import flash.events.Event;
   import flash.events.KeyboardEvent;
   import flash.ui.Keyboard;
   import logging.Logger;
   
   public class Screen extends Sprite
   {
      protected static const SWAP_VERTICAL:uint = 0;
      
      protected static const SWAP_HORIZONTAL:uint = 1;
      
      protected static const SWAP_DIAGONAL:uint = 2;
      
      protected static const SWAP_DIRECTION:uint = 1;
      
      protected static const SWAP_SIZE:uint = 50;
      
      public var widths:Array = [400];
      
      public var id:String;
      
      public var depth:uint;
      
      protected var inited:Boolean = false;
      
      protected var _enabled:Boolean = true;
      
      public function Screen(id:String, depth:uint = 0)
      {
         this.id = id;
         this.depth = depth;
         Logger.LogToChannel(Logger.DEBUG,"Screen construct:",this.id);
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         this.addEventListener(Event.REMOVED_FROM_STAGE,this.onRemovedFromStage);
         Base.stage.addEventListener(Event.RESIZE,this.onStageResize);
         super();
      }
      
      public function get fullWidth() : uint
      {
         return Base.stage.stageWidth - 50 - 50;
      }
      
      public function get fullHeight() : uint
      {
         return Base.stage.stageHeight - Navigator.HEADER_HEIGHT - Navigator.FOOTER_HEIGHT;
      }
      
      protected function init(... args) : void
      {
      }
      
      protected function freeze(... args) : void
      {
      }
      
      protected function unfreeze(... args) : void
      {
      }
      
      protected function initKeyboardShortcuts(... args) : void
      {
         Base.stage.addEventListener(KeyboardEvent.KEY_DOWN,this.onKeyDown);
      }
      
      protected function destroyKeyboardShortcuts(... args) : void
      {
         Base.stage.removeEventListener(KeyboardEvent.KEY_DOWN,this.onKeyDown);
      }
      
      protected function onKeyDown(event:KeyboardEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,this.id,event.keyCode);
         switch(event.keyCode)
         {
            case Keyboard.ESCAPE:
            case Keyboard.BACKSPACE:
               Logger.LogToChannel(Logger.DEBUG,"Screen event throw: goBack",this.id);
               this.goBack();
         }
      }
      
      protected function show(... args) : void
      {
      }
      
      protected function hide(... args) : void
      {
      }
      
      protected function resize(... args) : void
      {
      }
      
      public function destroy() : void
      {
         this.removeEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         this.removeEventListener(Event.REMOVED_FROM_STAGE,this.onRemovedFromStage);
      }
      
      protected function onAddedToStage(event:Event) : void
      {
         if(!this.inited)
         {
            this.init();
            this.inited = true;
         }
         this.unfreeze();
      }
      
      protected function onStageResize(event:Event) : void
      {
         this.resize();
      }
      
      protected function onRemovedFromStage(event:Event) : void
      {
         this.freeze();
      }
      
      public function get enabled() : Boolean
      {
         return this._enabled;
      }
      
      public function set enabled(value:Boolean) : void
      {
         this._enabled = value;
      }
      
      public function goBack() : void
      {
      }
   }
}

