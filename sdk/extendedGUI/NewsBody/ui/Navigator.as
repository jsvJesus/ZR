package ui
{
   import com.dvalimona.components.DialogWindow;
   import com.greensock.TimelineMax;
   import com.greensock.TweenMax;
   import com.greensock.easing.*;
   import communication.Auth;
   import events.ApiEvent;
   import flash.display.BitmapData;
   import flash.display.Shape;
   import flash.display.Sprite;
   import flash.events.Event;
   import flash.events.FocusEvent;
   import flash.events.KeyboardEvent;
   import flash.geom.Point;
   import flash.text.TextField;
   import flash.ui.Keyboard;
   import flash.utils.Dictionary;
   import flash.utils.setTimeout;
   import logging.Logger;
   import ui.components.BreadCrumbsLabel;
   import ui.components.ConfirmWindow;
   import ui.components.Focus;
   import ui.components.Shield;
   
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
      
      public var items:Object = new Object();
      
      public var alerts_helper:Dictionary = new Dictionary();
      
      private var _currentScreen:Screen;
      
      public var content:Sprite;
      
      public var leftSide:Sprite;
      
      public var rightSide:Sprite;
      
      public var header:Header;
      
      public var footer:Footer;
      
      public var shield:Shield;
      
      public var modalShield:Shield;
      
      public var dialogs:Sprite;
      
      public var modal:Sprite;
      
      public var keyboardMessageHolder:Sprite;
      
      public var keyboardMessage:KeyboardMessage;
      
      public var focusHolder:Sprite;
      
      public var focus:Focus;
      
      private var backgroundBitmap:BitmapData = new noised_half_black_png() as BitmapData;
      
      private var background:Shape = new Shape();
      
      private var crumbs:BreadCrumbsLabel;
      
      private var crumbsHolder:Sprite;
      
      public var oldScreen:Screen;
      
      public function Navigator(showHeader:Boolean = true, showFooter:Boolean = true, showBackground:Boolean = true)
      {
         FManager = new FocusManager();
         this.background.alpha = 0.1;
         if(showBackground)
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
      
      protected function onAuthFail(event:Event) : void
      {
         this.crumbs.visible = false;
      }
      
      protected function onAuthSuccess(event:Event) : void
      {
         this.crumbs.visible = true;
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
         if(Boolean(this.backgroundBitmap))
         {
            this.background.graphics.clear();
            this.background.graphics.beginBitmapFill(this.backgroundBitmap,null,true,false);
            this.background.graphics.drawRect(0,0,Base.stage.stageWidth * 1,Base.stage.stageHeight);
            this.background.graphics.endFill();
         }
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
      
      protected function killOldScreen() : void
      {
         Logger.LogToChannel(Logger.WARNING,"Navigator.killOldScreen",this.oldScreen);
         if(Boolean(this.oldScreen))
         {
            setTimeout(this.leftSide.removeChild,0,this.oldScreen);
         }
      }
      
      public function showScreen(id:String) : void
      {
         var targetScreen:Screen = this.getScreen(id);
         Logger.LogToChannel(Logger.DEBUG,"Navigator.showScreen");
         Logger.LogToChannel(Logger.DEBUG,"\tcurrentScreen:",Boolean(this.currentScreen) ? this.currentScreen.id : "[no screen]");
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
         _deeper = Boolean(this.currentScreen) ? targetScreen.depth > this.currentScreen.depth : true;
         if(Boolean(this.currentScreen))
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
         if(Boolean(targetScreen))
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
            setTimeout(this.resetStageFocus,500);
         }
      }
      
      public function showConfirmWindow(event:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Navigator.showConfirmWindow","\n\tneedConfirmIDCount:",event.data.answer.needConfirmIDCount,"\n\tdelivered:",event.data.answer.delivered,"\n\tmsg:",event.data.answer.msg);
         this.modalShield.show();
         var confirmWindow:ConfirmWindow = ConfirmWindow.getInstance(event.data.answer.needConfirmIDCount,event.data.answer.delivered,event.data.answer.msg);
         confirmWindow.setSize(700,500);
         confirmWindow.x = (Base.stage.stageWidth - confirmWindow.width) / 2;
         confirmWindow.y = (Navigator.ScreenHeight - confirmWindow.height) / 2 + HEADER_HEIGHT;
         this.modal.addChild(confirmWindow);
         if(Boolean(this.currentScreen))
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
         setTimeout(this.resetStageFocus,300);
         this.modalShield.hide();
         if(Boolean(this.currentScreen))
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
      }
      
      public function showDialog(dialogHeader:String, dialogMessage:String, localized:Boolean = false, buttons:Array = null, dialogWidth:uint = 400, dialogHeight:uint = 250, isHtml:Boolean = false) : DialogWindow
      {
         Base.stage.focus = null;
         this.shield.show();
         if(Boolean(this.currentScreen))
         {
            this.currentScreen.enabled = false;
         }
         var dialog:DialogWindow = new DialogWindow(buttons,isHtml);
         if(localized)
         {
            dialog.alertHeaderId = dialogHeader;
            dialog.alertMessageId = dialogMessage;
         }
         else
         {
            dialog.alertHeader = dialogHeader;
            dialog.alertMessage = dialogMessage;
         }
         dialog.addEventListener(Event.CLOSE,this.onDialogClose);
         dialog.setSize(dialogWidth,dialogHeight);
         dialog.x = (Base.stage.stageWidth - dialog.width) / 2;
         dialog.y = (Base.stage.stageHeight - dialog.height) / 2;
         this.dialogs.addChild(dialog);
         Logger.LogToChannel(Logger.DEBUG,this.currentScreen,this.currentScreen.x,this.currentScreen.y,this.currentScreen.z,this.currentScreen.transform.matrix3D);
         if(Boolean(this.currentScreen))
         {
            TweenMax.to(this.currentScreen,1,{
               "alpha":0.5,
               "delay":0,
               "ease":Expo.easeOut
            });
         }
         return dialog;
      }
      
      protected function onDialogClose(event:Event) : void
      {
         setTimeout(this.resetStageFocus,100);
         this.shield.hide();
         if(Boolean(this.currentScreen))
         {
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
         Base.stage.focus = this.currentScreen;
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
               Logger.LogToChannel(Logger.DEBUG,"Naigator say goBack");
               this.currentScreen.goBack();
         }
      }
   }
}

