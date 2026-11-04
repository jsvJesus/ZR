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
      
      public function Keybind(arg1:Object = null)
      {
         super();
      }
      
      public function get allowEdit() : Boolean
      {
         if(String(this.action).indexOf("Weapon_long_") >= 0)
         {
            return false;
         }
         if(String(this.shortcuts[0]).indexOf("ENTER") >= 0)
         {
            return false;
         }
         return true;
      }
      
      public function get localeId() : String
      {
         return !!this.action ? Locale.LookInKeybindsTable(this.action) : null;
      }
      
      public function set defaultShortcuts(arg1:Array) : void
      {
         this._defaultShortcuts = arg1;
      }
      
      public function get defaultShortcuts() : Array
      {
         return this._defaultShortcuts;
      }
      
      public function get originalShortcuts() : Array
      {
         return this._originalShortcuts;
      }
      
      public function set originalShortcuts(arg1:Array) : void
      {
         this._originalShortcuts = arg1;
      }
      
      public function get shortcuts() : Array
      {
         return this._shortcuts;
      }
      
      public function set shortcuts(arg1:Array) : void
      {
         this._shortcuts = arg1;
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
      
      protected function onAnswer(arg1:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.GET_NEW_KEYBIND,this.onAnswer);
         Logger.LogToChannel(Logger.DEBUG,"onAnswer",arg1.data.answer.key_names);
         if(arg1.data.answer.key_names[0] != "ESCAPE" && arg1.data.answer.key_names[0] != "RETURN" && arg1.data.answer.key_names[0] != "ENTER")
         {
            this._shortcuts = arg1.data.answer.key_names;
            this.dispatchEvent(new Event(Event.CHANGE));
         }
         else
         {
            this.dispatchEvent(new Event(Event.CANCEL));
         }
      }
   }
}

