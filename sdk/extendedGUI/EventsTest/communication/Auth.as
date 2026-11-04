package communication
{
   import com.dvalimona.components.DialogButtonItem;
   import events.ApiEvent;
   import flash.events.Event;
   import flash.events.EventDispatcher;
   import flash.events.IEventDispatcher;
   import flash.ui.Keyboard;
   import logging.Logger;
   
   public class Auth extends EventDispatcher
   {
      private static var _self:Auth;
      
      public static const AUTH_SUCCESS:String = "auth_success";
      
      public static const AUTH_FAIL:String = "auth_fail";
      
      public var authenticated:Boolean = false;
      
      public function Auth(target:IEventDispatcher = null)
      {
         Api.self.addEventListener(Api.AUTHENTICATE_USER,this.onAuthenticateHandler);
         Api.self.addEventListener(Api.ACTIVATE_ACCOUNT_WINDOW,this.onActivateAccountWindowHandler);
         super(target);
      }
      
      public static function get self() : Auth
      {
         if(!_self)
         {
            _self = new Auth();
         }
         return _self;
      }
      
      public function doLogin(loginPack:Object) : void
      {
         Api.call(Api.AUTHENTICATE_USER,[loginPack]);
      }
      
      public function doLogout() : void
      {
         Api.call(Api.DO_LOG_OUT);
         this.dispatchEvent(new Event(AUTH_FAIL));
      }
      
      protected function onAuthenticateHandler(event:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onAuthenticateHandler",event.data.name,event.data.answer.result);
         this.checkAuthEvent(event);
      }
      
      private function checkAuthEvent(event:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"checkAuth");
         Logger.LogToChannel(Logger.DEBUG,"checkAuth",event.data.name);
         Logger.LogToChannel(Logger.DEBUG,"checkAuth",event.data.name,event.data.answer.message);
         if(event.data.answer.message != null)
         {
            Logger.LogToChannel(Logger.DEBUG,"we have error message",event.data.answer.message);
            this.dispatchEvent(new Event(AUTH_FAIL));
            this.authenticated = false;
            Base.navigator.showDialog("extendedGUI.Dialogs.Warning",event.data.answer.message,true,[new DialogButtonItem("extendedGUI.Dialogs.Ok",null,1,Keyboard.ENTER)],500,250);
         }
         else
         {
            Logger.LogToChannel(Logger.DEBUG,"we do not have error message");
         }
      }
      
      protected function onActivateAccountWindowHandler(event:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Auth.onActivateAccountWindowHandler");
         this.dispatchEvent(new Event(AUTH_SUCCESS));
         Character.Update();
         this.authenticated = true;
      }
   }
}

