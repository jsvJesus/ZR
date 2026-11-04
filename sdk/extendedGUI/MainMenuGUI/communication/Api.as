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
      
      public static const DEBUG:Boolean = false;
      
      public static const STEAM_MODE:String = "steam_mode";
      
      public static const OPEN_SETTINGS:String = "open_settings";
      
      public static const CLOSE_SETTINGS:String = "close_settings";
      
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
      
      public static const GET_REMEMBER_PASS:String = "ActionsWithLogin.get_remember_pass";
      
      public static const SET_SETTINGS:String = "setSettings";
      
      public static const SET_CONTRAST_BRIGHTNESS:String = "setContrastBrightness";
      
      public static const SET_MAX_FPS:String = "setMaxFrameRate";
      
      public static const GET_DEFAULT_SETTINGS:String = "getDefaultOption";
      
      public static const SET_DEFAULT_KEY_BINDINGS:String = "setDefaultKeyBindings";
      
      public static const SET_ONE_SETTINGS:String = "update_one_setting";
      
      public static const DO_LOG_OUT:String = "ActionsWithLogin.doLogOut";
      
      public static const QUIT_GAME:String = "quitGame";
      
      public static const RESTART_GAME:String = "restartGame";
      
      public static const DO_RESTART_GAME:String = "doRestartGame";
      
      public static const SHOW_DEVELOPER_SERVERS:String = "ActionsWithLogin.showDeveloperServer";
      
      public static const AUTHENTICATE_USER:String = "ActionsWithLogin.authenticateUser";
      
      public static const DISCONNECT:String = "Disconnect";
      
      public static const USER_PARAMS:String = "user_params";
      
      public static const ACTIVATE_ACCOUNT_WINDOW:String = "activateAccountWindow";
      
      public static const ACTIVATE_FIRST_CHAR_WINDOW:String = "activateFirstCharWindow";
      
      public static const ALL_CHARACTERS_INFO:String = "allCharactersInfo";
      
      public static const GET_PREMIUM_ACCOUNT_DATA:String = "getPremiumAccountData";
      
      public static const PREMIUM_ACCOUNT_DATA:String = "premiumAccountData";
      
      public static const SELECT_CHAR:String = "selectChar";
      
      public static const DELETE_CHAR:String = "deleteCharacter";
      
      public static const RESTORE_CHAR:String = "restoreCharacter";
      
      public static const CREATING_CHAR:String = "creatingChar";
      
      public static const UPDATE_CHAR:String = "updateChar";
      
      public static const DONAT_UPDATE_CHAR:String = "donateFaceChange";
      
      public static const DONAT_UPDATING_CHAR:String = "donateFaceChanging";
      
      public static const DONAT_PAINT_CHAR:String = "donatePaintChange";
      
      public static const CALL_SURVEY:String = "callSurvey";
      
      public static const HIDE_SURVEY:String = "hideSurvey";
      
      public static const UPDATING_CHAR:String = "updatingChar";
      
      public static const NEW_CHAR_VIEW:String = "newCharView";
      
      public static const NEW_FULL_CHAR_VIEW:String = "newFullCharView";
      
      public static const FACE_CHAR_VIEW:String = "faceCharView";
      
      public static const FULL_FACE_CHAR_VIEW:String = "fullFaceCharView";
      
      public static const CHECK_AVATAR_NAME:String = "checkAvatarName";
      
      public static const CREATE_CHAR:String = "createChar";
      
      public static const CANCEL_CREATE_CHAR:String = "cancelCreateChar";
      
      public static const GO_TO_GAME:String = "goToGame";
      
      public static const GET_NEW_KEYBIND:String = "getNewKeybind";
      
      public static const OPEN_URL:String = "openURL";
      
      public static const UPDATE_PREMIUM:String = "updatePremium";
      
      public static const UPDATE_BOOSTER:String = "updateBooster";
      
      public static const UPDATE_EVENT:String = "updateEvent";
      
      public static const UPDATE_GOLD:String = "updateGold";
      
      public static const UPDATE_PLATINUM:String = "updatePlatinum";
      
      public static const SHOW_ME_NEWS_EVERYTIME:String = "showMeNewsEverytime";
      
      public static const INCOMING_MESSAGE:String = "incoming_message";
      
      public static const SEND_MESSAGE:String = "send_message";
      
      public static const CTRLV:String = "controlV";
      
      public static const CONFIRM_WINDOW_HIDE:String = "ConfirmWindow.hide";
      
      public static const CONFIRM_WINDOW_OK:String = "ConfirmWindow.click_ok";
      
      public static const CONFIRM_WINDOW_CHANGE_TYPE:String = "ConfirmWindow.change_confirm_type";
      
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
      
      public static const TRY_SHOW_NEWS:String = "tryShowNews";
      
      public static const CLOSE_NEWS:String = "closeNews";
      
      public static const FILELIST:String = "file_list";
      
      public static const REJECT_PREM:String = "reject_prem";
      
      public static const RESET_GUI_POS:String = "gui_reset_position";
      
      public static const GET_MINI_NEWS:String = "get_mini_news";
      
      public static const GET_FACE_CONFIG:String = "get_face_config";
      
      public static const CHAR_MAKER_CENTER_CAMERA:String = "set_center_camera";
      
      public static const CHAR_MAKER_FACE_MODE:String = "set_face_mode";
      
      public static const RESET_CHAR_VIEW:String = "reset_char_view";
      
      public static const RESET_FACE_VIEW:String = "reset_face_view";
      
      public static const NEED_PAY_WINDOW:String = "show_need_pay_window";
      
      public static const PAY_RESULT:String = "pbt_pay_result";
      
      public static const TRY_PAY:String = "try_pay";
      
      public static const ON_SELECT_CLOTH:String = "on_select_clothgroup";
      
      public static const ON_CHAR_MENU_MODE:String = "on_char_menu_mode";
      
      public static const SET_FACE_FORM:String = "set_face_form";
      
      public static const SET_FACE_VALUES:String = "set_face_values";
      
      public static const FACE_SAVE_CLICK:String = "face_save_click";
      
      public static const FACE_LOAD_CLICK:String = "face_load_click";
      
      public static const GET_ACCESS_LEVEL:String = "get_access_level";
      
      public static const GET_RANDOM_PERSONALITY:String = "generate_random_personality";
      
      public static const RECEIVEE_RANDOM_PERSONALITY:String = "receive_random_personality";
      
      public static const OPEN_SHOP:String = "open_shop";
      
      public static const OPEN_STORAGE:String = "open_storage";
      
      public static const SHOP_NOTIFICATION_COUNT:String = "shop_notification_count";
      
      public static const SHOP_OPENED:String = "shop_opened";
      
      public static const STORAGE_NOTIFICATION_COUNT:String = "storage_notification_count";
      
      public static const LOCK_CHAR_BUTTONS:String = "lock_char_buttons";
      
      public static const OPEN_GOLD:String = "add_gold_in_browser";
      
      public static const CHANGE_FACE_COST:String = "change_face_cost";
      
      public static const CHANGE_FACE_BTN_ENABLED:String = "change_face_btn_enabled";
      
      public static const PARTNER_ID_CHANGE:String = "partner_id_change";
      
      public static const GOLD_VISIBLE:String = "gold_visible";
      
      public static const PASSWORD_TEXT:String = "send_password_text";
      
      public static const REMEMBER_PASS_CHECK:String = "remember_pass_check";
      
      public static const SHOW_DIALOG:String = "show_dialog";
      
      public static const SHOW_DIALOG_SHIELD:String = "show_dialog_shield";
      
      public static const HIDE_DIALOG_SHIELD:String = "hide_dialog_shield";
      
      public static const SHOW_PREMIUM_SHOP:String = "show_premium_shop";
      
      public static const ON_FACE_CHANGED:String = "on_face_changed";
      
      public static const STEAM_TRUSTED:String = "set_steam_trusted";
      
      private static var idCount:uint = 0;
      
      idCount = 0;
      
      public function Api(arg1:IEventDispatcher = null)
      {
         super(arg1);
      }
      
      private static function get ResponseId() : String
      {
         var loc2:* = undefined;
         var loc1:* = "R" + idCount;
         ++idCount;
         return loc1;
      }
      
      public static function get self() : Api
      {
         if(!_self)
         {
            _self = new Api();
         }
         return _self;
      }
      
      public static function call(arg1:String, arg2:Array = null, arg3:Function = null) : void
      {
         var name:String = null;
         var json:Object = null;
         var loc1:* = undefined;
         var args:Array = null;
         var handler:Function = null;
         if(DEBUG)
         {
         }
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
      
      public static function onCallBack(arg1:*) : void
      {
         var json:Object = null;
         var response:ApiResponse = null;
         var id:String = null;
         var loc1:* = undefined;
         var i:* = undefined;
         response = null;
         json = arg1;
         id = ResponseId;
         try
         {
            response = new ApiResponse(arg1);
            Logger.LogToChannel(Logger.RX,"\tApi.onCallBack json:",response.name);
            Api.self.dispatchEvent(new ApiEvent(response));
            if(DEBUG)
            {
               for(i in response.answer)
               {
               }
            }
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,"Api.onCallBack decode error:",error.message);
         }
      }
   }
}

