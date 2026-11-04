package com.dvalimona.components
{
   import communication.*;
   import flash.display.DisplayObjectContainer;
   import flash.events.*;
   import flash.text.*;
   import logging.*;
   
   public class HtmlText extends Component
   {
      protected var _tf:TextField;
      
      protected var _text:String = "";
      
      protected var _editable:Boolean = true;
      
      protected var _panel:Panel;
      
      protected var _selectable:Boolean = true;
      
      protected var _autoHeight:Boolean = true;
      
      public function HtmlText(param1:DisplayObjectContainer = null, param2:Number = 0, param3:Number = 0, param4:String = "")
      {
         this.text = param4;
         super(param1,param2,param3);
         setSize(200,100);
      }
      
      override public function updateLocale(param1:String) : void
      {
         this.text = param1;
         invalidate();
      }
      
      override protected function init() : void
      {
         super.init();
      }
      
      override protected function addChildren() : void
      {
         this._panel = new Panel(this);
         this._panel.color = Style.TEXT_BACKGROUND;
         this._panel.alpha = 0;
         this._tf = new TextField();
         this._tf.x = 2;
         this._tf.y = 2;
         this._tf.height = _height;
         this._tf.embedFonts = false;
         this._tf.multiline = true;
         this._tf.wordWrap = true;
         this._tf.addEventListener(Event.CHANGE,this.onChange);
         addChild(this._tf);
         this._tf.addEventListener(TextEvent.LINK,this.linkClicked);
      }
      
      public function linkClicked(param1:TextEvent) : void
      {
         var event:TextEvent = param1;
         Logger.LogToChannel(Logger.WARNING,event.text);
         try
         {
            Api.call(Api.OPEN_URL,[{"url":event.text}]);
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,event.text," : ",error.message);
         }
      }
      
      override public function draw() : void
      {
         super.draw();
         if(this.autoHeight)
         {
            this._tf.x = 0;
            this._tf.y = 0;
            this._tf.width = _width - 4;
            this._tf.height = 1000;
            this._tf.autoSize = TextFieldAutoSize.LEFT;
            this._tf.htmlText = this._text;
            this._tf.selectable = false;
            this._panel.width = _width;
            this._panel.height = this._tf.textHeight;
            this._panel.draw();
            _height = this._tf.textHeight;
         }
         else
         {
            this._tf.autoSize = TextFieldAutoSize.NONE;
            this._panel.setSize(_width,_height);
            this._panel.draw();
            this._tf.x = 2;
            this._tf.y = 2;
            this._tf.width = _width - 4;
            this._tf.height = _height - 4;
            this._tf.htmlText = this._text;
            this._tf.selectable = false;
            this._tf.type = TextFieldType.DYNAMIC;
         }
         drawDebug();
      }
      
      protected function onChange(param1:Event) : void
      {
         this._text = this._tf.text;
         dispatchEvent(param1);
      }
      
      public function set autoHeight(param1:Boolean) : void
      {
         this._autoHeight = param1;
         invalidate();
      }
      
      public function get autoHeight() : Boolean
      {
         return this._autoHeight;
      }
      
      public function set text(param1:String) : void
      {
         this._text = param1;
         if(this._text == null)
         {
            this._text = "";
         }
         invalidate();
      }
      
      public function get text() : String
      {
         return this._text;
      }
      
      public function get textField() : TextField
      {
         return this._tf;
      }
      
      public function set selectable(param1:Boolean) : void
      {
         this._selectable = param1;
         invalidate();
      }
      
      public function get selectable() : Boolean
      {
         return this._selectable;
      }
      
      override public function set enabled(param1:Boolean) : void
      {
         super.enabled = param1;
         this._tf.tabEnabled = param1;
      }
   }
}

