package communication
{
   import events.*;
   import flash.events.*;
   import lang.*;
   import logging.*;
   
   public class Keybind extends EventDispatcher
   {
      public var action:String;
      
      private var _defaultShortcuts:Array;
      
      private var _originalShortcuts:Array;
      
      private var _shortcuts:Array;
      
      public function Keybind(param1:Object = null)
      {
         super();
      }
      
      public function get allowEdit() : Boolean
      {
         var _loc1_:* = true;
         if(String(this.shortcuts[0]).indexOf("ALT ") >= 0)
         {
            _loc1_ = false;
         }
         if(String(this.shortcuts[0]).indexOf("SHIFT ") >= 0)
         {
            _loc1_ = false;
         }
         return _loc1_;
      }
      
      public function get localeId() : String
      {
         return !!this.action ? Locale.LookInKeybindsTable(this.action) : null;
      }
      
      public function set defaultShortcuts(param1:Array) : void
      {
         this._defaultShortcuts = param1;
      }
      
      public function get defaultShortcuts() : Array
      {
         return this._defaultShortcuts;
      }
      
      public function get originalShortcuts() : Array
      {
         return this._originalShortcuts;
      }
      
      public function set originalShortcuts(param1:Array) : void
      {
         this._originalShortcuts = param1;
      }
      
      public function get shortcuts() : Array
      {
         return this._shortcuts;
      }
      
      public function set shortcuts(param1:Array) : void
      {
         this._shortcuts = param1;
      }
      
      public function get defaulted() : Boolean
      {
         return this._shortcuts.join("-") == this.defaultShortcuts.join("-");
      }
      
      public function get changed() : Boolean
      {
         return this._shortcuts.join("-") != this.originalShortcuts.join("-");
      }
      
      public function resetToDefault() : void
      {
         this._originalShortcuts = this.defaultShortcuts;
         this._shortcuts = this.defaultShortcuts;
      }
      
      public function getNewBind() : void
      {
         Api.self.addEventListener(Api.GET_NEW_KEYBIND,this.onAnswer);
         Api.call(Api.GET_NEW_KEYBIND);
      }
      
      protected function onAnswer(param1:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.GET_NEW_KEYBIND,this.onAnswer);
         Logger.LogToChannel(Logger.DEBUG,"onAnswer",param1.data.answer.key_names);
         if(param1.data.answer.key_names[0] != "ESCAPE")
         {
            this._shortcuts = param1.data.answer.key_names;
            this.dispatchEvent(new Event(Event.CHANGE));
         }
         else
         {
            this.dispatchEvent(new Event(Event.CANCEL));
         }
      }
   }
}

