package ui
{
   import com.dvalimona.components.DialogWindow;
   import com.greensock.TimelineMax;
   import com.greensock.TweenMax;
   import com.greensock.easing.*;
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
   import ui.components.Focus;
   
   public class Navigator extends Sprite
   {
      public static const HEADER_HEIGHT:uint = 100;
      
      public static const FOOTER_HEIGHT:uint = 56;
      
      protected static const SWAP_SIZE:uint = 500;
      
      private static var _deeper:Boolean = false;
      
      public var items:Object = new Object();
      
      public var alerts_helper:Dictionary = new Dictionary();
      
      private var _currentScreen:Screen;
      
      public var screens:Sprite;
      
      public var header:Header;
      
      public var footer:Footer;
      
      public var shield:Sprite;
      
      public var dialogs:Sprite;
      
      public var modal:Sprite;
      
      public var keyboardMessageHolder:Sprite;
      
      public var keyboardMessage:KeyboardMessage;
      
      public var focusHolder:Sprite;
      
      public var focus:Focus;
      
      private var backgroundBitmap:BitmapData = new linegrid_png() as BitmapData;
      
      private var background:Shape = new Shape();
      
      public var oldScreen:Screen;
      
      public function Navigator()
      {
         this.addChild(this.background);
         this.screens = new Sprite();
         this.screens.y = HEADER_HEIGHT;
         this.addChild(this.header = new Header());
         this.addChild(this.footer = new Footer());
         this.addChild(this.screens);
         this.addChild(this.shield = new Sprite());
         this.addChild(this.keyboardMessageHolder = new Sprite());
         this.addChild(this.dialogs = new Sprite());
         this.addChild(this.modal = new Sprite());
         this.addChild(this.focusHolder = new Sprite());
         this.focusHolder.addChild(this.focus = new Focus());
         this.focusHolder.mouseEnabled = false;
         this.focusHolder.mouseChildren = false;
         this.focus.mouseEnabled = false;
         this.focus.mouseChildren = false;
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         this.initKeyboard();
         super();
      }
      
      public static function get ScreenHeight() : Number
      {
         return Base.self.stage.stageHeight - HEADER_HEIGHT - FOOTER_HEIGHT;
      }
      
      public static function get moveDeeper() : Boolean
      {
         return _deeper;
      }
      
      public static function showOn(what:Array, depth:int = 500, duration:Number = 0.2, delayAdd:Number = 0.05, delayInit:Number = 0) : void
      {
         var i:uint = 0;
         for(var delay:Number = delayInit; i < what.length; )
         {
            what[i].alpha = 0;
            what[i].z = depth;
            TweenMax.to(what[i],duration,{
               "alpha":1,
               "z":0,
               "delay":delay,
               "ease":Expo.easeOut,
               "onComplete":null
            });
            delay += delayAdd;
            i++;
         }
      }
      
      public static function showOff(what:Array, onComplete:Function = null, depth:int = 300, duration:Number = 0.2, delayAdd:Number = 0.05, delayInit:Number = 0) : void
      {
         var i:uint = 0;
         var delay:Number = delayInit;
         what = what.reverse();
         for(var tweens:Array = new Array(); i < what.length; )
         {
            tweens.push(TweenMax.to(what[i],duration,{
               "alpha":0,
               "z":depth,
               "delay":delay,
               "ease":Expo.easeIn,
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
      
      protected function onAddedToStage(event:Event) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"bububu");
         this.removeEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         this.addEventListener(Event.REMOVED_FROM_STAGE,this.onRemovedFromStage);
         Base.self.stage.addEventListener(Event.RESIZE,this.onResize);
         this.onResize();
      }
      
      protected function onResize(event:Event = null) : void
      {
         this.header.y = this.header.x = 0;
         this.footer.x = 0;
         this.footer.y = Base.self.stage.stageHeight - FOOTER_HEIGHT;
         this.screens.y = HEADER_HEIGHT;
         this.drawCover();
      }
      
      protected function drawCover() : void
      {
         if(Boolean(this.backgroundBitmap))
         {
            this.background.graphics.clear();
            this.background.graphics.beginBitmapFill(this.backgroundBitmap,null,true,false);
            this.background.graphics.drawRect(0,0,Base.self.stage.stageWidth * 1,Base.self.stage.stageHeight);
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
         Base.self.stage.removeEventListener(Event.RESIZE,this.onResize);
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
         this.shield.graphics.clear();
         this.shield.graphics.beginFill(0,0.5);
         this.shield.graphics.drawRect(0,0,Base.self.stage.stageWidth,Base.self.stage.stageHeight);
         this.shield.graphics.endFill();
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
         this.shield.graphics.clear();
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
         if(Boolean(this.oldScreen))
         {
            setTimeout(this.screens.removeChild,0,this.oldScreen);
         }
      }
      
      public function showScreen(id:String) : void
      {
         this.focus.target = null;
         var targetScreen:Screen = this.getScreen(id);
         _deeper = Boolean(this.currentScreen) ? targetScreen.depth > this.currentScreen.depth : true;
         if(Boolean(this.currentScreen))
         {
            this.oldScreen = this.currentScreen;
            TweenMax.to(this.currentScreen,0.7,{
               "alpha":0,
               "z":(_deeper ? -SWAP_SIZE : SWAP_SIZE),
               "delay":0,
               "ease":Expo.easeInOut,
               "onComplete":this.killOldScreen
            });
         }
         if(Boolean(targetScreen))
         {
            this._currentScreen = targetScreen;
            Base.stage.focus = this.currentScreen;
            targetScreen.alpha = 0;
            targetScreen.z = _deeper ? SWAP_SIZE : -SWAP_SIZE;
            this.screens.addChild(targetScreen);
            TweenMax.to(targetScreen,0.7,{
               "alpha":1,
               "z":0,
               "delay":0,
               "ease":Expo.easeInOut,
               "onComplete":this.fixMe3D
            });
         }
      }
      
      public function getScreen(id:String) : Screen
      {
         return this.items[id] as Screen;
      }
      
      public function get currentScreen() : Screen
      {
         return this._currentScreen;
      }
      
      public function showDialog(dialogHeader:String, dialogMessage:String, localized:Boolean = false, buttons:Array = null, dialogWidth:uint = 400, dialogHeight:uint = 250) : void
      {
         this.shield.graphics.clear();
         this.shield.graphics.beginFill(2236962,0.4);
         this.shield.graphics.drawRect(0,0,Base.self.stage.stageWidth,Base.self.stage.stageHeight);
         this.shield.graphics.endFill();
         this.currentScreen.enabled = false;
         var dialog:DialogWindow = new DialogWindow(buttons);
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
         dialog.x = (Base.self.stage.stageWidth - dialog.width) / 2;
         dialog.y = (Navigator.ScreenHeight - dialog.height) / 2 + HEADER_HEIGHT;
         this.dialogs.addChild(dialog);
         Logger.LogToChannel(Logger.DEBUG,this.currentScreen,this.currentScreen.x,this.currentScreen.y,this.currentScreen.z,this.currentScreen.transform.matrix3D);
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
      
      protected function onDialogClose(event:Event) : void
      {
         setTimeout(this.resetStageFocus,100);
         this.shield.graphics.clear();
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
      
      protected function fixMe3D() : void
      {
         this.currentScreen.scaleX = this.currentScreen.width / (this.currentScreen.width + 1);
         this.currentScreen.scaleY = this.currentScreen.height / (this.currentScreen.height + 1);
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
         Base.stage.addEventListener(FocusEvent.FOCUS_IN,this.onFocusIn);
         Base.stage.addEventListener(FocusEvent.FOCUS_OUT,this.onFocusOut);
      }
      
      protected function onFocusOut(event:FocusEvent) : void
      {
      }
      
      protected function onFocusIn(event:FocusEvent) : void
      {
      }
      
      protected function onMouseFocusChange(event:FocusEvent) : void
      {
         this.focus.target = null;
      }
      
      protected function onKeyboardFocusChange(event:FocusEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,this,"kb>",event.target,event.relatedObject);
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

