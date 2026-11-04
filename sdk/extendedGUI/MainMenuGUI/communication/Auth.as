package communication
{
   import com.dvalimona.components.*;
   import events.*;
   import flash.events.*;
   import logging.*;
   
   public class Auth extends EventDispatcher
   {
      private static var _self:Auth;
      
      public static const AUTH_SUCCESS:String = "auth_success";
      
      public static const AUTH_FAIL:String = "auth_fail";
      
      public var authenticated:Boolean = false;
      
      public var access_level:Number = 0;
      
      public function Auth(arg1:IEventDispatcher = null)
      {
         Api.self.addEventListener(Api.AUTHENTICATE_USER,this.onAuthenticateHandler);
         Api.self.addEventListener(Api.DISCONNECT,this.onAuthenticateHandler);
         Api.self.addEventListener(Api.ACTIVATE_ACCOUNT_WINDOW,this.onActivateAccountWindowHandler);
         Api.self.addEventListener(Api.ACTIVATE_FIRST_CHAR_WINDOW,this.onActivateFirstCharWindowHandler);
         Api.self.addEventListener(Api.GET_ACCESS_LEVEL,this.onAccessLevelChange);
         super(arg1);
      }
      
      public static function get self() : Auth
      {
         if(!_self)
         {
            _self = new Auth();
         }
         return _self;
      }
      
      public function doLogin(arg1:Object) : void
      {
         Api.call(Api.AUTHENTICATE_USER,[arg1]);
      }
      
      public function doLogout() : void
      {
         Api.call(Api.DO_LOG_OUT);
         this.dispatchEvent(new Event(AUTH_FAIL));
      }
      
      protected function onAuthenticateHandler(arg1:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onAuthenticateHandler",arg1.data.name,arg1.data.answer.result);
         this.checkAuthEvent(arg1);
      }
      
      private function checkAuthEvent(arg1:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"checkAuth");
         Logger.LogToChannel(Logger.DEBUG,"checkAuth",arg1.data.name);
         Logger.LogToChannel(Logger.DEBUG,"checkAuth",arg1.data.name,arg1.data.answer.message);
         this.dispatchEvent(new Event(AUTH_FAIL));
         this.authenticated = false;
         this.access_level = 0;
         if(Base.IN_GAME)
         {
            Base.stage.removeChild(Base.stage.getChildAt(0));
            Base.IN_GAME = false;
            Base.navigator.header.exitButton.visible = true;
            Base.navigator.header.account.visible = false;
            BreadCrumbs.Clear();
         }
         Base.navigator.tryToRemoveMe();
         if(arg1.data.answer.message == null || arg1.data.answer.message == "")
         {
            Logger.LogToChannel(Logger.DEBUG,"we do not have error message");
         }
         else
         {
            Logger.LogToChannel(Logger.DEBUG,"we have error message",arg1.data.answer.message);
            Base.navigator.showDialog("extendedGUI.Dialogs.Warning",arg1.data.answer.message,true,[new DialogButtonItem("extendedGUI.Dialogs.Ok",null,1)],500,250,true);
         }
      }
      
      protected function onActivateAccountWindowHandler(arg1:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Auth.onActivateAccountWindowHandler");
         this.dispatchEvent(new Event(AUTH_SUCCESS));
         Base.navigator.footer.serverName = Base.navigator.currentServer;
         Base.navigator.header.accountName = Base.navigator.currentLogin;
         Character.Update();
         this.authenticated = true;
         Api.call(Api.GET_ACCESS_LEVEL);
      }
      
      protected function onAccessLevelChange(arg1:ApiEvent) : void
      {
         var level:Number = 0;
         if(typeof arg1.data.answer == "number")
         {
            level = Number(arg1.data.answer);
         }
         else
         {
            level = Number(arg1.data.answer["access_level"]);
         }
         this.access_level = level;
      }
      
      protected function onActivateFirstCharWindowHandler(arg1:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Auth.onActivateFirstCharWindowHandler");
         this.dispatchEvent(new ScreenEvent(ScreenEvent.GO_SCREEN,MainMenuGUI.CHARNAME_SCREEN));
      }
   }
}

