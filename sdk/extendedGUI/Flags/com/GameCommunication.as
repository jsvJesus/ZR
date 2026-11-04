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
      
      public static function set_cursor(param1:String) : *
      {
         var _loc2_:Object = new Object();
         _loc2_.cursor_data = param1;
         ExternalInterface.call("set_cursor",_loc2_);
      }
      
      public static function goToFlag(param1:String) : *
      {
         ExternalInterface.call("go_to_flag",{"name":param1});
      }
      
      public static function buyFlag(param1:String) : *
      {
         ExternalInterface.call("buy_flag",{"name":param1});
      }
      
      public static function go_to_main_plan() : *
      {
         ExternalInterface.call("go_to_main_plan");
      }
      
      public static function come_back_to_earth() : *
      {
         ExternalInterface.call("come_back_to_earth");
      }
      
      public static function post_guardian(param1:int) : *
      {
         ExternalInterface.call("post_guardian",{"guard_type":param1});
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
      
      public function translate(param1:String, param2:String) : *
      {
         var _loc3_:Object = new Object();
         _loc3_.text = param1;
         _loc3_.to_lang = param2;
         ExternalInterface.call("translate",_loc3_);
      }
      
      public function saveSettings(param1:Object) : *
      {
         var _loc2_:Object = new Object();
         _loc2_.user_data = param1;
         ExternalInterface.call("save_user_settings",_loc2_);
      }
      
      public function updateSoundSettings(param1:Array) : *
      {
         var _loc2_:Object = new Object();
         _loc2_.data = param1;
         ExternalInterface.call("update_sounds_settings",_loc2_);
      }
      
      public function getColors() : *
      {
         ExternalInterface.call("get_colors");
      }
      
      public function sendMessage(param1:Number = 0, param2:String = "", param3:String = "") : *
      {
         var _loc4_:Object = null;
         if(param3 != "")
         {
            _loc4_ = new Object();
            _loc4_.channel_id = param1;
            _loc4_.receiver = this.STR.replaceEnter(param2);
            _loc4_.text = this.STR.replaceEnter(param3);
            ExternalInterface.call("send_msg",_loc4_);
         }
      }
      
      public function sendCommand(param1:String = "", param2:String = "") : *
      {
         var _loc3_:Object = null;
         if(param1 != "")
         {
            _loc3_ = new Object();
            _loc3_.text = this.STR.replaceEnter(param1) + " " + this.STR.replaceEnter(param2);
            ExternalInterface.call("run_command",_loc3_);
         }
      }
      
      public function report(param1:String = "") : *
      {
         var _loc2_:Object = null;
         if(param1 != "")
         {
            _loc2_ = new Object();
            _loc2_.user = param1;
            ExternalInterface.call("report",_loc2_);
         }
      }
      
      public function getLocal(param1:Array) : *
      {
         var _loc2_:Object = new Object();
         _loc2_.paths = param1;
         ExternalInterface.call("localized_resource",_loc2_);
      }
      
      public function getChannels() : *
      {
         ExternalInterface.call("channel_data");
      }
      
      public function changeFocus(param1:Boolean) : *
      {
         var _loc2_:Object = new Object();
         _loc2_.lock = param1;
         ExternalInterface.call("focus_lock",_loc2_);
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
      
      public function changeMode(param1:String) : *
      {
         var _loc2_:Object = new Object();
         _loc2_.mode = param1;
         ExternalInterface.call("mode_was_changed",_loc2_);
      }
      
      public function modalMode(param1:Boolean) : *
      {
         var _loc2_:Object = new Object();
         _loc2_.mode = param1;
         ExternalInterface.call("modal_mode",_loc2_);
      }
      
      public function initialization() : *
      {
         setTimeout(this.getLang,0);
         setTimeout(this.getCommands,0);
         setTimeout(this.getColors,0);
         setTimeout(this.getLocal,0,["Chat"]);
         setTimeout(this.getAccountStatus,0);
      }
      
      public function set_radio_mode(param1:Boolean) : *
      {
         var _loc2_:Object = new Object();
         _loc2_.value = param1;
         ExternalInterface.call("set_radio_mode",_loc2_);
      }
      
      public function setIMEmode(param1:Boolean) : *
      {
         var _loc2_:Object = new Object();
         _loc2_.value = param1;
         ExternalInterface.call("set_ime_mode",_loc2_);
      }
      
      public function pasteWarning() : *
      {
         ExternalInterface.call("on_paste_warning");
      }
   }
}

