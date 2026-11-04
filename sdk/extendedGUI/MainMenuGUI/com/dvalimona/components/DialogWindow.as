package com.dvalimona.components
{
   import com.greensock.*;
   import com.greensock.easing.*;
   import flash.display.*;
   import flash.events.*;
   import flash.ui.*;
   import flash.utils.*;
   import lang.*;
   import logging.*;
   
   public class DialogWindow extends Window
   {
      public static const OK:String = "ok";
      
      protected var messageHolder:Sprite;
      
      protected var message:Component;
      
      protected var message_timer:Component;
      
      protected var message_timer_text:String;
      
      protected var messageBox:VBox;
      
      protected var buttonsBox:HBox;
      
      protected var buttonsBoxHeight:uint = 60;
      
      protected var okButton:PushButton;
      
      protected var _timer:Timer;
      
      protected var _time_stop:Number = 0;
      
      protected var _time_duration:Number = 0;
      
      protected var helper:Dictionary;
      
      protected var allButtons:Array;
      
      protected var isHtml:Boolean;
      
      protected var instance:DialogWindow;
      
      protected var screenTabEnabled:Boolean = true;
      
      protected var screenTabChildren:Boolean = true;
      
      private var _okFunction:Function = null;
      
      protected var _buttons:Array;
      
      public function DialogWindow(buttons:Array, isHtml:Boolean = false)
      {
         this.isHtml = isHtml;
         this.buttons = buttons;
         this.addEventListener(Event.ADDED_TO_STAGE,this.initKeyboardShortcuts);
         this.addEventListener(Event.REMOVED_FROM_STAGE,this.destroyKeyboardShortcuts);
         super();
         this.instance = this;
         this.lockScreen();
      }
      
      private function lockScreen() : void
      {
         this.screenTabEnabled = Base.navigator.currentScreen.tabEnabled;
         this.screenTabChildren = Base.navigator.currentScreen.tabChildren;
         Base.navigator.currentScreen.tabEnabled = false;
         Base.navigator.currentScreen.tabChildren = false;
      }
      
      private function unlockScreen() : void
      {
         Base.navigator.currentScreen.tabEnabled = this.screenTabEnabled;
         Base.navigator.currentScreen.tabChildren = this.screenTabChildren;
      }
      
      protected function initKeyboardShortcuts(... args) : void
      {
         Base.stage.addEventListener(KeyboardEvent.KEY_DOWN,this.onKeyDown);
      }
      
      protected function destroyKeyboardShortcuts(... args) : void
      {
         Base.stage.removeEventListener(KeyboardEvent.KEY_DOWN,this.onKeyDown);
      }
      
      protected function onKeyDown(event:KeyboardEvent) : void
      {
         var btnData:DialogButtonItem = null;
         event.stopPropagation();
         for each(btnData in this.buttons)
         {
            Logger.LogToChannel(Logger.DEBUG,this,"onKeyDown",btnData.shortCuts,event.keyCode,btnData.shortCuts.indexOf(event.keyCode) != -1,btnData.button != null,this.helper[btnData.button]);
            if(btnData.shortCuts.indexOf(event.keyCode) != -1 && btnData.button != null)
            {
               if(this.helper[btnData.button] != null)
               {
                  dispatchEvent(new Event(Event.CLOSE));
                  this.helper[btnData.button]();
                  this.close();
               }
               break;
            }
         }
         if(event.keyCode == Keyboard.ESCAPE)
         {
            this.doClose();
         }
         if((event.keyCode == Keyboard.ENTER || event.keyCode == Keyboard.NUMPAD_ENTER) && this.buttons.length == 1)
         {
            this.doClose();
         }
      }
      
      override protected function addChildren() : void
      {
         super.addChildren();
         this.headerDeals();
         this.bodyDeals();
         Base.stage.focus = this;
      }
      
      protected function bodyDeals() : void
      {
         var btnData:DialogButtonItem = null;
         var button:PushButton = null;
         this.messageHolder = new Sprite();
         this.addChild(this.messageHolder);
         this.messageBox = new VBox(this.messageHolder);
         this.messageBox.padding = 10;
         this.messageBox.alignment = VBox.LEFT;
         this.messageBox.spacing = 1;
         this.messageBox.debug = false;
         this.messageBox.width = 500;
         this.messageHolder.addChild(this.messageBox);
         this.message = new TextArea();
         this.message.html = this.isHtml;
         (this.message as TextArea).autoHeight = false;
         (this.message as TextArea).autoHideScrollBar = true;
         (this.message as TextArea).editable = false;
         (this.message as TextArea).font = Base.lightFontName;
         (this.message as TextArea).size = 20;
         if(this.isHtml)
         {
            this.message.addEventListener(TextEvent.LINK,this.onLink);
         }
         this.message_timer = new TextArea();
         (this.message_timer as TextArea).autoHeight = false;
         (this.message_timer as TextArea).autoHideScrollBar = true;
         (this.message_timer as TextArea).editable = false;
         (this.message_timer as TextArea).font = Base.lightFontName;
         (this.message_timer as TextArea).size = 20;
         this.message_timer.visible = false;
         this.messageBox.addChild(this.message);
         this.messageBox.addChild(this.message_timer);
         this.buttonsBox = new HBox();
         this.buttonsBox.alignment = HBox.MIDDLE;
         this.buttonsBox.fixedHeight = this.buttonsBoxHeight;
         this.buttonsBox.fixedWidth = 400;
         this.buttonsBox.horizontalAlign = HBox.LEFT;
         this.buttonsBox.spacing = 1;
         this.buttonsBox.debug = false;
         this.addChild(this.buttonsBox);
         this.helper = new Dictionary();
         this.allButtons = new Array();
         for each(btnData in this.buttons)
         {
            button = new PushButton();
            button.focusMarginX = 0;
            button.focusMarginY = 0;
            this.allButtons.push(button);
            btnData.button = button;
            this.helper[button] = btnData.callback;
            button.addEventListener(MouseEvent.CLICK,this.onButtonClickHandler);
            button.font = Base.boldFontName;
            button.size = 22;
            button.$ = btnData.label;
            button.overColorAlpha = 1;
            this.buttonsBox.addChild(button);
         }
         this.buttonsBox.draw();
      }
      
      protected function onButtonClickHandler(event:MouseEvent) : void
      {
         var btnData:DialogButtonItem = null;
         for each(btnData in this.buttons)
         {
            btnData.button.enabled = false;
         }
         Logger.LogToChannel(Logger.DEBUG,"onButtonClickHandler",event.target,this.helper[event.target]);
         dispatchEvent(new Event(Event.CLOSE));
         if(this.helper[event.target] != null)
         {
            this.helper[event.target]();
         }
         this.close();
      }
      
      protected function onLink(event:TextEvent) : void
      {
      }
      
      override protected function showOn() : void
      {
         var tl:TimelineMax = new TimelineMax();
         tl.staggerFromTo([this,_titleBar,_titleLabel,_panel,this.messageHolder].concat(this.allButtons),0.3,{
            "alpha":0,
            "z":500,
            "ease":Expo.easeOut
         },{
            "alpha":1,
            "z":0,
            "ease":Expo.easeOut
         },0.05,"+=0",this.onAppeared);
      }
      
      protected function onAppeared() : void
      {
         Base.stage.focus = this;
         this.lockScreen();
      }
      
      public function doClose() : void
      {
         dispatchEvent(new Event(Event.CLOSE));
         this.close();
      }
      
      override protected function close() : void
      {
         this.destroyKeyboardShortcuts();
         if(this._timer != null)
         {
            this._timer.stop();
         }
         var tl:TimelineMax = new TimelineMax();
         tl.staggerFromTo([this,_titleBar,_titleLabel,_panel,this.messageHolder].concat(this.allButtons).reverse(),0.3,{
            "alpha":1,
            "z":0,
            "ease":Expo.easeOut
         },{
            "alpha":0,
            "z":500,
            "ease":Expo.easeOut
         },0.05,"+=0",this.onShowOffComplete);
      }
      
      private function onShowOffComplete() : void
      {
         Base.stage.focus = Base.navigator.currentScreen.defaultFocus;
         Logger.LogToChannel(Logger.DEBUG,"onShowOffComplete");
         this.unlockScreen();
         if(this.parent)
         {
            setTimeout(this.parent.removeChild,0,this);
         }
         setTimeout(this.killMePlease,10);
      }
      
      private function killMePlease() : void
      {
         this.instance = null;
      }
      
      private function headerDeals() : void
      {
         leftItems.shift = 3;
         _titleLabel.autoSize = true;
         _titleLabel.font = Base.boldFontName;
         _titleLabel.text = "Внимание";
         _titleLabel.size = 20;
         _titleLabel.mouseEnabled = false;
         _titleLabel.mouseChildren = false;
      }
      
      public function alertHeader(value:String, localised:Boolean = false) : void
      {
         if(localised)
         {
            _titleLabel.$ = value;
         }
         else
         {
            _titleLabel.text = value;
         }
         invalidate();
      }
      
      public function alertMessage(value:String, localised:Boolean = false) : void
      {
         var txt:String = null;
         if(localised)
         {
            txt = Locale.getById(value);
         }
         else
         {
            txt = value;
         }
         (this.message as TextArea).text = txt;
         invalidate();
      }
      
      public function alertTimerMessage(value:String, timer_value:uint, localised:Boolean = false) : void
      {
         if(timer_value < 1)
         {
            return;
         }
         if(this._timer == null)
         {
            this._timer = new Timer(1000);
            this._timer.addEventListener(TimerEvent.TIMER,this.updateTimerText);
         }
         this.message_timer.visible = true;
         if(localised)
         {
            this.message_timer_text = Locale.getById(value);
         }
         else
         {
            this.message_timer_text = value;
         }
         this.start_timer(timer_value);
      }
      
      private function updateTimerText() : *
      {
         var current_time:Number = NaN;
         var event:KeyboardEvent = null;
         var date:Date = new Date();
         current_time = this._time_stop - date.time / 1000;
         if(current_time <= 0)
         {
            this._timer.stop();
            current_time = 0;
            event = new KeyboardEvent(KeyboardEvent.KEY_DOWN,true,false,Keyboard.ESCAPE,Keyboard.ESCAPE);
            event.keyCode = Keyboard.ESCAPE;
            this.onKeyDown(event);
            return;
         }
         this.message_timer.text = this.message_timer_text + int(current_time).toString();
         this.messageBox.invalidate();
      }
      
      public function start_timer(time_stop:Number) : *
      {
         var date:Date = new Date();
         this._time_stop = date.time / 1000 + time_stop;
         this._time_duration = time_stop;
         if(this._time_stop >= 0)
         {
            this.updateTimerText();
            this._timer.start();
         }
      }
      
      public function get okFunction() : Function
      {
         return this._okFunction;
      }
      
      public function set okFunction(value:Function) : void
      {
         this._okFunction = value;
      }
      
      public function get buttons() : Array
      {
         return this._buttons;
      }
      
      public function set buttons(value:Array) : void
      {
         this._buttons = value;
         invalidate();
      }
      
      override public function draw() : void
      {
         var btnData:DialogButtonItem = null;
         var _height:int = 0;
         super.draw();
         for each(btnData in this.buttons)
         {
            btnData.button.setSize((width - 0) * btnData.widthPercent,this.buttonsBoxHeight);
         }
         _titleLabel.x = sideMargin;
         this.messageBox.x = sideMargin;
         this.messageBox.y = topMargin;
         this.messageBox.setSize(width - sideMargin,height - this.buttonsBoxHeight - topMargin - bottomMargin);
         _height = height - this.buttonsBoxHeight - topMargin - bottomMargin;
         if(this.message_timer.visible)
         {
            _height -= 30;
         }
         this.message.setSize(width - sideMargin,_height);
         this.message_timer.setSize(width - sideMargin,40);
         this.buttonsBox.setSize(width,this.buttonsBoxHeight);
         this.buttonsBox.x = 0;
         this.buttonsBox.y = height - this.buttonsBoxHeight;
      }
   }
}

