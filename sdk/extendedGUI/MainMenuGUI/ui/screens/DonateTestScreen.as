package ui.screens
{
   import communication.*;
   import flash.events.Event;
   import flash.utils.*;
   import logging.*;
   import ui.*;
   import ui.components.*;
   
   public class DonateTestScreen extends Screen
   {
      private var donatTestWindow:DonatTestWindow;
      
      public var vis_pbt:Boolean = false;
      
      public function DonateTestScreen(id:String, depth:uint = 0)
      {
         super(id,depth);
         Auth.self.addEventListener(Auth.AUTH_FAIL,this.onAuthFail);
      }
      
      override protected function unfreeze(... args) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"DonateTestScreen unfreeze",Navigator.NEWS_DIALOG);
      }
      
      override protected function init(... args) : void
      {
         appears = [];
         this.donatTestWindow = new DonatTestWindow(this);
         this.donatTestWindow.draggable = true;
         this.resize();
         this.donatTestWindow.setSize(460,350);
         this.donatTestWindow.x = (Base.stage.stageWidth - this.donatTestWindow.width) / 2;
         this.donatTestWindow.y = (Base.stage.stageHeight - this.donatTestWindow.height) / 2;
      }
      
      protected function onAuthFail(event:Event) : void
      {
         Base.navigator.contentShield.hide();
         Base.navigator.leftSide.removeChild(this);
      }
      
      override protected function resize(... args) : void
      {
         if(this.donatTestWindow == null)
         {
            return;
         }
         this.donatTestWindow.x = (Base.stage.stageWidth - this.donatTestWindow.width) / 2;
         this.donatTestWindow.y = (Base.stage.stageHeight - this.donatTestWindow.height) / 2;
      }
      
      override public function set enabled(value:Boolean) : void
      {
         _enabled = value;
         this.donatTestWindow.enabled = _enabled;
      }
      
      protected function setFocus() : void
      {
      }
      
      override public function showUp() : void
      {
         this.donatTestWindow.setSize(460,350);
         this.donatTestWindow.x = (Base.stage.stageWidth - this.donatTestWindow.width) / 2;
         this.donatTestWindow.y = (Base.stage.stageHeight - this.donatTestWindow.height) / 2;
         this.donatTestWindow.scaleX = 1;
         this.donatTestWindow.scaleY = 1;
         this.donatTestWindow.alpha = 1;
         this.vis_pbt = true;
         setTimeout(this.setFocus,100);
      }
      
      override public function showDown() : void
      {
         this.vis_pbt = true;
      }
      
      override public function hideDown(onCompleteFunction:Function = null) : void
      {
         onCompleteFunction();
         this.vis_pbt = false;
      }
      
      override public function hideUp(onCompleteFunction:Function = null) : void
      {
         onCompleteFunction();
         this.vis_pbt = false;
      }
   }
}

