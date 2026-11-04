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
      
      public function DialogWindow(param1:Array, param2:Boolean = false)
      {
         this.isHtml = param2;
         this.buttons = param1;
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
      
      protected function initKeyboardShortcuts(... rest) : void
      {
         Base.stage.addEventListener(KeyboardEvent.KEY_DOWN,this.onKeyDown);
      }
      
      protected function destroyKeyboardShortcuts(... rest) : void
      {
         Base.stage.removeEventListener(KeyboardEvent.KEY_DOWN,this.onKeyDown);
      }
      
      protected function onKeyDown(param1:KeyboardEvent) : void
      {
         var _loc2_:DialogButtonItem = null;
         param1.stopPropagation();
         for each(_loc2_ in this.buttons)
         {
            Logger.LogToChannel(Logger.DEBUG,this,"onKeyDown",_loc2_.shortCut,param1.keyCode,param1.keyCode == _loc2_.shortCut,_loc2_.button != null,this.helper[_loc2_.button]);
            if(_loc2_.shortCut == param1.keyCode && _loc2_.button != null)
            {
               if(this.helper[_loc2_.button] != null)
               {
                  dispatchEvent(new Event(Event.CLOSE));
                  this.helper[_loc2_.button]();
                  this.close();
               }
               break;
            }
            if(param1.keyCode == Keyboard.ESCAPE)
            {
               this.doClose();
            }
            if(param1.keyCode == Keyboard.ENTER && this.buttons.length == 1)
            {
               this.doClose();
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
      
      protected function bodyDeals() : void
      {
         var _loc1_:DialogButtonItem = null;
         var _loc2_:PushButton = null;
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
         for each(_loc1_ in this.buttons)
         {
            _loc2_ = new PushButton();
            _loc2_.focusMarginX = 0;
            _loc2_.focusMarginY = 0;
            this.allButtons.push(_loc2_);
            _loc1_.button = _loc2_;
            this.helper[_loc2_] = _loc1_.callback;
            _loc2_.addEventListener(MouseEvent.CLICK,this.onButtonClickHandler);
            _loc2_.font = Base.FONT_BOLD;
            _loc2_.size = 22;
            _loc2_.$ = _loc1_.label;
            _loc2_.overColorAlpha = 1;
            this.buttonsBox.addChild(_loc2_);
         }
         this.buttonsBox.draw();
      }
      
      protected function onButtonClickHandler(param1:MouseEvent) : void
      {
         var _loc2_:DialogButtonItem = null;
         for each(_loc2_ in this.buttons)
         {
            _loc2_.button.enabled = false;
         }
         Logger.LogToChannel(Logger.DEBUG,"onButtonClickHandler",param1.target,this.helper[param1.target]);
         dispatchEvent(new Event(Event.CLOSE));
         if(this.helper[param1.target] != null)
         {
            this.helper[param1.target]();
         }
         this.close();
      }
      
      override protected function showOn() : void
      {
         var _loc1_:TimelineMax = new TimelineMax();
         _loc1_.staggerFromTo([this,_titleBar,_titleLabel,_panel,this.messageHolder].concat(this.allButtons),0.3,{
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
         var _loc1_:TimelineMax = new TimelineMax();
         _loc1_.staggerFromTo([this,_titleBar,_titleLabel,_panel,this.messageHolder].concat(this.allButtons).reverse(),0.3,{
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
      
      public function set alertHeaderId(param1:String) : void
      {
         _titleLabel.$ = param1;
         invalidate();
      }
      
      public function set alertHeader(param1:String) : void
      {
         _titleLabel.text = param1;
      }
      
      public function set alertMessageId(param1:String) : void
      {
         if(this.isHtml)
         {
            (this.message as HtmlTextArea).text = Locale.getById(param1);
         }
         else
         {
            (this.message as TextArea).text = Locale.getById(param1);
         }
         invalidate();
      }
      
      public function set alertMessage(param1:String) : void
      {
         if(this.isHtml)
         {
            (this.message as HtmlTextArea).text = param1;
         }
         else
         {
            (this.message as TextArea).text = param1;
         }
         invalidate();
      }
      
      public function get okFunction() : Function
      {
         return this._okFunction;
      }
      
      public function set okFunction(param1:Function) : void
      {
         this._okFunction = param1;
      }
      
      public function get buttons() : Array
      {
         return this._buttons;
      }
      
      public function set buttons(param1:Array) : void
      {
         this._buttons = param1;
         invalidate();
      }
      
      override public function draw() : void
      {
         var _loc1_:DialogButtonItem = null;
         super.draw();
         for each(_loc1_ in this.buttons)
         {
            _loc1_.button.setSize((width - 0) * _loc1_.widthPercent,this.buttonsBoxHeight);
         }
         this.message.x = sideMargin;
         this.message.y = topMargin;
         this.message.setSize(width - sideMargin,height - this.buttonsBoxHeight - topMargin - bottomMargin);
         this.message.draw();
         this.buttonsBox.setSize(width,this.buttonsBoxHeight);
         this.buttonsBox.x = 0;
         this.buttonsBox.y = height - this.buttonsBoxHeight;
      }
   }
}

