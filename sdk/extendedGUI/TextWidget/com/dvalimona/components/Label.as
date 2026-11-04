package com.dvalimona.components
{
   import flash.display.DisplayObjectContainer;
   import flash.events.*;
   import flash.text.*;
   
   public class Label extends Component
   {
      public static const LEFT:String = TextFormatAlign.LEFT;
      
      public static const RIGHT:String = TextFormatAlign.RIGHT;
      
      public static const JUSTIFY:String = TextFormatAlign.JUSTIFY;
      
      public static const CENTER:String = TextFormatAlign.CENTER;
      
      protected var _autoSize:Boolean = true;
      
      protected var _text:String = "";
      
      protected var _tf:TextField;
      
      private var _color:uint;
      
      protected var _size:Number = -1;
      
      private var _htmlText:String;
      
      protected var _font:String = Style.fontName;
      
      protected var _align:String = "left";
      
      public function Label(param1:DisplayObjectContainer = null, param2:Number = 0, param3:Number = 0, param4:String = "", param5:Number = -1)
      {
         this.text = param4;
         this._size = param5;
         super(param1,param2,param3);
      }
      
      override public function updateLocale(param1:String) : void
      {
         this.text = param1;
         invalidate();
      }
      
      override protected function init() : void
      {
         super.init();
         mouseEnabled = false;
         mouseChildren = false;
      }
      
      override protected function addChildren() : void
      {
         this._tf = new TextField();
         this._tf.height = _height;
         this._tf.embedFonts = Style.embedFonts;
         this._tf.selectable = false;
         this._tf.mouseEnabled = false;
         this._tf.border = false;
         this._tf.borderColor = 16711935;
         this._tf.defaultTextFormat = new TextFormat(this.font,this._size > 0 ? this._size : Style.LABEL_FONT_SIZE,Style.LABEL_TEXT);
         this._tf.text = this._text;
         addChild(this._tf);
         this.align = LEFT;
         this.draw();
      }
      
      override public function draw() : void
      {
         super.draw();
         if(!this._tf)
         {
            return;
         }
         if(this._htmlText != null)
         {
            this._tf.htmlText = this._htmlText;
         }
         else
         {
            this._tf.text = this._text;
         }
         if(this._autoSize)
         {
            this._tf.autoSize = TextFieldAutoSize.LEFT;
            _width = this._tf.width;
            dispatchEvent(new Event(Event.RESIZE));
         }
         else
         {
            this._tf.autoSize = TextFieldAutoSize.NONE;
            this._tf.width = _width;
         }
         _height = this._tf.textHeight + 5;
         _width = this._text.length > 0 ? _width : 0;
         drawDebug();
      }
      
      public function get color() : uint
      {
         return this._color;
      }
      
      public function set color(param1:uint) : void
      {
         this._color = param1;
         var _loc2_:TextFormat = this._tf.defaultTextFormat;
         _loc2_.color = param1;
         this._tf.defaultTextFormat = _loc2_;
         invalidate();
      }
      
      public function get size() : Number
      {
         return this._size;
      }
      
      public function set size(param1:Number) : void
      {
         this._size = param1;
         var _loc2_:TextFormat = this._tf.defaultTextFormat;
         _loc2_.size = param1;
         this._tf.defaultTextFormat = _loc2_;
         invalidate();
      }
      
      public function set underline(param1:Boolean) : void
      {
         var _loc2_:TextFormat = this._tf.defaultTextFormat;
         _loc2_.underline = param1;
         this._tf.defaultTextFormat = _loc2_;
         invalidate();
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
      
      public function set htmlText(param1:String) : void
      {
         this._htmlText = param1;
         if(this._htmlText == null)
         {
            this.htmlText = "";
         }
         invalidate();
      }
      
      public function get htmlText() : String
      {
         return this._htmlText;
      }
      
      public function set autoSize(param1:Boolean) : void
      {
         this._autoSize = param1;
      }
      
      public function get autoSize() : Boolean
      {
         return this._autoSize;
      }
      
      public function get textField() : TextField
      {
         return this._tf;
      }
      
      public function set font(param1:String) : void
      {
         this._font = param1;
         var _loc2_:TextFormat = this._tf.defaultTextFormat;
         _loc2_.font = this.font;
         this._tf.defaultTextFormat = _loc2_;
         invalidate();
      }
      
      public function get font() : String
      {
         return this._font;
      }
      
      public function get align() : String
      {
         return this._align;
      }
      
      public function set align(param1:String) : void
      {
         this._align = param1;
         var _loc2_:TextFormat = this._tf.defaultTextFormat;
         _loc2_.align = this.align;
         this._tf.defaultTextFormat = _loc2_;
         invalidate();
      }
   }
}

