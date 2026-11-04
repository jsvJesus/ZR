package com.dvalimona.components
{
   import flash.display.Sprite;
   import flash.events.Event;
   import flash.events.KeyboardEvent;
   import flash.events.MouseEvent;
   import flash.ui.Keyboard;
   import flash.utils.Dictionary;
   import flash.utils.setTimeout;
   import lang.Locale;
   import logging.Logger;
   import ui.Navigator;
   
   public class DialogWindow extends Window
   {
      public static const OK:String = "ok";
      
      protected var messageHolder:Sprite;
      
      protected var message:TextArea;
      
      protected var buttonsBox:HBox;
      
      protected var buttonsBoxHeight:uint = 60;
      
      protected var okButton:PushButton;
      
      protected var helper:Dictionary;
      
      protected var allButtons:Array;
      
      private var _okFunction:Function = null;
      
      protected var _buttons:Array;
      
      public function DialogWindow(buttons:Array)
      {
         this.buttons = buttons;
         this.addEventListener(Event.ADDED_TO_STAGE,this.initKeyboardShortcuts);
         this.addEventListener(Event.REMOVED_FROM_STAGE,this.destroyKeyboardShortcuts);
         super();
      }
      
      protected function initKeyboardShortcuts(... args) : void
      {
         Base.stage.focus = this;
         Base.stage.addEventListener(KeyboardEvent.KEY_DOWN,this.onKeyDown);
      }
      
      protected function destroyKeyboardShortcuts(... args) : void
      {
         Base.stage.removeEventListener(KeyboardEvent.KEY_DOWN,this.onKeyDown);
      }
      
      protected function onKeyDown(event:KeyboardEvent) : void
      {
         var btnData:DialogButtonItem = null;
         Logger.LogToChannel(Logger.DEBUG,this,"onKeyDown",event.keyCode);
         for each(btnData in this.buttons)
         {
            Logger.LogToChannel(Logger.DEBUG,this,"onKeyDown",btnData.shortCut,event.keyCode,event.keyCode == btnData.shortCut);
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
               this.doClose();
            }
         }
      }
      
      override protected function showOn() : void
      {
         Navigator.showOn([this,_titleBar,_titleLabel,_panel,this.messageHolder].concat(this.allButtons));
      }
      
      override protected function addChildren() : void
      {
         super.addChildren();
         this.headerDeals();
         this.bodyDeals();
      }
      
      private function bodyDeals() : void
      {
         var btnData:DialogButtonItem = null;
         var button:PushButton = null;
         this.messageHolder = new Sprite();
         this.addChild(this.messageHolder);
         this.message = new TextArea();
         this.message.autoHideScrollBar = true;
         this.message.editable = false;
         this.message.font = Base.FONT_LIGHT;
         this.message.size = 20;
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
      
      protected function doClose() : void
      {
         dispatchEvent(new Event(Event.CLOSE));
         this.close();
      }
      
      override protected function close() : void
      {
         this.destroyKeyboardShortcuts();
         Navigator.showOff([this,_titleBar,_titleLabel,_panel,this.messageHolder].concat(this.allButtons),this.onShowOffComplete);
      }
      
      private function onShowOffComplete() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onShowOffComplete");
         setTimeout(this.parent.removeChild,0,this);
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
         this.message.text = Locale.getById(value);
         invalidate();
      }
      
      public function set alertMessage(value:String) : void
      {
         this.message.text = value;
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
         this.message.setSize(width - sideMargin,height - this.buttonsBoxHeight - sideMargin);
         this.buttonsBox.setSize(width,this.buttonsBoxHeight);
         this.buttonsBox.x = 0;
         this.buttonsBox.y = height - this.buttonsBoxHeight;
      }
   }
}

