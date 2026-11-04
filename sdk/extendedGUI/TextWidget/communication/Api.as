package communication
{
   import events.*;
   import flash.events.*;
   import flash.external.*;
   import flash.utils.*;
   import logging.*;
   
   public class Api extends EventDispatcher
   {
      private static var _self:Api;
      
      public static const OPEN_SETTINGS:String = "open_settings";
      
      public static const SHOW_TEXT:String = "showText";
      
      public static const GET_EULA_ACCEPTED:String = "EULAFlash.getEulaAccepted";
      
      public static const SET_EULA_ACCEPTED:String = "EULAFlash.setEulaAccepted";
      
      public static const SHOW_DUMMY:String = "show_dummy";
      
      public static const HIDE_DUMMY:String = "hide_dummy";
      
      public static const RESIZE_DUMMY:String = "DummyWindow.resize_component";
      
      public static const DUMMY_SIZE_AND_POSITION:String = "DummyWindow.component_size_and_position";
      
      public static const DUMMY_MOVE_X:String = "DummyWindow.set_avatar_Xposition";
      
      public static const DUMMY_MOVE_Y:String = "DummyWindow.set_avatar_Yposition";
      
      public static const GET_CLIENT_VERSION:String = "getClientVersion";
      
      public static const IS_IN_GAME:String = "isInGame";
      
      public static const GET_SETTINGS:String = "getSettings";
      
      public static const GET_SETTINGS_RANGE:String = "getSettingsRange";
      
      public static const GET_CURRENT_LOCALE:String = "getCurrentLocale";
      
      public static const SET_CURRENT_LOCALE:String = "setCurrentLocale";
      
      public static const GET_LOCALES_LIST:String = "getLocalesList";
      
      public static const GET_LOCALIZED_RESOURCE:String = "getLocalizedResource";
      
      public static const GET_KEYBIND_LOC_TABLE:String = "getKeybindLocalizeTable";
      
      public static const GET_SERVER_LIST:String = "ActionsWithLogin.getServerList";
      
      public static const GET_LOGIN:String = "ActionsWithLogin.getLogin";
      
      public static const SET_SETTINGS:String = "setSettings";
      
      public static const GET_DEFAULT_SETTINGS:String = "getDefaultOption";
      
      public static const SET_DEFAULT_KEY_BINDINGS:String = "setDefaultKeyBindings";
      
      public static const DO_LOG_OUT:String = "ActionsWithLogin.doLogOut";
      
      public static const QUIT_GAME:String = "quitGame";
      
      public static const RESTART_GAME:String = "restartGame";
      
      public static const DO_RESTART_GAME:String = "doRestartGame";
      
      public static const SHOW_DEVELOPER_SERVERS:String = "ActionsWithLogin.showDeveloperServer";
      
      public static const AUTHENTICATE_USER:String = "ActionsWithLogin.authenticateUser";
      
      public static const ACTIVATE_ACCOUNT_WINDOW:String = "activateAccountWindow";
      
      public static const ALL_CHARACTERS_INFO:String = "allCharactersInfo";
      
      public static const GET_PREMIUM_ACCOUNT_DATA:String = "getPremiumAccountData";
      
      public static const PREMIUM_ACCOUNT_DATA:String = "premiumAccountData";
      
      public static const SELECT_CHAR:String = "selectChar";
      
      public static const DELETE_CHAR:String = "deleteCharacter";
      
      public static const RESTORE_CHAR:String = "restoreCharacter";
      
      public static const CREATING_CHAR:String = "creatingChar";
      
      public static const NEW_CHAR_VIEW:String = "newCharView";
      
      public static const CHECK_AVATAR_NAME:String = "checkAvatarName";
      
      public static const CREATE_CHAR:String = "createChar";
      
      public static const CANCEL_CREATE_CHAR:String = "cancelCreateChar";
      
      public static const GO_TO_GAME:String = "goToGame";
      
      public static const GET_NEW_KEYBIND:String = "getNewKeybind";
      
      public static const OPEN_URL:String = "openURL";
      
      public static const UPDATE_PREMIUM:String = "updatePremium";
      
      public static const UPDATE_GOLD:String = "updateGold";
      
      public static const SHOW_ME_NEWS_EVERYTIME:String = "showMeNewsEverytime";
      
      public static const INCOMING_MESSAGE:String = "incoming_message";
      
      public static const SEND_MESSAGE:String = "send_message";
      
      public static const CTRLV:String = "controlV";
      
      public static const CONFIRM_WINDOW_HIDE:String = "ConfirmWindow.hide";
      
      public static const CONFIRM_WINDOW_OK:String = "ConfirmWindow.click_ok";
      
      public static const CONFIRM_WINDOW_RESEND:String = "ConfirmWindow.click_resend";
      
      public static const CHOOSE_DIRECTORY:String = "choose_directory";
      
      public static const CHOOSE_FILE:String = "choose_file";
      
      public static const CHANGE_ROOT:String = "change_root";
      
      public static const UP_LEVEL:String = "up_directory";
      
      public static const PUSH_CANCEL:String = "push_cancel";
      
      public static const CONFIRM_WINDOW_SHOW:String = "ConfirmWindow.show";
      
      public static const FILESYSTEM_ROOTS:String = "filesystem_roots";
      
      public static const READY:String = "ready";
      
      public static const SCREEN_SIZE:String = "screenSize";
      
      public static const SCREEN_MODE:String = "screenMode";
      
      public static const ALERT:String = "alert";
      
      public static const SHOW_NEWS:String = "showNews";
      
      public static const FILELIST:String = "file_list";
      
      private static var idCount:uint = 0;
      
      idCount = 0;
      
      public function Api(param1:IEventDispatcher = null)
      {
         super(param1);
      }
      
      private static function get ResponseId() : String
      {
         var _loc2_:* = undefined;
         var _loc1_:* = "R" + idCount;
         ++idCount;
         return _loc1_;
      }
      
      public static function get self() : Api
      {
         if(!_self)
         {
            _self = new Api();
         }
         return _self;
      }
      
      public static function call(param1:String, param2:Array = null, param3:Function = null) : void
      {
         var name:String = null;
         var json:Object = null;
         var loc1:* = undefined;
         var arg1:String = param1;
         var arg2:Array = param2;
         var arg3:Function = param3;
         var args:Array = null;
         var handler:Function = null;
         json = null;
         name = arg1;
         args = arg2;
         handler = arg3;
         ;
         if(handler == null)
         {
         }
         if(args == null)
         {
            args = new Array();
         }
         arguments = {"arguments":args};
         try
         {
            json = arguments;
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,"Api.call: error while encode args to json",error.message);
         }
         if(ExternalInterface.available)
         {
            if(name == AUTHENTICATE_USER)
            {
               Logger.LogToChannel(Logger.TX,"Api.call:",name);
            }
            else
            {
               Logger.LogToChannel(Logger.TX,"Api.call:",name,json);
            }
            try
            {
               setTimeout(ExternalInterface.call,1,name,json);
            }
            catch(error:Error)
            {
               Logger.LogToChannel(Logger.ERROR,"Api.call: error while ExternalInterface.call",error.message);
            }
         }
         else
         {
            Logger.LogToChannel(Logger.ERROR,"Api.call: External interface not available.");
         }
      }
      
      public static function onCallBack(param1:*) : void
      {
         var json:Object = null;
         var response:ApiResponse = null;
         var id:String = null;
         var loc1:* = undefined;
         var arg1:* = param1;
         response = null;
         json = arg1;
         id = ResponseId;
         try
         {
            response = new ApiResponse(arg1);
            Logger.LogToChannel(Logger.RX,"\tApi.onCallBack json:",response.name);
            Api.self.dispatchEvent(new ApiEvent(response));
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,"Api.onCallBack decode error:",error.message);
         }
      }
   }
}

