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
      
      public function LabelShadowed(param1:DisplayObjectContainer = null, param2:Number = 0, param3:Number = 0, param4:String = "", param5:Number = -1)
      {
         super(param1,param2,param3,param4,param5);
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
      
      public function set shadowX(param1:Number) : void
      {
         this._shadowX = param1;
         invalidate();
      }
      
      override public function set color(param1:uint) : void
      {
         var _loc2_:TextFormat = _tf.defaultTextFormat;
         _loc2_.color = param1;
         _tf.defaultTextFormat = _loc2_;
         invalidate();
      }
      
      public function set shadowColor(param1:uint) : void
      {
         var _loc2_:TextFormat = this._tfShadow.defaultTextFormat;
         _loc2_.color = param1;
         this._tfShadow.defaultTextFormat = _loc2_;
         invalidate();
      }
      
      public function set shadowAlpha(param1:Number) : void
      {
         this._tfShadow.alpha = param1;
         invalidate();
      }
      
      public function set shadowSize(param1:Number) : void
      {
         this._shadowSize = param1;
         invalidate();
      }
      
      public function get shadowSize() : Number
      {
         return this._shadowSize;
      }
      
      override public function set size(param1:Number) : void
      {
         var _loc2_:TextFormat = _tf.defaultTextFormat;
         _loc2_.size = param1;
         _tf.defaultTextFormat = _loc2_;
         _loc2_ = this._tfShadow.defaultTextFormat;
         _loc2_.size = param1;
         this._tfShadow.defaultTextFormat = _loc2_;
         invalidate();
      }
      
      override public function set underline(param1:Boolean) : void
      {
         var _loc2_:TextFormat = _tf.defaultTextFormat;
         _loc2_.underline = param1;
         _tf.defaultTextFormat = _loc2_;
         _loc2_ = this._tfShadow.defaultTextFormat;
         _loc2_.underline = param1;
         this._tfShadow.defaultTextFormat = _loc2_;
         invalidate();
      }
      
      override public function get textField() : TextField
      {
         return _tf;
      }
      
      override public function set font(param1:String) : void
      {
         _font = param1;
         var _loc2_:TextFormat = _tf.defaultTextFormat;
         _loc2_.font = font;
         _tf.defaultTextFormat = _loc2_;
         _loc2_ = this._tfShadow.defaultTextFormat;
         _loc2_.font = font;
         this._tfShadow.defaultTextFormat = _loc2_;
         invalidate();
      }
      
      override public function set align(param1:String) : void
      {
         _align = param1;
         var _loc2_:TextFormat = _tf.defaultTextFormat;
         _loc2_.align = align;
         _tf.defaultTextFormat = _loc2_;
         _loc2_ = this._tfShadow.defaultTextFormat;
         _loc2_.align = align;
         this._tfShadow.defaultTextFormat = _loc2_;
         invalidate();
      }
   }
}

