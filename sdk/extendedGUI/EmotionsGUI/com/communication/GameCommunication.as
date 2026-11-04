package com.communication
{
   import com.StringParse;
   import flash.external.ExternalInterface;
   
   public class GameCommunication
   {
      internal var STR:StringParse = new StringParse();
      
      public function GameCommunication()
      {
         super();
      }
      
      public static function set_cursor(name:String) : *
      {
         var Obj:Object = new Object();
         Obj.cursor_data = name;
         ExternalInterface.call("set_cursor",Obj);
      }
      
      public static function play_anim(id:int) : *
      {
         var Obj:Object = new Object();
         Obj.anim_id = id;
         ExternalInterface.call("play_anim",Obj);
      }
      
      public static function change_anim_key(id:int) : *
      {
         var Obj:Object = new Object();
         Obj.anim_id = id;
         ExternalInterface.call("change_anim_key",Obj);
      }
      
      public static function check_anim(id:int, status:Boolean) : *
      {
         var Obj:Object = new Object();
         Obj.anim_id = id;
         Obj.value = status;
         ExternalInterface.call("check_anim",Obj);
      }
      
      public static function get_settings() : *
      {
         ExternalInterface.call("get_settings");
      }
      
      public static function set_settings() : *
      {
         ExternalInterface.call("set_settings");
      }
      
      public static function ready() : *
      {
         ExternalInterface.call("ready",{});
      }
      
      public static function hide_interface() : *
      {
         ExternalInterface.call("hide_interface");
      }
      
      public static function python_trace(text:String) : *
      {
         var Obj:Object = new Object();
         Obj.text = text;
         ExternalInterface.call("python_trace",Obj);
      }
      
      public static function save_position(x:int, y:int) : *
      {
         var Obj:Object = new Object();
         Obj.x = x;
         Obj.y = y;
         ExternalInterface.call("save_position",Obj);
      }
      
      public function getLang() : *
      {
         ExternalInterface.call("get_locale",{});
      }
      
      public function loadSettings() : *
      {
         ExternalInterface.call("read_user_settings");
      }
      
      public function translate(text:String, lang:String) : *
      {
         var Obj:Object = new Object();
         Obj.text = text;
         Obj.to_lang = lang;
         ExternalInterface.call("translate",Obj);
      }
      
      public function saveSettings(Obj:Object) : *
      {
         var Z:Object = new Object();
         Z.user_data = Obj;
         ExternalInterface.call("save_user_settings",Z);
      }
      
      public function sendMessage(id:Number = 0, usr:String = "", msg:String = "") : *
      {
         var Obj:Object = null;
         if(msg != "")
         {
            Obj = new Object();
            Obj.channel_id = id;
            Obj.receiver = this.STR.replaceEnter(usr);
            Obj.text = this.STR.replaceEnter(msg);
            ExternalInterface.call("send_msg",Obj);
         }
      }
      
      public function sendCommand(command:String = "", args:String = "") : *
      {
         var Obj:Object = null;
         if(command != "")
         {
            Obj = new Object();
            Obj.text = this.STR.replaceEnter(command) + " " + this.STR.replaceEnter(args);
            ExternalInterface.call("run_command",Obj);
         }
      }
      
      public function getLocal(arr:Array) : *
      {
         var Obj:Object = new Object();
         Obj.paths = arr;
         ExternalInterface.call("localized_resource",Obj);
      }
      
      public function getChannels() : *
      {
         ExternalInterface.call("channel_data");
      }
      
      public function changeFocus(lock:Boolean) : *
      {
         var Obj:Object = new Object();
         Obj.lock = lock;
         ExternalInterface.call("focus_lock",Obj);
      }
      
      public function ready() : *
      {
         ExternalInterface.call("ready",{});
      }
      
      public function getAccountStatus() : *
      {
         ExternalInterface.call("access_level");
      }
      
      public function cursorMode() : *
      {
         ExternalInterface.call("cursor_mode");
      }
      
      public function changeMode(mode:String) : *
      {
         var Obj:Object = new Object();
         Obj.mode = mode;
         ExternalInterface.call("mode_was_changed",Obj);
      }
      
      public function modalMode(mode:Boolean) : *
      {
         var Obj:Object = new Object();
         Obj.mode = mode;
         ExternalInterface.call("modal_mode",Obj);
      }
   }
}

