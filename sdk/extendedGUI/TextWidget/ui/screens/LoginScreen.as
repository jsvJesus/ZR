package ui.screens
{
   import logging.*;
   import ui.*;
   import ui.components.*;
   
   public class LoginScreen extends Screen
   {
      private var loginWindow:LoginWindow;
      
      public function LoginScreen(param1:String, param2:uint = 0)
      {
         super(param1,param2);
      }
      
      override protected function unfreeze(... rest) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"LoginScreen unfreeze",Navigator.NEWS_DIALOG);
         if(Navigator.NEWS_DIALOG != null)
         {
            Navigator.NEWS_DIALOG.doClose();
         }
      }
      
      override protected function init(... rest) : void
      {
         appears = [];
         this.loginWindow = new LoginWindow(this);
         this.loginWindow.draggable = true;
         this.resize();
         this.loginWindow.setSize(460,350);
         this.loginWindow.x = (Base.stage.stageWidth - this.loginWindow.width) / 2;
         this.loginWindow.y = (Base.stage.stageHeight - this.loginWindow.height) / 2;
      }
      
      override protected function resize(... rest) : void
      {
         this.loginWindow.x = (Base.stage.stageWidth - this.loginWindow.width) / 2;
         this.loginWindow.y = (Base.stage.stageHeight - this.loginWindow.height) / 2;
      }
      
      override public function set enabled(param1:Boolean) : void
      {
         _enabled = param1;
         this.loginWindow.enabled = _enabled;
      }
      
      override public function showUp() : void
      {
      }
      
      override public function showDown() : void
      {
      }
      
      override public function hideDown(param1:Function = null) : void
      {
         param1();
      }
      
      override public function hideUp(param1:Function = null) : void
      {
         param1();
      }
   }
}

