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
      
      public function Auth(param1:IEventDispatcher = null)
      {
         Api.self.addEventListener(Api.AUTHENTICATE_USER,this.onAuthenticateHandler);
         Api.self.addEventListener(Api.ACTIVATE_ACCOUNT_WINDOW,this.onActivateAccountWindowHandler);
         super(param1);
      }
      
      public static function get self() : Auth
      {
         if(!_self)
         {
            _self = new Auth();
         }
         return _self;
      }
      
      public function doLogin(param1:Object) : void
      {
         Api.call(Api.AUTHENTICATE_USER,[param1]);
      }
      
      public function doLogout() : void
      {
         Api.call(Api.DO_LOG_OUT);
         this.dispatchEvent(new Event(AUTH_FAIL));
      }
      
      protected function onAuthenticateHandler(param1:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onAuthenticateHandler",param1.data.name,param1.data.answer.result);
         this.checkAuthEvent(param1);
      }
      
      private function checkAuthEvent(param1:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"checkAuth");
         Logger.LogToChannel(Logger.DEBUG,"checkAuth",param1.data.name);
         Logger.LogToChannel(Logger.DEBUG,"checkAuth",param1.data.name,param1.data.answer.message);
         if(param1.data.answer.message == null)
         {
            Logger.LogToChannel(Logger.DEBUG,"we do not have error message");
         }
         else
         {
            Logger.LogToChannel(Logger.DEBUG,"we have error message",param1.data.answer.message);
            this.dispatchEvent(new Event(AUTH_FAIL));
            this.authenticated = false;
            Base.navigator.showDialog("extendedGUI.Dialogs.Warning",param1.data.answer.message,true,[new DialogButtonItem("extendedGUI.Dialogs.Ok",null,1)],500,250);
         }
      }
      
      protected function onActivateAccountWindowHandler(param1:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Auth.onActivateAccountWindowHandler");
         this.dispatchEvent(new Event(AUTH_SUCCESS));
         Base.navigator.footer.serverName = Base.navigator.currentServer;
         Character.Update();
         this.authenticated = true;
      }
   }
}

