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
      
      public var items:Object;
      
      public var alerts_helper:Dictionary = new Dictionary();
      
      private var _currentScreen:Screen;
      
      public var currentServer:String;
      
      public var content:Sprite;
      
      public var leftSide:Sprite;
      
      public var rightSide:Sprite;
      
      public var header:Header;
      
      public var footer:Footer;
      
      public var shield:Shield;
      
      public var premiumShield:Shield;
      
      public var modalShield:Shield;
      
      public var dialogs:Sprite;
      
      public var premiumDialogs:Sprite;
      
      public var modal:Sprite;
      
      public var keyboardMessageHolder:Sprite;
      
      public var keyboardMessage:KeyboardMessage;
      
      public var focusHolder:Sprite;
      
      public var focus:Focus;
      
      private var backgroundBitmap:BitmapData;
      
      private var background:Shape;
      
      private var backgroundPicture:Background;
      
      private var crumbs:BreadCrumbsLabel;
      
      private var crumbsHolder:Sprite;
      
      public var oldScreen:Screen;
      
      public function Navigator(param1:Boolean = true, param2:Boolean = true, param3:Boolean = true)
      {
         FManager = new FocusManager();
         this.items = new Object();
         this.backgroundBitmap = new noised_half_black_png() as BitmapData;
         this.background = new Shape();
         this.background.alpha = 0.1;
         if(param3)
         {
            this.addChild(this.background);
         }
         this.addChild(this.content = new Sprite());
         this.leftSide = new Sprite();
         this.leftSide.addChild(this.crumbsHolder = new Sprite());
         this.crumbs = new BreadCrumbsLabel(this.crumbsHolder);
         this.crumbs.x = 50;
         this.crumbs.y = 30;
         this.rightSide = new Sprite();
         this.header = new Header();
         if(param1)
         {
            this.content.addChild(this.header);
         }
         this.footer = new Footer();
         if(param2)
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
         super();
      }
      
      public static function get ScreenHeight() : Number
      {
         return Base.stage.stageHeight - HEADER_HEIGHT - FOOTER_HEIGHT;
      }
      
      public static function get LeftWidth() : uint
      {
         var _loc1_:uint = 400;
         var _loc2_:uint = 1024;
         var _loc3_:Number = Base.stage.stageWidth / _loc2_;
         var _loc4_:Number = 1.3;
         return _loc1_ * Math.min(_loc3_,_loc4_);
      }
      
      public static function get moveDeeper() : Boolean
      {
         return _deeper;
      }
      
      public static function showOn(param1:Array, param2:int = 500, param3:Number = 0.2, param4:Number = 0.05, param5:Number = 0, param6:Ease = null) : void
      {
         var _loc8_:uint = 0;
         param6 = param6 == null ? Expo.easeOut : param6;
         var _loc7_:Number = param5;
         while(_loc8_ < param1.length)
         {
            param1[_loc8_].alpha = 0;
            param1[_loc8_].z = param2;
            TweenMax.to(param1[_loc8_],param3,{
               "alpha":1,
               "delay":_loc7_,
               "ease":param6,
               "onComplete":null
            });
            _loc7_ += param4;
            _loc8_++;
         }
      }
      
      public static function showOff(param1:Array, param2:Function = null, param3:int = 300, param4:Number = 0.2, param5:Number = 0.05, param6:Number = 0, param7:Ease = null) : void
      {
         var _loc10_:uint = 0;
         param7 = param7 == null ? Expo.easeIn : param7;
         var _loc8_:Number = param6;
         param1 = param1.reverse();
         var _loc9_:Array = new Array();
         while(_loc10_ < param1.length)
         {
            _loc9_.push(TweenMax.to(param1[_loc10_],param4,{
               "alpha":0,
               "delay":_loc8_,
               "ease":param7,
               "onComplete":null
            }));
            _loc8_ += param5;
            _loc10_++;
         }
         var _loc11_:TimelineMax = new TimelineMax({
            "tweens":_loc9_,
            "align":"start",
            "onComplete":param2
         });
      }
      
      public function getScreen(param1:String) : Screen
      {
         return this.items[param1] as Screen;
      }
      
      public function get currentScreen() : Screen
      {
         return this._currentScreen;
      }
      
      protected function onAuthFail(param1:Event) : void
      {
         this.crumbs.visible = false;
      }
      
      protected function onAuthSuccess(param1:Event) : void
      {
         this.crumbs.visible = true;
      }
      
      protected function onAddedToStage(param1:Event) : void
      {
         Logger.LogToChannel(Logger.DEBUG,this,"onAddedToStage");
         this.removeEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         this.addEventListener(Event.REMOVED_FROM_STAGE,this.onRemovedFromStage);
         Base.stage.addEventListener(Base.STAGE_RESIZE,this.onResize);
         this.onResize();
         this.initKeyboard();
      }
      
      protected function onResize(param1:Event = null) : void
      {
         var _loc2_:* = undefined;
         this.header.y = this.header.x = 0;
         this.footer.x = 0;
         this.footer.y = Base.stage.stageHeight - FOOTER_HEIGHT;
         this.leftSide.y = 0;
         this.leftSide.x = 0;
         this.rightSide.y = 0;
         this.rightSide.x = Base.stage.stageWidth;
         for(_loc2_ in this.items)
         {
            trace(this.items[_loc2_]);
         }
         setTimeout(this.drawBackground,0);
         setTimeout(this.drawBackground,50);
      }
      
      protected function drawBackground() : void
      {
         if(this.backgroundBitmap)
         {
            this.background.graphics.clear();
            this.background.graphics.beginBitmapFill(this.backgroundBitmap,null,true,false);
            this.background.graphics.drawRect(0,0,Base.stage.stageWidth * 1,Base.stage.stageHeight);
            this.background.graphics.endFill();
         }
      }
      
      public function tryToRemoveMe(param1:DialogWindow = null) : void
      {
         while(this.dialogs.numChildren > 0)
         {
            this.dialogs.removeChildAt(0);
         }
      }
      
      protected function onRemovedFromStage(param1:Event) : void
      {
         this.removeEventListener(Event.REMOVED_FROM_STAGE,this.onRemovedFromStage);
         Base.stage.removeEventListener(Base.STAGE_RESIZE,this.onResize);
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
      }
      
      public function addScreen(param1:Screen) : void
      {
         this.items[param1.id] = param1;
      }
      
      public function showKeyboardMessage(param1:*, param2:String) : void
      {
         this.keyboardMessage = new KeyboardMessage(param2,param1);
         var _loc3_:Point = param1.localToGlobal(new Point(0,0));
         this.keyboardMessage.width = param1.width;
         this.keyboardMessage.height = param1.height;
         this.keyboardMessage.x = _loc3_.x;
         this.keyboardMessage.y = _loc3_.y;
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
      
      public function hideKeyboardMessage(param1:*) : void
      {
         (this.keyboardMessage as KeyboardMessage).clear();
         this.shield.hide();
         var _loc2_:Point = (this.keyboardMessage as KeyboardMessage).targetButton.localToGlobal(new Point(0,0));
         Logger.LogToChannel(Logger.DEBUG,this,"hideKeyboardMessage",_loc2_);
         TweenMax.to(this.keyboardMessage,0.2,{
            "alpha":0.5,
            "x":_loc2_.x,
            "y":_loc2_.y,
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
      
      protected function killOldScreen() : void
      {
         Logger.LogToChannel(Logger.WARNING,"Navigator.killOldScreen",this.oldScreen);
         if(this.oldScreen)
         {
            setTimeout(this.leftSide.removeChild,0,this.oldScreen);
         }
      }
      
      public function showScreen(param1:String, param2:Boolean = false) : void
      {
         if(param1 == MainMenuGUI.LOGIN_SCREEN)
         {
            Base.navigator.header.gold.visible = false;
            Base.background.visible = true;
            Base.navigator.footer.serverName = " ";
         }
         else
         {
            if(!Base.IN_GAME)
            {
               Base.navigator.header.gold.visible = true;
            }
            Base.background.visible = false;
         }
         var _loc3_:Screen = this.getScreen(param1);
         Logger.LogToChannel(Logger.DEBUG,"Navigator.showScreen");
         Logger.LogToChannel(Logger.DEBUG,"\tcurrentScreen:",!!this.currentScreen ? this.currentScreen.id : "[no screen]");
         Logger.LogToChannel(Logger.DEBUG,"\ttargetScreen:",param1);
         TweenMax.to(this.leftSide,0.3,{
            "delay":0,
            "ease":Expo.easeInOut,
            "onComplete":this.fixMe3D
         });
         if(this.currentScreen != null && this.currentScreen.id == param1)
         {
            Logger.LogToChannel(Logger.DEBUG,"Navigator.showScreen: same screen");
            this.currentScreen.showDown();
            return;
         }
         this.focus.target = null;
         _deeper = !!this.currentScreen ? _loc3_.depth > this.currentScreen.depth : true;
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
         if(_loc3_)
         {
            if(param1 == MainMenuGUI.ROOT_SCREEN && Base.IN_GAME)
            {
               Base.stage.removeChild(Base.stage.getChildAt(0));
               Base.IN_GAME = false;
               Base.navigator.header.exitButton.visible = true;
               Base.navigator.header.gold.visible = true;
               Api.call("closed_settings");
            }
            if(param2)
            {
               Base.IN_GAME = true;
               _loc3_.showBackgroundInGame();
            }
            this.leftSide.addChild(_loc3_);
            this._currentScreen = _loc3_;
            if(_deeper)
            {
               _loc3_.showUp();
            }
            else
            {
               _loc3_.showDown();
            }
         }
      }
      
      public function showConfirmWindow(param1:ApiEvent) : void
      {
         if(Dummy.visible)
         {
            Dummy.visible = false;
         }
         Logger.LogToChannel(Logger.DEBUG,"Navigator.showConfirmWindow","\n\tneedConfirmIDCount:",param1.data.answer.needConfirmIDCount,"\n\tdelivered:",param1.data.answer.delivered,"\n\tmsg:",param1.data.answer.msg);
         this.modalShield.show();
         var _loc2_:ConfirmWindow = ConfirmWindow.getInstance(param1.data.answer.needConfirmIDCount,param1.data.answer.delivered,param1.data.answer.msg);
         _loc2_.setSize(700,500);
         _loc2_.x = (Base.stage.stageWidth - _loc2_.width) / 2;
         _loc2_.y = (Navigator.ScreenHeight - _loc2_.height) / 2 + HEADER_HEIGHT;
         this.modal.addChild(_loc2_);
         if(this.currentScreen)
         {
            TweenMax.to(this.currentScreen,1,{
               "alpha":0.5,
               "z":100,
               "delay":0,
               "ease":Expo.easeOut
            });
         }
      }
      
      public function hideConfirmWindow() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Navigator.hideConfirmWindow");
         if(ConfirmWindow.self == null)
         {
            Logger.LogToChannel(Logger.DEBUG,"Navigator.hideConfirmWindow: no ConfirmWindow");
            return;
         }
         ConfirmWindow.self.doClose();
         this.modalShield.hide();
         if(this.currentScreen)
         {
            this.currentScreen.enabled = true;
            TweenMax.to(this.currentScreen,0.3,{
               "alpha":1,
               "z":0,
               "delay":0.1,
               "ease":Expo.easeInOut,
               "onComplete":this.fixMe3D
            });
         }
         Dummy.visible = true;
      }
      
      public function showDialog(param1:String, param2:String, param3:Boolean = false, param4:Array = null, param5:uint = 400, param6:uint = 250, param7:Boolean = false) : DialogWindow
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
         var _loc8_:DialogWindow = new DialogWindow(param4,param7);
         if(param3)
         {
            _loc8_.alertHeaderId = param1;
            _loc8_.alertMessageId = param2;
         }
         else
         {
            _loc8_.alertHeader = param1;
            _loc8_.alertMessage = param2;
         }
         _loc8_.addEventListener(Event.CLOSE,this.onDialogClose);
         _loc8_.setSize(param5,param6);
         _loc8_.x = (Base.stage.stageWidth - _loc8_.width) / 2;
         _loc8_.y = (Base.stage.stageHeight - _loc8_.height) / 2;
         this.dialogs.addChild(_loc8_);
         Logger.LogToChannel(Logger.DEBUG,this.currentScreen,this.currentScreen.x,this.currentScreen.y,this.currentScreen.z,this.currentScreen.transform.matrix3D);
         if(this.currentScreen)
         {
            TweenMax.to(this.currentScreen,1,{
               "alpha":0.5,
               "delay":0,
               "ease":Expo.easeOut
            });
         }
         return _loc8_;
      }
      
      public function showNewsDialog(param1:String, param2:String, param3:Boolean = false, param4:Array = null, param5:uint = 400, param6:uint = 250, param7:Boolean = false) : DialogWindow
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
         var _loc8_:NewsWindow = new NewsWindow(param4,param7);
         if(param3)
         {
            _loc8_.alertHeaderId = param1;
            _loc8_.alertMessageId = param2;
         }
         else
         {
            _loc8_.alertHeader = param1;
            _loc8_.alertMessage = param2;
         }
         _loc8_.addEventListener(Event.CLOSE,this.onDialogClose);
         _loc8_.setSize(param5,param6);
         _loc8_.x = (Base.stage.stageWidth - _loc8_.width) / 2;
         _loc8_.y = (Base.stage.stageHeight - _loc8_.height) / 2;
         this.dialogs.addChild(_loc8_);
         Logger.LogToChannel(Logger.DEBUG,this.currentScreen,this.currentScreen.x,this.currentScreen.y,this.currentScreen.z,this.currentScreen.transform.matrix3D);
         if(this.currentScreen)
         {
            TweenMax.to(this.currentScreen,1,{
               "alpha":0.5,
               "delay":0,
               "ease":Expo.easeOut
            });
         }
         return _loc8_;
      }
      
      protected function onDialogClose(param1:Event) : void
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
      
      protected function onFocusOut(param1:FocusEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,this,"onFocusOut",param1.target);
      }
      
      protected function onFocusIn(param1:FocusEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,this,"onFocusIn",param1.target);
      }
      
      protected function onMouseFocusChange(param1:FocusEvent) : void
      {
         this.focus.target = null;
      }
      
      protected function onKeyboardFocusChange(param1:FocusEvent) : void
      {
         this.focus.target = param1.relatedObject;
      }
      
      protected function onKeyDown(param1:KeyboardEvent) : void
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
         switch(param1.keyCode)
         {
            case Keyboard.ESCAPE:
            case Keyboard.BACKSPACE:
               if(!this.dialogs.numChildren && !this.modal.numChildren)
               {
                  Logger.LogToChannel(Logger.DEBUG,"Naigator say goBack");
                  this.currentScreen.goBack();
               }
         }
      }
   }
}

