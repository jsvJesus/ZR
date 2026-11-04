package ui.screens
{
   import flash.utils.*;
   import logging.*;
   import ui.*;
   import ui.components.*;
   
   public class FirstCharNameScreen extends Screen
   {
      private var charNameWindow:CharNameWindow;
      
      public function FirstCharNameScreen(id:String, depth:uint = 0)
      {
         super(id,depth);
      }
      
      override protected function unfreeze(... args) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"FirstCharNameScreen unfreeze",Navigator.NEWS_DIALOG);
         if(Navigator.NEWS_DIALOG != null)
         {
            Navigator.NEWS_DIALOG.doClose();
         }
      }
      
      override protected function init(... args) : void
      {
         appears = [];
         this.charNameWindow = new CharNameWindow(this);
         this.charNameWindow.draggable = true;
         this.resize();
         this.charNameWindow.setSize(460,350);
         this.charNameWindow.x = (Base.stage.stageWidth - this.charNameWindow.width) / 2;
         this.charNameWindow.y = (Base.stage.stageHeight - this.charNameWindow.height) / 2;
      }
      
      override protected function resize(... args) : void
      {
         if(this.charNameWindow == null)
         {
            return;
         }
         this.charNameWindow.x = (Base.stage.stageWidth - this.charNameWindow.width) / 2;
         this.charNameWindow.y = (Base.stage.stageHeight - this.charNameWindow.height) / 2;
      }
      
      override public function set enabled(value:Boolean) : void
      {
         _enabled = value;
         this.charNameWindow.enabled = _enabled;
      }
      
      protected function setFocus() : void
      {
         Base.stage.focus = this.charNameWindow.nameInput.textField;
      }
      
      override public function showUp() : void
      {
         this.charNameWindow.setSize(460,350);
         this.charNameWindow.x = (Base.stage.stageWidth - this.charNameWindow.width) / 2;
         this.charNameWindow.y = (Base.stage.stageHeight - this.charNameWindow.height) / 2;
         this.charNameWindow.scaleX = 1;
         this.charNameWindow.scaleY = 1;
         this.charNameWindow.alpha = 1;
         this.charNameWindow.setText("");
         setTimeout(this.setFocus,100);
      }
      
      override public function showDown() : void
      {
      }
      
      override public function hideDown(onCompleteFunction:Function = null) : void
      {
         onCompleteFunction();
      }
      
      override public function hideUp(onCompleteFunction:Function = null) : void
      {
         onCompleteFunction();
      }
   }
}

