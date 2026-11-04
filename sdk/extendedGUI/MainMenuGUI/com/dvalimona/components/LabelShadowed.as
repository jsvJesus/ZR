package com.dvalimona.components
{
   import flash.display.DisplayObjectContainer;
   import flash.events.*;
   import flash.text.*;
   
   public class LabelShadowed extends Label
   {
      protected var _tfShadow:TextField;
      
      private var _shadowX:Number = -1;
      
      protected var _shadowSize:Number = 1;
      
      public function LabelShadowed(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0, text:String = "", size:Number = -1)
      {
         super(parent,xpos,ypos,text,size);
      }
      
      public function get textFieldShadow() : TextField
      {
         return this._tfShadow;
      }
      
      override protected function addChildren() : void
      {
         this._tfShadow = new TextField();
         this._tfShadow.height = _height;
         this._tfShadow.embedFonts = Style.embedFonts;
         this._tfShadow.selectable = false;
         this._tfShadow.mouseEnabled = false;
         this._tfShadow.border = false;
         this._tfShadow.borderColor = 16711935;
         this._tfShadow.defaultTextFormat = new TextFormat(font,_size > 0 ? _size : Style.LABEL_FONT_SIZE);
         this._tfShadow.text = _text;
         addChild(this._tfShadow);
         super.addChildren();
         this.align = LEFT;
         this.draw();
      }
      
      override public function draw() : void
      {
         if(!_tf || !this._tfShadow)
         {
            return;
         }
         _tf.text = _text;
         this._tfShadow.text = _text;
         if(_autoSize)
         {
            _tf.autoSize = TextFieldAutoSize.LEFT;
            this._tfShadow.autoSize = TextFieldAutoSize.LEFT;
            _width = _tf.width;
            dispatchEvent(new Event(Event.RESIZE));
         }
         else
         {
            _tf.autoSize = TextFieldAutoSize.NONE;
            _tf.width = _width;
            this._tfShadow.autoSize = TextFieldAutoSize.NONE;
            this._tfShadow.width = _width;
         }
         _height = _tf.textHeight + 5;
         _width = _text.length > 0 ? _width : 0;
         this._tfShadow.y = _tf.y + this._shadowSize;
         this._tfShadow.x = this._shadowX;
         drawDebug();
      }
      
      public function get shadowX() : Number
      {
         return this._shadowX;
      }
      
      public function set shadowX(value:Number) : void
      {
         this._shadowX = value;
         invalidate();
      }
      
      override public function set color(colorValue:uint) : void
      {
         var format:TextFormat = _tf.defaultTextFormat;
         format.color = colorValue;
         _tf.defaultTextFormat = format;
         invalidate();
      }
      
      public function set shadowColor(colorValue:uint) : void
      {
         var format:TextFormat = this._tfShadow.defaultTextFormat;
         format.color = colorValue;
         this._tfShadow.defaultTextFormat = format;
         invalidate();
      }
      
      public function set shadowAlpha(alphaValue:Number) : void
      {
         this._tfShadow.alpha = alphaValue;
         invalidate();
      }
      
      public function set shadowSize(sizeValue:Number) : void
      {
         this._shadowSize = sizeValue;
         invalidate();
      }
      
      public function get shadowSize() : Number
      {
         return this._shadowSize;
      }
      
      override public function set size(sizeValue:Number) : void
      {
         var format:TextFormat = _tf.defaultTextFormat;
         format.size = sizeValue;
         _tf.defaultTextFormat = format;
         format = this._tfShadow.defaultTextFormat;
         format.size = sizeValue;
         this._tfShadow.defaultTextFormat = format;
         invalidate();
      }
      
      override public function set underline(underlineValue:Boolean) : void
      {
         var format:TextFormat = _tf.defaultTextFormat;
         format.underline = underlineValue;
         _tf.defaultTextFormat = format;
         format = this._tfShadow.defaultTextFormat;
         format.underline = underlineValue;
         this._tfShadow.defaultTextFormat = format;
         invalidate();
      }
      
      override public function get textField() : TextField
      {
         return _tf;
      }
      
      override public function set font(newFontName:String) : void
      {
         _font = newFontName;
         var format:TextFormat = _tf.defaultTextFormat;
         format.font = font;
         _tf.defaultTextFormat = format;
         format = this._tfShadow.defaultTextFormat;
         format.font = font;
         this._tfShadow.defaultTextFormat = format;
         invalidate();
      }
      
      override public function set align(value:String) : void
      {
         _align = value;
         var format:TextFormat = _tf.defaultTextFormat;
         format.align = align;
         _tf.defaultTextFormat = format;
         format = this._tfShadow.defaultTextFormat;
         format.align = align;
         this._tfShadow.defaultTextFormat = format;
         invalidate();
      }
   }
}

