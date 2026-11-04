package com.dvalimona.components
{
   import com.greensock.TimelineMax;
   import com.greensock.easing.Expo;
   import flash.display.Sprite;
   import flash.events.Event;
   import flash.events.KeyboardEvent;
   import flash.events.MouseEvent;
   import flash.ui.Keyboard;
   import flash.utils.Dictionary;
   import flash.utils.setTimeout;
   import lang.Locale;
   import logging.Logger;
   
   public class DialogWindow extends Window
   {
      public static const OK:String = "ok";
      
      protected var messageHolder:Sprite;
      
      protected var message:Component;
      
      protected var buttonsBox:HBox;
      
      protected var buttonsBoxHeight:uint = 60;
      
      protected var okButton:PushButton;
      
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
         for each(btnData in this.buttons)
         {
            Logger.LogToChannel(Logger.DEBUG,this,"onKeyDown",btnData.shortCut,event.keyCode,event.keyCode == btnData.shortCut,btnData.button != null);
            if(btnData.shortCut == event.keyCode && btnData.button != null)
            {
               if(this.helper[btnData.button] != null)
               {
                  dispatchEvent(new Event(Event.CLOSE));
                  this.helper[btnData.button]();
                  this.close();
               }
               break;
            }
            if(event.keyCode == Keyboard.ESCAPE)
            {
            }
         }
      }
      
      override protected function addChildren() : void
      {
         super.addChildren();
         this.headerDeals();
         this.bodyDeals();
         Base.stage.focus = this;
      }
      
      private function bodyDeals() : void
      {
         var btnData:DialogButtonItem = null;
         var button:PushButton = null;
         this.messageHolder = new Sprite();
         this.addChild(this.messageHolder);
         if(this.isHtml)
         {
            this.message = new HtmlTextArea();
            (this.message as HtmlTextArea).autoHeight = false;
            (this.message as HtmlTextArea).autoHideScrollBar = true;
         }
         else
         {
            this.message = new TextArea();
            (this.message as TextArea).autoHeight = false;
            (this.message as TextArea).autoHideScrollBar = true;
            (this.message as TextArea).editable = false;
            (this.message as TextArea).font = Base.FONT_LIGHT;
            (this.message as TextArea).size = 20;
         }
         this.messageHolder.addChild(this.message);
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
            button.font = Base.FONT_BOLD;
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
         Logger.LogToChannel(Logger.DEBUG,"onShowOffComplete");
         this.unlockScreen();
         setTimeout(this.parent.removeChild,0,this);
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
         _titleLabel.font = Base.FONT_BOLD;
         _titleLabel.text = "Внимание";
         _titleLabel.size = 20;
         _titleLabel.mouseEnabled = false;
         _titleLabel.mouseChildren = false;
      }
      
      public function set alertHeaderId(value:String) : void
      {
         _titleLabel.$ = value;
         invalidate();
      }
      
      public function set alertHeader(value:String) : void
      {
         _titleLabel.text = value;
      }
      
      public function set alertMessageId(value:String) : void
      {
         if(this.isHtml)
         {
            (this.message as HtmlTextArea).text = Locale.getById(value);
         }
         else
         {
            (this.message as TextArea).text = Locale.getById(value);
         }
         invalidate();
      }
      
      public function set alertMessage(value:String) : void
      {
         if(this.isHtml)
         {
            (this.message as HtmlTextArea).text = value;
         }
         else
         {
            (this.message as TextArea).text = value;
         }
         invalidate();
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
         super.draw();
         for each(btnData in this.buttons)
         {
            btnData.button.setSize((width - 0) * btnData.widthPercent,this.buttonsBoxHeight);
         }
         this.message.x = sideMargin;
         this.message.y = sideMargin;
         this.message.setSize(width - sideMargin,height - this.buttonsBoxHeight - sideMargin - sideMargin);
         this.message.draw();
         this.buttonsBox.setSize(width,this.buttonsBoxHeight);
         this.buttonsBox.x = 0;
         this.buttonsBox.y = height - this.buttonsBoxHeight;
      }
   }
}

