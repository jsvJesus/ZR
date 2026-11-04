package com
{
   import flash.external.ExternalInterface;
   import flash.utils.setTimeout;
   
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
      
      public static function goToFlag(id:String) : *
      {
         ExternalInterface.call("go_to_flag",{"name":id});
      }
      
      public static function buyFlag(id:String) : *
      {
         ExternalInterface.call("buy_flag",{"name":id});
      }
      
      public static function go_to_main_plan() : *
      {
         ExternalInterface.call("go_to_main_plan");
      }
      
      public static function come_back_to_earth() : *
      {
         ExternalInterface.call("come_back_to_earth");
      }
      
      public static function post_guardian(guard_type:int) : *
      {
         ExternalInterface.call("post_guardian",{"guard_type":guard_type});
      }
      
      public static function stop_guardians_buying() : *
      {
         ExternalInterface.call("stop_guardians_buying");
      }
      
      public static function remove_guardians() : *
      {
         ExternalInterface.call("remove_guardians");
      }
      
      public function getLang() : *
      {
         ExternalInterface.call("get_locale",{});
      }
      
      public function getCommands() : *
      {
         ExternalInterface.call("get_commands");
      }
      
      public function defaultSettings() : *
      {
         ExternalInterface.call("default_settings");
      }
      
      public function sound_settings() : *
      {
         ExternalInterface.call("sound_settings");
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
      
      public function updateSoundSettings(obj:Array) : *
      {
         var Z:Object = new Object();
         Z.data = obj;
         ExternalInterface.call("update_sounds_settings",Z);
      }
      
      public function getColors() : *
      {
         ExternalInterface.call("get_colors");
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
      
      public function report(usr:String = "") : *
      {
         var Obj:Object = null;
         if(usr != "")
         {
            Obj = new Object();
            Obj.user = usr;
            ExternalInterface.call("report",Obj);
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
      
      public function initialization() : *
      {
         setTimeout(this.getLang,0);
         setTimeout(this.getCommands,0);
         setTimeout(this.getColors,0);
         setTimeout(this.getLocal,0,["Chat"]);
         setTimeout(this.getAccountStatus,0);
      }
      
      public function set_radio_mode(value:Boolean) : *
      {
         var Obj:Object = new Object();
         Obj.value = value;
         ExternalInterface.call("set_radio_mode",Obj);
      }
      
      public function setIMEmode(value:Boolean) : *
      {
         var Obj:Object = new Object();
         Obj.value = value;
         ExternalInterface.call("set_ime_mode",Obj);
      }
      
      public function pasteWarning() : *
      {
         ExternalInterface.call("on_paste_warning");
      }
   }
}

