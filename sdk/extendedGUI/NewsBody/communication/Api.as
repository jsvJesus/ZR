package communication
{
   import events.ApiEvent;
   import flash.events.EventDispatcher;
   import flash.events.IEventDispatcher;
   import flash.external.ExternalInterface;
   import flash.utils.setTimeout;
   import logging.Logger;
   
   public class Api extends EventDispatcher
   {
      private static var _self:Api;
      
      public static const CTRLV:String = "controlV";
      
      public static const ALERT:String = "alert";
      
      public static const SHOW_NEWS:String = "showNews";
      
      public static const GET_EULA_ACCEPTED:String = "getEulaAccepted";
      
      public static const SET_EULA_ACCEPTED:String = "setEulaAccepted";
      
      public static const GET_CLIENT_VERSION:String = "getClientVersion";
      
      public static const IS_IN_GAME:String = "isInGame";
      
      public static const GET_SETTINGS:String = "getSettings";
      
      public static const GET_SETTINGS_RANGE:String = "getSettingsRange";
      
      public static const GET_CURRENT_LOCALE:String = "getCurrentLocale";
      
      public static const SET_CURRENT_LOCALE:String = "setCurrentLocale";
      
      public static const GET_LOCALES_LIST:String = "getLocalesList";
      
      public static const GET_LOCALIZED_RESOURCE:String = "getLocalizedResource";
      
      public static const GET_KEYBIND_LOC_TABLE:String = "getKeybindLocalizeTable";
      
      public static const GET_SERVER_LIST:String = "getServerList";
      
      public static const GET_LOGIN:String = "getLogin";
      
      public static const SET_SETTINGS:String = "setSettings";
      
      public static const GET_DEFAULT_SETTINGS:String = "getDefaultOption";
      
      public static const SET_DEFAULT_KEY_BINDINGS:String = "setDefaultKeyBindings";
      
      public static const DO_LOG_OUT:String = "doLogOut";
      
      public static const QUIT_GAME:String = "quitGame";
      
      public static const RESTART_GAME:String = "restartGame";
      
      public static const DO_RESTART_GAME:String = "doRestartGame";
      
      public static const SHOW_DEVELOPER_SERVERS:String = "showDeveloperServer";
      
      public static const AUTHENTICATE_USER:String = "authenticateUser";
      
      public static const ACTIVATE_ACCOUNT_WINDOW:String = "activateAccountWindow";
      
      public static const ALL_CHARACTERS_INFO:String = "allCharactersInfo";
      
      public static const GET_PREMIUM_ACCOUNT_DATA:String = "getPremiumAccountData";
      
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
      
      public static const INCOMING_MESSAGE:String = "incoming_message";
      
      public static const SEND_MESSAGE:String = "send_message";
      
      public static const CONFIRM_WINDOW_SHOW:String = "ConfirmWindow.show";
      
      public static const CONFIRM_WINDOW_HIDE:String = "ConfirmWindow.hide";
      
      public static const CONFIRM_WINDOW_OK:String = "ConfirmWindow.click_ok";
      
      public static const CONFIRM_WINDOW_RESEND:String = "ConfirmWindow.click_resend";
      
      public static const CHOOSE_DIRECTORY:String = "choose_directory";
      
      public static const CHOOSE_FILE:String = "choose_file";
      
      public static const CHANGE_ROOT:String = "change_root";
      
      public static const UP_LEVEL:String = "up_directory";
      
      public static const PUSH_CANCEL:String = "push_cancel";
      
      public static const FILELIST:String = "file_list";
      
      public static const FILESYSTEM_ROOTS:String = "filesystem_roots";
      
      public static const READY:String = "ready";
      
      private static var idCount:uint = 0;
      
      public function Api(target:IEventDispatcher = null)
      {
         super(target);
      }
      
      private static function get ResponseId() : String
      {
         var result:String = "R" + idCount;
         ++idCount;
         return result;
      }
      
      public static function get self() : Api
      {
         if(!_self)
         {
            _self = new Api();
         }
         return _self;
      }
      
      public static function call(name:String, args:Array = null, handler:Function = null) : void
      {
         var json:String = null;
         if(handler != null)
         {
         }
         if(args == null)
         {
            args = new Array();
         }
         arguments = {"arguments":args};
         try
         {
            json = doJSON.encode(arguments);
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,"Api.call: error while encode args to json",error.message);
         }
         if(ExternalInterface.available)
         {
            Logger.LogToChannel(Logger.TX,"Api.call:",name,json);
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
      
      public static function onCallBack(json:String) : void
      {
         var response:ApiResponse = null;
         var id:String = ResponseId;
         try
         {
            Logger.LogToChannel(Logger.RX,"Api.onCallBack ",id,"{");
            response = new ApiResponse(doJSON.decode(json));
            Logger.LogToChannel(Logger.RX,"\tApi.onCallBack json:",response.name);
            Logger.LogToChannel(Logger.RX,"\tApi.onCallBack json:",response.name,json.substr(0,300));
            Api.self.dispatchEvent(new ApiEvent(response));
            Logger.LogToChannel(Logger.RX,"} Api.onCallBack ",id);
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,"Api.onCallBack decode error:",error.message);
         }
      }
   }
}

