package ui
{
   import com.dvalimona.components.DialogButtonItem;
   import com.dvalimona.components.HBox;
   import com.dvalimona.components.PushButton;
   import communication.Api;
   import flash.display.Bitmap;
   import flash.display.BitmapData;
   import flash.display.Shape;
   import flash.display.Sprite;
   import flash.events.Event;
   import flash.utils.setTimeout;
   import logging.Logger;
   
   public class Header extends Sprite
   {
      public static const LOGIN_STATE:String = "login_state";
      
      public static const MAIN_MENU_STATE:String = "main_menu_state";
      
      public static const sideMargin:uint = 50;
      
      private var _state:String = "login_state";
      
      private var debugBtnsBox:HBox;
      
      private var logoBitmap:BitmapData;
      
      private var logo:Sprite;
      
      private var background:Shape = new Shape();
      
      private var backgroundBitmap:BitmapData = new noised_half_black_png() as BitmapData;
      
      private var switchLogButton:PushButton;
      
      private var clearLogButton:PushButton;
      
      private var exitButton:PushButton;
      
      private var restartButton:PushButton;
      
      private var languageSelector:LanguageSelector;
      
      public function Header()
      {
         this.addChild(this.background);
         this.logoBitmap = new header_logo_png() as BitmapData;
         this.logo = new Sprite();
         this.logo.addChild(new Bitmap(this.logoBitmap));
         this.addChild(this.logo);
         this.languageSelector = new LanguageSelector(this);
         this.languageSelector.alignment = HBox.BOTTOM;
         this.languageSelector.horizontalAlign = HBox.RIGHT;
         this.languageSelector.fixedWidth = 512;
         this.languageSelector.fixedHeight = 50;
         this.debugBtnsBox = new HBox(this);
         this.debugBtnsBox.alignment = HBox.TOP;
         this.debugBtnsBox.horizontalAlign = HBox.RIGHT;
         this.debugBtnsBox.fixedWidth = 512;
         this.debugBtnsBox.spacing = 1;
         this.exitButton = new PushButton(this.debugBtnsBox,10,10,"X",this.doExit);
         this.exitButton.setSize(36,36);
         this.exitButton.tabEnabled = false;
         this.switchLogButton = new PushButton(this.debugBtnsBox,10,10,"switch log",this.doLogSwitch);
         this.switchLogButton.setSize(100,36);
         this.switchLogButton.tabEnabled = false;
         this.clearLogButton = new PushButton(this.debugBtnsBox,10,10,"clear log",this.doLogClear);
         this.clearLogButton.setSize(100,36);
         this.clearLogButton.tabEnabled = false;
         this.restartButton = new PushButton(this.debugBtnsBox,10,10,"restart",this.doRestart);
         this.restartButton.setSize(100,36);
         this.restartButton.tabEnabled = false;
         this.debugBtnsBox.tabChildren = false;
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         super();
      }
      
      public function get state() : String
      {
         return this._state;
      }
      
      public function set state(value:String) : void
      {
         this._state = value;
         this.draw();
      }
      
      private function doLogClear(... args) : void
      {
         Logger.clear();
      }
      
      private function doLogSwitch(... args) : void
      {
         Logger.Switch();
      }
      
      private function doExit(... args) : void
      {
         Api.call(Api.DO_CLOSE);
      }
      
      private function doRestart(... args) : void
      {
         Base.navigator.showDialog("extendedGUI.Dialogs.Error","Подтвердите перезапуск приложения",true,[new DialogButtonItem("extendedGUI.Dialogs.Ok",this.onRestartOkButton,0.61),new DialogButtonItem("extendedGUI.Dialogs.Cancel",null,0.39)]);
      }
      
      protected function onRestartOkButton() : void
      {
         Logger.LogToChannel(Logger.DEBUG,this,"onRestartOkButton");
         Api.call(Api.DO_RESTART_GAME,[]);
      }
      
      protected function onAddedToStage(event:Event) : void
      {
         this.removeEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         this.addEventListener(Event.REMOVED_FROM_STAGE,this.onRemovedFromStage);
         Base.stage.addEventListener(Event.RESIZE,this.onResize);
         this.onResize();
         setTimeout(this.onResize,100);
      }
      
      protected function onResize(event:Event = null) : void
      {
         this.draw();
      }
      
      protected function draw() : void
      {
         this.debugBtnsBox.y = 10;
         this.debugBtnsBox.x = Base.stage.stageWidth - this.debugBtnsBox.width - 10;
         this.languageSelector.x = Base.stage.stageWidth - this.languageSelector.width - sideMargin;
         this.languageSelector.y = Navigator.HEADER_HEIGHT - this.languageSelector.height;
         Logger.LogToChannel(Logger.LOCALIZATION,"from header",this.languageSelector.width,this.languageSelector.height);
         this.drawBackground();
      }
      
      private function drawBackground() : void
      {
         this.background.graphics.clear();
         this.background.graphics.beginBitmapFill(this.backgroundBitmap,null,true,false);
         this.background.graphics.drawRect(0,0,Base.stage.stageWidth,Navigator.HEADER_HEIGHT);
         this.background.graphics.endFill();
      }
      
      protected function onRemovedFromStage(event:Event) : void
      {
         this.removeEventListener(Event.REMOVED_FROM_STAGE,this.onRemovedFromStage);
         Base.stage.removeEventListener(Event.RESIZE,this.onResize);
      }
   }
}

