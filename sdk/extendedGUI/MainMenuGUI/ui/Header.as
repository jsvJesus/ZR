package ui
{
   import com.dvalimona.components.*;
   import communication.*;
   import events.ApiEvent;
   import flash.display.*;
   import flash.events.*;
   import flash.ui.*;
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
      
      public var account:HeaderAccountPanel;
      
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
      
      private var newsBox:VBox;
      
      private var newsLabel:TextShadowed;
      
      private var newsButton:PushButton;
      
      private var newsCaption:LabelShadowed;
      
      private var newsBack:ui.components.BlackPanel;
      
      public var optionBtn:OptionButton;
      
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
         this.background = new Shape();
         addChild(this.background);
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
         this.languageSelector.fixedHeight = 34;
         this.languageSelector.debug = false;
         this.languageSelector.paddingRight = 20;
         this.optionBtn = new OptionButton();
         this.optionBtn.height = 28;
         this.optionBtn.width = 28;
         this.optionBtn.addEventListener(MouseEvent.CLICK,this.onSettingsClick);
         this.optionBtn.addEventListener(MouseEvent.MOUSE_DOWN,this.onMouseGoDown);
         this.optionBtn.addEventListener(MouseEvent.ROLL_OVER,this.onMouseOver);
         this.addChild(this.optionBtn);
         this.account = new HeaderAccountPanel(this);
         this.account.height = 34;
         this.account.y = -48;
         this.account.paddingRight = this.gold_padding + this.account.valueWidth;
         this.account.visible = false;
         this.debugBtnsBox.tabChildren = false;
         this.newsBack = new ui.components.BlackPanel();
         this.newsBack.mouseEnabled = false;
         addChild(this.newsBack);
         this.newsBox = new VBox(this);
         this.newsBox.padding = 10;
         this.newsBox.width = 517;
         this.newsBox.alignment = VBox.LEFT;
         this.newsBox.spacing = 1;
         this.newsBox.debug = false;
         this.newsBox.x = Base.stage.stageWidth - this.newsBox.width - 40;
         this.newsBox.y = 116;
         this.newsBox.paddingTop = 5;
         this.newsBox.paddingLeft = 10;
         this.newsCaption = new LabelShadowed(this.newsBox,0,0,"");
         this.newsCaption.$ = "extendedGUI.RootWindow.premiumButton";
         this.newsCaption.shadowColor = 0;
         this.newsCaption.shadowAlpha = 0.9;
         this.newsCaption.shadowSize = 1;
         this.newsCaption.size = 22;
         this.newsCaption.font = Base.fontName;
         this.newsCaption.color = Style.MENU_LABEL_COLOR;
         this.newsCaption.clipContent = true;
         this.newsCaption.paddingTop = 10;
         this.newsCaption.paddingLeft = 10;
         this.newsBack.width = this.newsBox.width;
         this.newsBack.height = this.newsBox.height;
         this.newsBack.x = this.newsBox.x;
         this.newsBack.y = this.newsBox.y;
         this.newsLabel = new TextShadowed(this.newsBox);
         this.newsLabel.editable = false;
         this.newsLabel.selectable = false;
         this.newsLabel.autoHeight = true;
         this.newsLabel.font = Base.lightFontName;
         this.newsLabel.size = 18;
         this.newsLabel.color = 8947848;
         this.newsLabel.text = "какой-то текст, я хочу посмотреть как будет выглядеть длинный текст";
         this.newsLabel.width = 487;
         this.newsLabel.paddingLeft = 20;
         this.newsLabel.paddingRight = 10;
         this.newsLabel.paddingBottom = 10;
         this.newsButton = new PushButton(this.newsBox,0,200);
         this.newsButton.setSize(100,34);
         this.newsButton.autoWidth = true;
         this.newsButton.tabEnabled = false;
         this.newsButton.label = "Обновить";
         this.newsButton.addEventListener(MouseEvent.CLICK,this.onClickNews);
         Api.self.addEventListener(Api.GET_MINI_NEWS,this.onGetNews);
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         super();
         this.newsVisible = false;
         Api.call(Api.GET_MINI_NEWS);
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
      
      protected function onMouseOver(event:MouseEvent) : void
      {
         this.optionBtn.addEventListener(MouseEvent.ROLL_OUT,this.onMouseOut);
         PlaySounds.onOver();
      }
      
      protected function onMouseOut(event:MouseEvent) : void
      {
         removeEventListener(MouseEvent.ROLL_OUT,this.onMouseOut);
      }
      
      protected function onMouseGoDown(event:MouseEvent) : void
      {
         PlaySounds.onDown();
         PlaySounds.onDown();
         stage.addEventListener(MouseEvent.MOUSE_UP,this.onMouseGoUp);
      }
      
      protected function onMouseGoUp(event:MouseEvent) : void
      {
         stage.removeEventListener(MouseEvent.MOUSE_UP,this.onMouseGoUp);
         PlaySounds.onClick();
      }
      
      private function onSettingsClick(event:MouseEvent) : *
      {
         Base.navigator.showSettingsScreenWhitoutLogin();
      }
      
      private function onMoveDummyX(event:Event) : void
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
      
      private function onMoveDummyY(event:Event) : void
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
      
      private function onSizeDummy(event:Event) : void
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
      
      protected function onAuthFail(event:Event) : void
      {
      }
      
      protected function onAuthSuccess(event:Event) : void
      {
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
         Base.navigator.showDialog("extendedGUI.Dialogs.Warning","extendedGUI.RootWindow.quitMessage",true,[new DialogButtonItem("extendedGUI.Dialogs.Ok",this.onExitOkButton,0.4,[Keyboard.ENTER]),new DialogButtonItem("extendedGUI.Dialogs.Cancel",null,0.6,[Keyboard.ESCAPE])],500,200);
      }
      
      private function doRestart(... args) : void
      {
         Base.navigator.showDialog("extendedGUI.Dialogs.Warning","extendedGUI.Dialogs.askRestart",true,[new DialogButtonItem("extendedGUI.Dialogs.Ok",this.onRestartOkButton,0.61),new DialogButtonItem("extendedGUI.Dialogs.Cancel",null,0.39)]);
      }
      
      protected function onExitOkButton() : void
      {
         Api.call(Api.QUIT_GAME);
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
         Base.stage.addEventListener(Base.STAGE_RESIZE,this.onResize);
         this.onResize();
         setTimeout(this.onResize,100);
         setTimeout(this.onResize,300);
      }
      
      protected function onResize(event:Event = null) : void
      {
         this.draw();
      }
      
      public function set accountName(name:String) : *
      {
         this.account.accountName = name;
         this.draw();
      }
      
      public function invalidateGold() : *
      {
         this.account.paddingRight = this.gold_padding + this.account.valueWidth;
         this.debugBtnsBox.draw();
      }
      
      protected function draw() : void
      {
         this.debugBtnsBox.y = 16;
         this.debugBtnsBox.x = Base.stage.stageWidth - this.debugBtnsBox.width - 10;
         this.debugBtnsBox.draw();
         this.optionBtn.y = this.debugBtnsBox.y + 3;
         this.optionBtn.x = this.debugBtnsBox.x + this.languageSelector.x - this.optionBtn.width - 24;
         Logger.LogToChannel(Logger.LOCALIZATION,"from header",this.languageSelector.width,this.languageSelector.height);
         this.newsBox.x = Base.stage.stageWidth - this.newsBox.width - 40;
         this.newsBox.height = this.newsButton.y + this.newsButton.height;
         this.newsBack.width = this.newsBox.width;
         this.newsBack.height = this.newsBox.height;
         this.newsBack.x = this.newsBox.x;
         this.newsBack.y = this.newsBox.y;
         this.updateAccountPanelPosition();
         this.drawBackground();
      }
      
      public function updateAccountPanelPosition() : *
      {
         this.account.paddingRight = this.gold_padding + this.account.valueWidth;
         this.account.y = 32;
         this.account.x = Base.stage.stageWidth - this.languageSelector.dinamic_width - 100;
      }
      
      public function setNewsDialog(title:String, text:String, buttonText:String = "", local:Boolean = true) : *
      {
         this.newsButton.visible = buttonText.length > 0;
         if(local)
         {
            this.newsCaption.$ = title;
            this.newsLabel.$ = text;
            this.newsButton.$ = buttonText;
         }
         else
         {
            this.newsCaption.text = title;
            this.newsLabel.text = text;
            this.newsButton.label = buttonText;
         }
         setTimeout(this.draw,100);
      }
      
      private function onGetNews(args:ApiEvent) : void
      {
         this.setNewsDialog(args.data.answer.title,args.data.answer.text,args.data.answer.button);
      }
      
      private function onClickNews(event:MouseEvent) : void
      {
         dispatchEvent(new Event("onClickNews"));
      }
      
      public function set newsVisible(value:Boolean) : void
      {
         this.newsBox.visible = value;
         this.newsBack.visible = value;
         this.newsBack.alpha = value ? 1 : 0;
         this.draw();
      }
      
      private function drawBackground() : void
      {
         this.background.graphics.clear();
         this.background.graphics.beginBitmapFill(new headerBackground() as BitmapData);
         this.background.graphics.drawRect(0,0,Base.stage.stageWidth,100);
         this.background.graphics.endFill();
      }
      
      protected function onRemovedFromStage(event:Event) : void
      {
         this.removeEventListener(Event.REMOVED_FROM_STAGE,this.onRemovedFromStage);
         Base.stage.removeEventListener(Event.RESIZE,this.onResize);
      }
   }
}

