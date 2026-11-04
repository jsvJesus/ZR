package ui
{
   import com.dvalimona.components.*;
   import communication.*;
   import flash.display.*;
   import flash.events.*;
   import flash.utils.*;
   import logging.*;
   import ui.components.*;
   
   public class Header extends Sprite
   {
      public static const LOGIN_STATE:String = "login_state";
      
      public static const MAIN_MENU_STATE:String = "main_menu_state";
      
      public static const sideMargin:uint = 50;
      
      public const gold_padding:* = 90;
      
      private var _state:String = "login_state";
      
      public var gold:GoldPanel;
      
      private var debugBtnsBox:HBox;
      
      private var logoBitmap:BitmapData;
      
      private var logo:Sprite;
      
      private var background:Shape;
      
      private var backgroundBitmap:BitmapData;
      
      private var switchLogButton:PushButton;
      
      private var clearLogButton:PushButton;
      
      public var exitButton:PushButton;
      
      private var restartButton:PushButton;
      
      private var languageSelector:LanguageSelector;
      
      private var crumbs:BreadCrumbsLabel;
      
      private var crumbsHolder:Sprite;
      
      private var showDummy:PushButton;
      
      private var hideDummy:PushButton;
      
      private var moveDummyXY:PushButton;
      
      private var moveDummyX:PushButton;
      
      private var moveDummyY:PushButton;
      
      private var sizeDummy:PushButton;
      
      private var sizeInput:InputText;
      
      private var xInput:InputText;
      
      private var yInput:InputText;
      
      public function Header()
      {
         Auth.self.addEventListener(Auth.AUTH_SUCCESS,this.onAuthSuccess);
         Auth.self.addEventListener(Auth.AUTH_FAIL,this.onAuthFail);
         this.backgroundBitmap = new noised_half_black_png() as BitmapData;
         this.background = new Shape();
         this.debugBtnsBox = new HBox(this);
         this.debugBtnsBox.alignment = HBox.TOP;
         this.debugBtnsBox.horizontalAlign = HBox.RIGHT;
         this.debugBtnsBox.fixedWidth = 1024;
         this.debugBtnsBox.spacing = 1;
         this.exitButton = new PushButton(this.debugBtnsBox,10,10,"X",this.doExit);
         this.exitButton.setSize(40,34);
         this.exitButton.tabEnabled = false;
         this.switchLogButton = new PushButton(null,10,10,"switch log",this.doLogSwitch);
         this.switchLogButton.setSize(100,34);
         this.switchLogButton.tabEnabled = false;
         if(Base.DEBUG)
         {
            this.debugBtnsBox.addChild(this.switchLogButton);
         }
         this.clearLogButton = new PushButton(null,10,10,"clear log",this.doLogClear);
         this.clearLogButton.setSize(100,34);
         this.clearLogButton.tabEnabled = false;
         if(Base.DEBUG)
         {
            this.debugBtnsBox.addChild(this.clearLogButton);
         }
         this.restartButton = new PushButton(null,10,10,"restart",this.doRestart);
         this.restartButton.setSize(100,34);
         this.restartButton.tabEnabled = false;
         if(Base.DEBUG)
         {
            this.debugBtnsBox.addChild(this.restartButton);
         }
         this.showDummy = new PushButton(null,10,10,"show",Dummy.show);
         this.showDummy.setSize(75,34);
         this.showDummy.tabEnabled = false;
         if(Base.DEBUG && Boolean(Dummy.DEBUG))
         {
            this.debugBtnsBox.addChild(this.showDummy);
         }
         this.hideDummy = new PushButton(null,10,10,"hide",Dummy.hide);
         this.hideDummy.setSize(75,34);
         this.hideDummy.tabEnabled = false;
         if(Base.DEBUG && Boolean(Dummy.DEBUG))
         {
            this.debugBtnsBox.addChild(this.hideDummy);
         }
         this.xInput = new InputText();
         this.xInput.setSize(50,34);
         this.xInput.paddingRight = 5;
         if(Base.DEBUG && Boolean(Dummy.DEBUG))
         {
            this.debugBtnsBox.addChild(this.xInput);
         }
         this.moveDummyX = new PushButton(null,10,10,"X",this.onMoveDummyX);
         this.moveDummyX.setSize(40,34);
         this.moveDummyX.tabEnabled = false;
         if(Base.DEBUG && Boolean(Dummy.DEBUG))
         {
            this.debugBtnsBox.addChild(this.moveDummyX);
         }
         this.yInput = new InputText();
         this.yInput.setSize(50,34);
         this.yInput.paddingRight = 5;
         if(Base.DEBUG && Boolean(Dummy.DEBUG))
         {
            this.debugBtnsBox.addChild(this.yInput);
         }
         this.moveDummyY = new PushButton(null,10,10,"Y",this.onMoveDummyY);
         this.moveDummyY.setSize(40,34);
         this.moveDummyY.tabEnabled = false;
         if(Base.DEBUG && Boolean(Dummy.DEBUG))
         {
            this.debugBtnsBox.addChild(this.moveDummyY);
         }
         this.sizeInput = new InputText();
         this.sizeInput.setSize(50,34);
         this.sizeInput.paddingRight = 5;
         if(Base.DEBUG && Boolean(Dummy.DEBUG))
         {
            this.debugBtnsBox.addChild(this.sizeInput);
         }
         this.sizeDummy = new PushButton(null,10,10,"size",this.onSizeDummy);
         this.sizeDummy.setSize(75,34);
         this.sizeDummy.tabEnabled = false;
         if(Base.DEBUG && Boolean(Dummy.DEBUG))
         {
            this.debugBtnsBox.addChild(this.sizeDummy);
         }
         this.languageSelector = new LanguageSelector(this.debugBtnsBox);
         this.languageSelector.alignment = HBox.MIDDLE;
         this.languageSelector.horizontalAlign = HBox.RIGHT;
         this.languageSelector.fixedWidth = 200;
         this.languageSelector.fixedHeight = 34;
         this.languageSelector.debug = false;
         this.languageSelector.paddingRight = 20;
         this.gold = new GoldPanel(this.debugBtnsBox);
         this.gold.height = 34;
         this.gold.paddingRight = this.gold_padding + this.gold.valueWidth;
         this.debugBtnsBox.tabChildren = false;
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         super();
      }
      
      public function get state() : String
      {
         return this._state;
      }
      
      public function set state(param1:String) : void
      {
         this._state = param1;
         this.draw();
      }
      
      private function onMoveDummyX(param1:Event) : void
      {
         if(this.xInput.text.length)
         {
            if(!isNaN(Number(this.xInput.text)))
            {
               Dummy.moveX(Number(this.xInput.text),0.1);
            }
         }
         else
         {
            Dummy.moveTestRandomX();
         }
      }
      
      private function onMoveDummyY(param1:Event) : void
      {
         if(this.yInput.text.length)
         {
            if(!isNaN(Number(this.yInput.text)))
            {
               Dummy.moveY(Number(this.yInput.text),0.3);
            }
         }
         else
         {
            Dummy.moveTestRandomY();
         }
      }
      
      private function onSizeDummy(param1:Event) : void
      {
         if(this.sizeInput.text.length)
         {
            if(!isNaN(Number(this.sizeInput.text)))
            {
               Dummy.setSize(Number(this.sizeInput.text));
            }
         }
         else
         {
            Dummy.sizeTestRandom();
         }
      }
      
      protected function onAuthFail(param1:Event) : void
      {
      }
      
      protected function onAuthSuccess(param1:Event) : void
      {
      }
      
      private function doLogClear(... rest) : void
      {
         Logger.clear();
      }
      
      private function doLogSwitch(... rest) : void
      {
         Logger.Switch();
      }
      
      private function doExit(... rest) : void
      {
         Api.call(Api.QUIT_GAME);
      }
      
      private function doRestart(... rest) : void
      {
         Base.navigator.showDialog("extendedGUI.Dialogs.Warning","Подтвердите перезапуск приложения",true,[new DialogButtonItem("extendedGUI.Dialogs.Ok",this.onRestartOkButton,0.61),new DialogButtonItem("extendedGUI.Dialogs.Cancel",null,0.39)]);
      }
      
      protected function onRestartOkButton() : void
      {
         Logger.LogToChannel(Logger.DEBUG,this,"onRestartOkButton");
         Api.call(Api.DO_RESTART_GAME,[]);
      }
      
      protected function onAddedToStage(param1:Event) : void
      {
         this.removeEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         this.addEventListener(Event.REMOVED_FROM_STAGE,this.onRemovedFromStage);
         Base.stage.addEventListener(Base.STAGE_RESIZE,this.onResize);
         this.onResize();
         setTimeout(this.onResize,100);
      }
      
      protected function onResize(param1:Event = null) : void
      {
         this.draw();
      }
      
      public function invalidateGold() : *
      {
         this.gold.paddingRight = this.gold_padding + this.gold.valueWidth;
         this.debugBtnsBox.draw();
      }
      
      protected function draw() : void
      {
         this.gold.paddingRight = this.gold_padding + this.gold.valueWidth;
         this.debugBtnsBox.y = 10;
         this.debugBtnsBox.x = Base.stage.stageWidth - this.debugBtnsBox.width - 10;
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
      
      protected function onRemovedFromStage(param1:Event) : void
      {
         this.removeEventListener(Event.REMOVED_FROM_STAGE,this.onRemovedFromStage);
         Base.stage.removeEventListener(Event.RESIZE,this.onResize);
      }
   }
}

