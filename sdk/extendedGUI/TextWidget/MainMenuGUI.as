package
{
   import com.dvalimona.components.*;
   import communication.*;
   import events.*;
   import flash.events.*;
   import flash.ui.*;
   import logging.*;
   import ui.*;
   import ui.screens.*;
   
   public class MainMenuGUI extends Base
   {
      public static const TEST_SCREEN:String = "test_screen";
      
      public static const LOGIN_SCREEN:String = "login_screen";
      
      public static const ROOT_SCREEN:String = "root_screen";
      
      public static const CHAR_SCREEN:String = "char_screen";
      
      public static const NEW_CHAR_SCREEN:String = "new_char_screen";
      
      public static const BASE_SETTINGS:String = "base_settings";
      
      public static const SETTINGS_TUNE:String = "settings_tune";
      
      public static const SETTINGS_TUNE_VIDEO_MAIN:String = "settings_tune_video_main";
      
      public static const KEYBINDS_TUNE:String = "keybinds_tune";
      
      public static var TARGET_SCREEN:String = "";
      
      TARGET_SCREEN = "";
      
      public function MainMenuGUI()
      {
         super();
      }
      
      override protected function initGUI() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Base.initGUI");
         navigator.addScreen(new LoginScreen(LOGIN_SCREEN,100));
         navigator.addScreen(new MainScreen(ROOT_SCREEN,200));
         navigator.addScreen(new CharScreen(CHAR_SCREEN,300));
         navigator.addScreen(new NewCharScreen(NEW_CHAR_SCREEN,400));
         navigator.addScreen(new NewSettingsScreen(BASE_SETTINGS,301));
         navigator.addScreen(new SettingsTuneScreen(SETTINGS_TUNE,401));
         navigator.addScreen(new SettingsTuneVideoMainScreen(SETTINGS_TUNE_VIDEO_MAIN,410));
         navigator.addScreen(new SettingsKeyboardScreen(KEYBINDS_TUNE,450));
         navigator.showScreen(LOGIN_SCREEN);
         Auth.self.addEventListener(Auth.AUTH_SUCCESS,this.onAuthSuccess);
         Auth.self.addEventListener(Auth.AUTH_FAIL,this.onAuthFail);
         navigator.addEventListener(ScreenEvent.GO_SCREEN,this.goScreenHandler,true);
         Api.self.addEventListener(Api.RESTART_GAME,this.onRestartGameHandler);
         Api.self.addEventListener(Api.ALERT,this.onAlertHandler);
         Api.self.addEventListener(Api.SHOW_NEWS,this.onNewsHandler);
         Api.self.addEventListener(Api.CONFIRM_WINDOW_SHOW,this.onConfirmWindowShowHandler);
         Api.self.addEventListener(Api.CONFIRM_WINDOW_HIDE,this.onConfirmWindowHideHandler);
         Api.self.addEventListener(Api.OPEN_SETTINGS,this.onOpenSettings);
      }
      
      private function onOpenSettings(param1:Object) : void
      {
         navigator.showScreen(MainMenuGUI.BASE_SETTINGS,true);
      }
      
      protected function goScreenHandler(param1:ScreenEvent) : void
      {
         navigator.showScreen(param1.data);
      }
      
      protected function onConfirmWindowShowHandler(param1:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"MainMenuGUI.onConfirmWindoShowHandler");
         navigator.showConfirmWindow(param1);
      }
      
      protected function onConfirmWindowHideHandler(param1:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"MainMenuGUI.onConfirmWindoHideHandler");
         navigator.hideConfirmWindow();
      }
      
      protected function onNewsHandler(param1:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"MainMenuGUI.onNewsHandler");
         News.content = "<font color=\'#ffffff\' size=\'22\' face=\'GUILight\'>" + param1.data.answer.text + "</font>";
         News.title = param1.data.answer.title;
         News.source = param1.data.answer.source;
         News.doNotShowNewsWindowAnymore = param1.data.answer.showMeNewsEverytime == 0;
         Logger.LogToChannel(Logger.DEBUG,"MainMenuGUI.onNewsHandler2");
         var _loc2_:* = "<font color=\'#ffffff\' size=\'22\' face=\'GUIRegular\'>" + "<p>" + "Hello, this is <font color=\'#54bdff\'><a href=\'event:http://www.ya.ru\'>link</a>.</font>" + "</font>" + "</p>";
         Navigator.NEWS_DIALOG = Base.navigator.showNewsDialog(News.title,News.content,true,[new DialogButtonItem("extendedGUI.NewsWindow.openNewsArchive",this.openNewsArchive,0.3),new DialogButtonItem("extendedGUI.NewsWindow.closeDialog",this.doCloseNewsDialog,0.7,Keyboard.ENTER)],Base.stage.stageWidth * 0.7,Base.stage.stageHeight * 0.7,true);
      }
      
      protected function openNewsArchive() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"MainMenuGUI.openNewsArchive",News.source);
         Api.call(Api.OPEN_URL,[{"url":News.source}]);
         Navigator.NEWS_DIALOG.doClose();
      }
      
      protected function doCloseNewsDialog() : void
      {
         Navigator.NEWS_DIALOG.doClose();
      }
      
      protected function onAlertHandler(param1:ApiEvent) : void
      {
         Base.navigator.showDialog(param1.data.answer.message.title,param1.data.answer.message.text,false,[new DialogButtonItem("extendedGUI.Dialogs.Ok",null,1,Keyboard.ENTER)]);
      }
      
      protected function onRestartGameHandler(param1:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onRestartGameHandler");
         Api.call(Api.DO_RESTART_GAME,[]);
      }
      
      protected function onRestartOkButton() : void
      {
         Logger.LogToChannel(Logger.DEBUG,this,"onRestartOkButton");
         Api.call(Api.DO_RESTART_GAME,[]);
      }
      
      protected function onAuthFail(param1:Event) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Auth.onAuthFail.");
         navigator.showScreen(LOGIN_SCREEN);
      }
      
      protected function onAuthSuccess(param1:Event) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Auth.onAuthSuccess.");
         navigator.showScreen(ROOT_SCREEN);
      }
   }
}

