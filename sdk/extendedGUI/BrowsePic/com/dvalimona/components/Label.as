package com.dvalimona.components
{
   import flash.display.DisplayObjectContainer;
   import flash.events.Event;
   import flash.text.TextField;
   import flash.text.TextFieldAutoSize;
   import flash.text.TextFormat;
   import flash.text.TextFormatAlign;
   
   [Event(name="resize",type="flash.events.Event")]
   public class Label extends Component
   {
      public static const LEFT:String = TextFormatAlign.LEFT;
      
      public static const RIGHT:String = TextFormatAlign.RIGHT;
      
      public static const JUSTIFY:String = TextFormatAlign.JUSTIFY;
      
      public static const CENTER:String = TextFormatAlign.CENTER;
      
      protected var _autoSize:Boolean = true;
      
      protected var _text:String = "";
      
      protected var _tf:TextField;
      
      protected var _size:Number = -1;
      
      private var _htmlText:String;
      
      protected var _font:String = Style.fontName;
      
      protected var _align:String = "left";
      
      public function Label(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0, text:String = "", size:Number = -1)
      {
         this.text = text;
         this._size = size;
         super(parent,xpos,ypos);
      }
      
      override public function updateLocale(localized:String) : void
      {
         this.text = localized;
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
      
      public function set color(colorValue:uint) : void
      {
         var format:TextFormat = this._tf.defaultTextFormat;
         format.color = colorValue;
         this._tf.defaultTextFormat = format;
         invalidate();
      }
      
      public function set size(sizeValue:uint) : void
      {
         var format:TextFormat = this._tf.defaultTextFormat;
         format.size = sizeValue;
         this._tf.defaultTextFormat = format;
         invalidate();
      }
      
      public function set underline(underlineValue:Boolean) : void
      {
         var format:TextFormat = this._tf.defaultTextFormat;
         format.underline = underlineValue;
         this._tf.defaultTextFormat = format;
         invalidate();
      }
      
      public function set text(t:String) : void
      {
         this._text = t;
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
      
      public function set htmlText(t:String) : void
      {
         this._htmlText = t;
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
      
      public function set autoSize(b:Boolean) : void
      {
         this._autoSize = b;
      }
      
      public function get autoSize() : Boolean
      {
         return this._autoSize;
      }
      
      public function get textField() : TextField
      {
         return this._tf;
      }
      
      public function set font(newFontName:String) : void
      {
         this._font = newFontName;
         var format:TextFormat = this._tf.defaultTextFormat;
         format.font = this.font;
         this._tf.defaultTextFormat = format;
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
      
      public function set align(value:String) : void
      {
         this._align = value;
         var format:TextFormat = this._tf.defaultTextFormat;
         format.align = this.align;
         this._tf.defaultTextFormat = format;
         invalidate();
      }
   }
}

