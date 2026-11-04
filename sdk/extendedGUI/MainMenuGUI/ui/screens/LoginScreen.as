package ui.screens
{
   import logging.Logger;
   import ui.Navigator;
   import ui.Screen;
   import ui.components.LoginWindow;
   
   public class LoginScreen extends Screen
   {
      private var loginWindow:LoginWindow;
      
      public function LoginScreen(id:String, depth:uint = 0)
      {
         super(id,depth);
      }
      
      override protected function unfreeze(... args) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"LoginScreen unfreeze",Navigator.NEWS_DIALOG);
         if(Navigator.NEWS_DIALOG != null)
         {
            Navigator.NEWS_DIALOG.doClose();
         }
      }
      
      override protected function init(... args) : void
      {
         appears = [];
         this.loginWindow = new LoginWindow(this);
         this.loginWindow.draggable = true;
         this.loginWindow.setSize(460,350);
         this.resize();
      }
      
      override protected function resize(... args) : void
      {
         if(this.loginWindow == null)
         {
            return;
         }
         this.loginWindow.x = (Base.stage.stageWidth - this.loginWindow.width) / 2;
         this.loginWindow.y = (Base.stage.stageHeight - this.loginWindow.height) / 2;
      }
      
      override public function set enabled(value:Boolean) : void
      {
         _enabled = value;
         if(this.loginWindow != null)
         {
            this.loginWindow.enabled = value;
         }
      }
      
      override public function showUp() : void
      {
      }
      
      override public function showDown() : void
      {
      }
      
      override public function hideDown(onCompleteFunction:Function = null) : void
      {
         if(onCompleteFunction != null)
         {
            onCompleteFunction();
         }
      }
      
      override public function hideUp(onCompleteFunction:Function = null) : void
      {
         if(onCompleteFunction != null)
         {
            onCompleteFunction();
         }
      }
   }
}

