package ui
{
   import com.dvalimona.components.*;
   import com.greensock.*;
   import com.greensock.easing.*;
   import communication.*;
   import events.ApiEvent;
   import flash.display.*;
   import flash.events.*;
   import flash.geom.*;
   import flash.text.*;
   import flash.ui.*;
   import flash.utils.*;
   import logging.*;
   import ui.components.*;
   import ui.screens.NewCharScreen2;
   import ui.screens.NewSettingsScreen;
   
   public class Navigator extends Sprite
   {
      public static var NEWS_DIALOG:DialogWindow;
      
      public static var FManager:FocusManager;
      
      public static const HEADER_HEIGHT:uint = 100;
      
      public static const FOOTER_HEIGHT:uint = 56;
      
      protected static const SWAP_SIZE:int = 0;
      
      protected static const SHIFT_X_SIZE:uint = 300;
      
      public static const SideRotation:uint = 30;
      
      private static var _deeper:Boolean = false;
      
      public var is_auth:Boolean = false;
      
      public var is_lock:Boolean = false;
      
      public var items:Object;
      
      public var alerts_helper:Dictionary = new Dictionary();
      
      private var _currentScreen:Screen;
      
      public var currentServer:String;
      
      public var currentLogin:String;
      
      public var content:Sprite;
      
      public var leftSide:Sprite;
      
      public var rightSide:Sprite;
      
      public var toolTip:ToolTip;
      
      public var header:Header;
      
      public var footer:Footer;
      
      public var shield:Shield;
      
      public var premiumShield:Shield;
      
      public var contentShield:Shield;
      
      public var modalShield:Shield;
      
      public var darkShield:Sprite;
      
      public var dialogs:Sprite;
      
      public var premiumDialogs:Sprite;
      
      public var modal:Sprite;
      
      public var keyboardMessageHolder:Sprite;
      
      public var keyboardMessage:KeyboardMessage;
      
      public var focusHolder:Sprite;
      
      public var focus:Focus;
      
      private var background:Shape;
      
      private var backgroundPicture:Background;
      
      private var crumbs:BreadCrumbsLabel;
      
      private var crumbsHolder:Sprite;
      
      public var oldScreen:Screen;
      
      public function Navigator(showHeader:Boolean = true, showFooter:Boolean = true, showBackground:Boolean = true)
      {
         FManager = new FocusManager();
         this.items = new Object();
         this.background = new Shape();
         this.background.alpha = 0.1;
         if(showBackground)
         {
            this.addChild(this.background);
         }
         this.addChild(this.content = new Sprite());
         this.leftSide = new Sprite();
         this.leftSide.addChild(this.crumbsHolder = new Sprite());
         this.crumbs = new BreadCrumbsLabel(this.crumbsHolder);
         this.crumbs.x = 30;
         this.crumbs.y = 30;
         this.rightSide = new Sprite();
         this.content.addChild(this.contentShield = new Shield());
         this.header = new Header();
         if(showHeader)
         {
            this.content.addChild(this.header);
         }
         this.footer = new Footer();
         if(showFooter)
         {
            this.content.addChild(this.footer);
         }
         this.content.addChild(this.leftSide);
         this.content.addChild(this.rightSide);
         this.addChild(this.premiumShield = new Shield());
         this.addChild(this.premiumDialogs = new Sprite());
         this.addChild(this.shield = new Shield());
         this.addChild(this.keyboardMessageHolder = new Sprite());
         this.addChild(this.dialogs = new Sprite());
         this.addChild(this.modalShield = new Shield());
         this.addChild(this.darkShield = new Sprite());
         this.addChild(this.modal = new Sprite());
         this.addChild(this.focusHolder = new Sprite());
         this.focusHolder.addChild(this.focus = new Focus());
         this.focusHolder.mouseEnabled = false;
         this.focusHolder.mouseChildren = false;
         this.focus.mouseEnabled = false;
         this.focus.mouseChildren = false;
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         Auth.self.addEventListener(Auth.AUTH_SUCCESS,this.onAuthSuccess);
         Auth.self.addEventListener(Auth.AUTH_FAIL,this.onAuthFail);
         Api.self.addEventListener(Api.USER_PARAMS,this.onUserParams);
         Api.self.addEventListener(Api.SHOW_DIALOG,this.onPythonShowDialog);
         Api.self.addEventListener(Api.SHOW_DIALOG_SHIELD,this.onPythonShowShield);
         Api.self.addEventListener(Api.HIDE_DIALOG_SHIELD,this.onPythonHideShield);
         super();
      }
      
      public static function get ScreenHeight() : Number
      {
         return Base.stage.stageHeight - HEADER_HEIGHT - FOOTER_HEIGHT;
      }
      
      public static function get LeftWidth() : uint
      {
         var lowestWidth:uint = 400;
         var lowestScreenWidth:uint = 1024;
         var ratio:Number = Base.stage.stageWidth / lowestScreenWidth;
         var maxRatio:Number = 1.3;
         return lowestWidth * Math.min(ratio,maxRatio);
      }
      
      public static function get moveDeeper() : Boolean
      {
         return _deeper;
      }
      
      public static function showOn(what:Array, depth:int = 500, duration:Number = 0.2, delayAdd:Number = 0.05, delayInit:Number = 0, ease:Ease = null) : void
      {
         var i:uint = 0;
         ease = ease == null ? Expo.easeOut : ease;
         for(var delay:Number = delayInit; i < what.length; )
         {
            what[i].alpha = 0;
            what[i].z = depth;
            TweenMax.to(what[i],duration,{
               "alpha":1,
               "delay":delay,
               "ease":ease,
               "onComplete":null
            });
            delay += delayAdd;
            i++;
         }
      }
      
      public static function showOff(what:Array, onComplete:Function = null, depth:int = 300, duration:Number = 0.2, delayAdd:Number = 0.05, delayInit:Number = 0, ease:Ease = null) : void
      {
         var i:uint = 0;
         ease = ease == null ? Expo.easeIn : ease;
         var delay:Number = delayInit;
         what = what.reverse();
         for(var tweens:Array = new Array(); i < what.length; )
         {
            tweens.push(TweenMax.to(what[i],duration,{
               "alpha":0,
               "delay":delay,
               "ease":ease,
               "onComplete":null
            }));
            delay += delayAdd;
            i++;
         }
         var timeline:TimelineMax = new TimelineMax({
            "tweens":tweens,
            "align":"start",
            "onComplete":onComplete
         });
      }
      
      public function getScreen(id:String) : Screen
      {
         return this.items[id] as Screen;
      }
      
      public function get currentScreen() : Screen
      {
         return this._currentScreen;
      }
      
      protected function onUserParams(arg1:ApiEvent) : void
      {
         Base.navigator.currentLogin = arg1.data.answer.login;
         Base.navigator.header.accountName = arg1.data.answer.login;
      }
      
      protected function onAuthFail(event:Event) : void
      {
         this.crumbs.visible = false;
         this.is_auth = false;
      }
      
      protected function onAuthSuccess(event:Event) : void
      {
         this.crumbs.visible = true;
         this.is_auth = true;
      }
      
      protected function onAddedToStage(event:Event) : void
      {
         Logger.LogToChannel(Logger.DEBUG,this,"onAddedToStage");
         this.removeEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         this.addEventListener(Event.REMOVED_FROM_STAGE,this.onRemovedFromStage);
         Base.stage.addEventListener(Base.STAGE_RESIZE,this.onResize);
         this.onResize();
         this.initKeyboard();
      }
      
      protected function onResize(event:Event = null) : void
      {
         this.header.y = this.header.x = 0;
         this.footer.x = 0;
         this.footer.y = Base.stage.stageHeight - FOOTER_HEIGHT;
         this.leftSide.y = 0;
         this.leftSide.x = 0;
         this.rightSide.y = 0;
         this.rightSide.x = Base.stage.stageWidth;
         setTimeout(this.drawBackground,0);
         setTimeout(this.drawBackground,50);
      }
      
      protected function drawBackground() : void
      {
         this.background.graphics.clear();
         this.background.graphics.drawRect(0,0,Base.stage.stageWidth * 1,Base.stage.stageHeight);
         this.background.graphics.endFill();
      }
      
      public function onPythonShowDialog(event:ApiEvent) : *
      {
         this.showDialog(event.data.answer.title,event.data.answer.text,false,[new DialogButtonItem("extendedGUI.Dialogs.Ok",null,1)],500,250);
      }
      
      public function onPythonShowShield(event:ApiEvent) : *
      {
         this.shield.show(0.9);
      }
      
      public function onPythonHideShield(event:ApiEvent) : *
      {
         this.shield.hide();
         if(this.currentScreen)
         {
            if(this.currentScreen.id == MainMenuGUI.ROOT_SCREEN || this.currentScreen.id == MainMenuGUI.NEW_CHAR_SCREEN)
            {
               Dummy.visible = true;
            }
            Base.stage.focus = this.currentScreen;
            TweenMax.to(this.currentScreen,0.3,{
               "alpha":1,
               "delay":0.1,
               "ease":Expo.easeInOut,
               "onComplete":this.fixMe3D
            });
         }
      }
      
      public function showToolTip(sender:Object, message:String) : *
      {
         if(this.toolTip == null)
         {
            this.addChild(this.toolTip = new ToolTip());
         }
         this.toolTip.showToolTip(sender,message);
      }
      
      public function tryToRemoveMe(obj:DialogWindow = null) : void
      {
         while(this.dialogs.numChildren > 0)
         {
            this.dialogs.removeChildAt(0);
         }
      }
      
      protected function onRemovedFromStage(event:Event) : void
      {
         this.removeEventListener(Event.REMOVED_FROM_STAGE,this.onRemovedFromStage);
         Base.stage.removeEventListener(Base.STAGE_RESIZE,this.onResize);
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
      }
      
      public function addScreen(screen:Screen) : void
      {
         this.items[screen.id] = screen;
      }
      
      public function showKeyboardMessage(obj:*, labelText:String) : void
      {
         this.keyboardMessage = new KeyboardMessage(labelText,obj);
         var pnt:Point = obj.localToGlobal(new Point(0,0));
         this.keyboardMessage.width = obj.width;
         this.keyboardMessage.height = obj.height;
         this.keyboardMessage.x = pnt.x;
         this.keyboardMessage.y = pnt.y;
         this.keyboardMessage.alpha = 0.5;
         this.keyboardMessageHolder.addChild(this.keyboardMessage);
         this.shield.show();
         TweenMax.to(this.keyboardMessage,0.5,{
            "alpha":1,
            "x":0,
            "y":HEADER_HEIGHT,
            "width":Base.stage.stageWidth - 0,
            "height":Base.stage.stageHeight - HEADER_HEIGHT - FOOTER_HEIGHT,
            "delay":0,
            "ease":Expo.easeInOut,
            "onComplete":null
         });
      }
      
      public function hideKeyboardMessage(obj:*) : void
      {
         (this.keyboardMessage as KeyboardMessage).clear();
         this.shield.hide();
         var pnt:Point = (this.keyboardMessage as KeyboardMessage).targetButton.localToGlobal(new Point(0,0));
         Logger.LogToChannel(Logger.DEBUG,this,"hideKeyboardMessage",pnt);
         TweenMax.to(this.keyboardMessage,0.2,{
            "alpha":0.5,
            "x":pnt.x,
            "y":pnt.y,
            "width":(this.keyboardMessage as KeyboardMessage).targetButton.width,
            "height":(this.keyboardMessage as KeyboardMessage).targetButton.height,
            "delay":0,
            "ease":Expo.easeInOut,
            "onComplete":this.killKeyboardMessage
         });
      }
      
      protected function killKeyboardMessage() : void
      {
         this.keyboardMessageHolder.removeChild(this.keyboardMessage);
         this.keyboardMessage = null;
      }
      
      public function killOldScreen() : void
      {
         Logger.LogToChannel(Logger.WARNING,"Navigator.killOldScreen",this.oldScreen);
         if(this.oldScreen)
         {
            setTimeout(this.leftSide.removeChild,0,this.oldScreen);
         }
      }
      
      internal function hideBackground() : *
      {
         Base.background.alpha = 1;
         this.is_lock = false;
         if(!this.is_auth || this.currentScreen.id == MainMenuGUI.CHARNAME_SCREEN)
         {
            return;
         }
         Base.background.visible = false;
      }
      
      public function showCreateCharScreen(old_screen:Screen, charName:String, updateChar:Boolean = false, donateUpdateChar:Boolean = false) : *
      {
         this.crumbs.visible = true;
         this.is_lock = true;
         TweenMax.killTweensOf(Base.background);
         TweenMax.to(Base.background,2.5,{
            "alpha":0.2,
            "ease":Expo.easeOut,
            "onComplete":this.hideBackground
         });
         var targetScreen:NewCharScreen2 = this.getScreen(MainMenuGUI.NEW_CHAR_SCREEN);
         this.focus.target = null;
         if(old_screen)
         {
            _deeper = !!old_screen ? targetScreen.depth > old_screen.depth : true;
            this.oldScreen = old_screen;
            if(_deeper)
            {
               old_screen.hideUp(this.killOldScreen);
            }
            else
            {
               old_screen.hideDown(this.killOldScreen);
            }
            if(this.currentScreen)
            {
               this.oldScreen = this.currentScreen;
               if(_deeper)
               {
                  this.oldScreen.hideUp(this.killOldScreen);
               }
               else
               {
                  this.oldScreen.hideDown(this.killOldScreen);
               }
            }
         }
         else
         {
            _deeper = true;
         }
         if(targetScreen)
         {
            this.leftSide.addChild(targetScreen);
            if(!updateChar)
            {
               targetScreen.setFirstChar(true);
            }
            if(donateUpdateChar)
            {
               targetScreen.setDonatChangeFace(donateUpdateChar);
            }
            else
            {
               targetScreen.setOldChar(updateChar);
            }
            targetScreen.charNameInput.text = charName;
            targetScreen.createButtonEnabled(true);
            this._currentScreen = targetScreen;
            if(donateUpdateChar)
            {
               setTimeout(targetScreen.resetButtonEnabled,600);
            }
            if(_deeper)
            {
               targetScreen.showUp();
            }
            else
            {
               targetScreen.showDown();
            }
            Base.navigator.header.account.visible = true;
         }
      }
      
      public function showSettingsScreenWhitoutLogin() : *
      {
         var targetScreen:NewSettingsScreen = this.getScreen(MainMenuGUI.BASE_SETTINGS);
         this.focus.target = null;
         _deeper = !!this.currentScreen ? targetScreen.depth > this.currentScreen.depth : true;
         if(this.currentScreen)
         {
            this.oldScreen = this.currentScreen;
            if(_deeper)
            {
               this.oldScreen.hideUp(this.killOldScreen);
            }
            else
            {
               this.oldScreen.hideDown(this.killOldScreen);
            }
         }
         if(targetScreen)
         {
            this.leftSide.addChild(targetScreen);
            this._currentScreen = targetScreen;
            if(_deeper)
            {
               targetScreen.showUp();
            }
            else
            {
               targetScreen.showDown();
            }
            this.leftSide.addChild(targetScreen);
         }
         this.crumbs.visible = true;
         this.header.optionBtn.visible = false;
         Api.call("on_open_settings");
      }
      
      public function showScreen(id:String, flagInGame:Boolean = false) : void
      {
         if(!this.is_auth || id == MainMenuGUI.CHARNAME_SCREEN)
         {
            Base.navigator.header.account.visible = false;
            Base.background.visible = true;
            this.crumbs.visible = id != MainMenuGUI.CHARNAME_SCREEN && id != MainMenuGUI.LOGIN_SCREEN;
            Base.navigator.footer.serverName = " ";
            if(!this.header.optionBtn.visible && id == MainMenuGUI.LOGIN_SCREEN)
            {
               Api.call("on_closed_settings");
            }
            this.header.optionBtn.visible = id == MainMenuGUI.LOGIN_SCREEN;
         }
         else
         {
            if(!Base.IN_GAME)
            {
               Base.navigator.header.account.visible = true;
            }
            this.crumbs.visible = id != MainMenuGUI.DONATE_INFO_SCREEN;
            Base.background.visible = false;
            if(id == MainMenuGUI.DONATE_INFO_SCREEN)
            {
               Base.navigator.header.newsVisible = false;
               Base.background.visible = true;
               Base.navigator.contentShield.show(0.9);
            }
            Base.navigator.header.optionBtn.visible = false;
         }
         var targetScreen:Screen = this.getScreen(id);
         Logger.LogToChannel(Logger.DEBUG,"Navigator.showScreen");
         Logger.LogToChannel(Logger.DEBUG,"\tcurrentScreen:",!!this.currentScreen ? this.currentScreen.id : "[no screen]");
         Logger.LogToChannel(Logger.DEBUG,"\ttargetScreen:",id);
         TweenMax.to(this.leftSide,0.3,{
            "delay":0,
            "ease":Expo.easeInOut,
            "onComplete":this.fixMe3D
         });
         if(this.currentScreen != null && this.currentScreen.id == id)
         {
            Logger.LogToChannel(Logger.DEBUG,"Navigator.showScreen: same screen");
            this.currentScreen.showDown();
            return;
         }
         this.focus.target = null;
         _deeper = !!this.currentScreen ? targetScreen.depth > this.currentScreen.depth : true;
         if(this.currentScreen)
         {
            this.oldScreen = this.currentScreen;
            if(_deeper)
            {
               this.oldScreen.hideUp(this.killOldScreen);
            }
            else
            {
               this.oldScreen.hideDown(this.killOldScreen);
            }
         }
         if(!this.oldScreen)
         {
         }
         if(targetScreen)
         {
            if(id == MainMenuGUI.ROOT_SCREEN && Base.IN_GAME && !flagInGame)
            {
               Base.IN_GAME = false;
               Base.navigator.header.exitButton.visible = true;
               Base.navigator.header.account.visible = true;
               this.oldScreen.hideBackgroundInGame();
            }
            else if(flagInGame)
            {
               Base.IN_GAME = true;
               targetScreen.showBackgroundInGame();
            }
            this.leftSide.addChild(targetScreen);
            this._currentScreen = targetScreen;
            if(_deeper)
            {
               targetScreen.showUp();
            }
            else
            {
               targetScreen.showDown();
            }
         }
         Base.navigator.header.account.visible = id == MainMenuGUI.ROOT_SCREEN;
         Base.navigator.header.updateAccountPanelPosition();
         Base.navigator.header.account.invalidate();
      }
      
      public function showConfirmWindow(event:ApiEvent) : void
      {
         if(Dummy.visible)
         {
            Dummy.visible = false;
            Api.call(Api.HIDE_DUMMY);
         }
         if(this.currentScreen)
         {
            this.currentScreen.visible = false;
            TweenMax.to(this.currentScreen,1,{
               "alpha":0,
               "z":100,
               "delay":0,
               "ease":Expo.easeOut
            });
         }
         Logger.LogToChannel(Logger.DEBUG,"Navigator.showConfirmWindow","\n\tneedConfirmIDCount:",event.data.answer.needConfirmIDCount,"\n\tdelivered:",event.data.answer.delivered,"\n\tmsg:",event.data.answer.msg);
         this.darkShield.graphics.clear();
         this.darkShield.graphics.beginFill(2236962,0.4);
         this.darkShield.graphics.drawRect(0,0,Base.stage.stageWidth,Base.stage.stageHeight);
         this.darkShield.graphics.endFill();
         this.modalShield.show();
         var confirmWindow:ConfirmWindow = ConfirmWindow.getInstance(event.data.answer.needConfirmIDCount,event.data.answer.delivered,event.data.answer.msg,event.data.answer.time);
         this.header.visible = false;
         confirmWindow.setSize(700,500);
         confirmWindow.x = (Base.stage.stageWidth - confirmWindow.width) / 2;
         confirmWindow.y = (Navigator.ScreenHeight - confirmWindow.height) / 2 + HEADER_HEIGHT;
         this.modal.addChild(confirmWindow);
      }
      
      public function hideShields() : void
      {
         this.darkShield.graphics.clear();
         this.modalShield.hide();
      }
      
      public function hideConfirmWindow() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Navigator.hideConfirmWindow");
         if(ConfirmWindow.self != null)
         {
            ConfirmWindow.self.doClose();
         }
         this.hideShields();
         if(this.currentScreen)
         {
            this.currentScreen.enabled = true;
            this.currentScreen.visible = true;
            TweenMax.to(this.currentScreen,0.3,{
               "alpha":1,
               "z":0,
               "delay":0.1,
               "ease":Expo.easeInOut,
               "onComplete":this.fixMe3D
            });
         }
         this.header.visible = true;
         Dummy.visible = true;
         Api.call(Api.SHOW_DUMMY);
      }
      
      public function showDialog(dialogHeader:String, dialogMessage:String, localized:Boolean = false, buttons:Array = null, dialogWidth:uint = 400, dialogHeight:uint = 250, isHtml:Boolean = false, shieldOn:* = true, last_child:Boolean = false, timer:uint = 0, timerText:String = "") : DialogWindow
      {
         if(Dummy.visible)
         {
            Dummy.visible = false;
         }
         Base.stage.focus = null;
         if(shieldOn)
         {
            this.shield.show(0.9);
            if(this.currentScreen)
            {
               this.currentScreen.enabled = false;
            }
         }
         var dialog:DialogWindow = new DialogWindow(buttons,isHtml);
         dialog.alertHeader(dialogHeader,localized);
         dialog.alertMessage(dialogMessage,localized);
         dialog.alertTimerMessage(timerText,timer,localized);
         dialog.addEventListener(Event.CLOSE,this.onDialogClose);
         dialog.addEventListener(TextEvent.LINK,this.onDialogLink);
         dialog.setSize(dialogWidth,dialogHeight);
         dialog.x = (Base.stage.stageWidth - dialog.width) / 2;
         dialog.y = (Base.stage.stageHeight - dialog.height) / 2;
         if(last_child)
         {
            this.dialogs.addChildAt(dialog,0);
         }
         else
         {
            this.dialogs.addChild(dialog);
         }
         Logger.LogToChannel(Logger.DEBUG,this.currentScreen,this.currentScreen.x,this.currentScreen.y,this.currentScreen.z,this.currentScreen.transform.matrix3D);
         if(Boolean(this.currentScreen) && shieldOn)
         {
            TweenMax.to(this.currentScreen,1,{
               "alpha":0.5,
               "delay":0,
               "ease":Expo.easeOut
            });
         }
         return dialog;
      }
      
      public function showDonateTestDialog() : *
      {
         if(!this.is_auth)
         {
            return;
         }
         this.showScreen(MainMenuGUI.DONATE_INFO_SCREEN);
      }
      
      public function showNewsDialog() : DialogWindow
      {
         if(Dummy.visible)
         {
            Dummy.visible = false;
         }
         Base.stage.focus = null;
         this.shield.show(0.9);
         if(this.currentScreen)
         {
            this.currentScreen.enabled = false;
         }
         Api.self.addEventListener(Api.CLOSE_NEWS,this.onNewsClose);
         Api.call(Api.SHOW_NEWS);
      }
      
      protected function onNewsClose(event:ApiEvent) : *
      {
         Api.self.removeEventListener(Api.CLOSE_NEWS,this.onNewsClose);
         this.onDialogClose(null);
      }
      
      public function update_setting(setting_name:String, setting_type:String) : *
      {
         if(!this.currentScreen)
         {
            return;
         }
         if(this.currentScreen.id == MainMenuGUI.SETTINGS_TUNE)
         {
            this.currentScreen.reconstruct();
         }
      }
      
      protected function onDialogClose(event:Event) : void
      {
         setTimeout(this.resetStageFocus,100);
         this.shield.hide();
         if(this.currentScreen)
         {
            if(this.currentScreen.id == MainMenuGUI.ROOT_SCREEN || this.currentScreen.id == MainMenuGUI.NEW_CHAR_SCREEN)
            {
               Dummy.visible = true;
            }
            this.currentScreen.enabled = true;
            TweenMax.to(this.currentScreen,0.3,{
               "alpha":1,
               "delay":0.1,
               "ease":Expo.easeInOut,
               "onComplete":this.fixMe3D
            });
         }
      }
      
      protected function onDialogLink(event:TextEvent) : void
      {
         Api.call(Api.OPEN_URL,[{"url":event.text}]);
      }
      
      protected function fixMe3D() : void
      {
         if(this.currentScreen.use3D)
         {
         }
      }
      
      protected function resetStageFocus() : void
      {
      }
      
      protected function initKeyboard() : void
      {
         Base.stage.removeEventListener(KeyboardEvent.KEY_DOWN,this.onKeyDown);
         Base.stage.addEventListener(KeyboardEvent.KEY_DOWN,this.onKeyDown);
         Base.stage.addEventListener(FocusEvent.KEY_FOCUS_CHANGE,this.onKeyboardFocusChange);
         Base.stage.addEventListener(FocusEvent.MOUSE_FOCUS_CHANGE,this.onMouseFocusChange);
         Base.stage.addEventListener(FocusEvent.FOCUS_IN,this.onFocusIn,true);
         Base.stage.addEventListener(FocusEvent.FOCUS_OUT,this.onFocusOut,true);
      }
      
      protected function onFocusOut(event:FocusEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,this,"onFocusOut",event.target);
      }
      
      protected function onFocusIn(event:FocusEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,this,"onFocusIn",event.target);
      }
      
      protected function onMouseFocusChange(event:FocusEvent) : void
      {
         this.focus.target = null;
      }
      
      protected function onKeyboardFocusChange(event:FocusEvent) : void
      {
         this.focus.target = event.relatedObject;
      }
      
      protected function onKeyDown(event:KeyboardEvent) : void
      {
         if(Base.stage.focus is TextField)
         {
            return;
         }
         if(Base.stage.focus is DialogWindow)
         {
            Logger.LogToChannel(Logger.DEBUG,this,"onKeyDown blocked by DialogWindow");
            return;
         }
         switch(event.keyCode)
         {
            case Keyboard.ESCAPE:
            case Keyboard.BACKSPACE:
               if(this.shield.visible)
               {
                  return;
               }
               if(this.is_lock)
               {
                  return;
               }
               if(!this.dialogs.numChildren && !this.modal.numChildren)
               {
                  Logger.LogToChannel(Logger.DEBUG,"Naigator say goBack");
                  this.currentScreen.goBack();
               }
               break;
         }
      }
   }
}

