package com.dvalimona.components
{
   import flash.display.DisplayObjectContainer;
   import flash.events.*;
   import flash.text.*;
   
   public class TextShadowed extends Text
   {
      protected var _shadow:TextField;
      
      protected var _shadowFormat:TextFormat;
      
      public function TextShadowed(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0, text:String = "")
      {
         super(parent,xpos,ypos,text);
      }
      
      override protected function addChildren() : void
      {
         _panel = new Panel(this);
         _panel.color = Style.TEXT_BACKGROUND;
         _panel.alpha = 0;
         this._shadowFormat = new TextFormat(font,_size > 0 ? _size : Style.LABEL_FONT_SIZE,0);
         _format = new TextFormat(font,_size > 0 ? _size : Style.LABEL_FONT_SIZE,Style.LABEL_TEXT);
         this._shadow = new TextField();
         this._shadow.x = 2;
         this._shadow.y = 2;
         this._shadow.height = _height;
         this._shadow.embedFonts = Style.embedFonts;
         this._shadow.multiline = true;
         this._shadow.wordWrap = true;
         this._shadow.selectable = true;
         this._shadow.mouseEnabled = false;
         this._shadow.type = TextFieldType.DYNAMIC;
         this._shadow.defaultTextFormat = this._shadowFormat;
         addChild(this._shadow);
         _tf = new TextField();
         _tf.x = 2;
         _tf.y = 2;
         _tf.height = _height;
         _tf.embedFonts = Style.embedFonts;
         _tf.multiline = true;
         _tf.wordWrap = true;
         _tf.selectable = true;
         _tf.type = TextFieldType.DYNAMIC;
         _tf.defaultTextFormat = _format;
         _tf.addEventListener(Event.CHANGE,this.onChange);
         addChild(_tf);
      }
      
      override public function draw() : void
      {
         super.draw();
         if(autoHeight)
         {
            _tf.x = 0;
            _tf.y = 0;
            _tf.width = _width - 4;
            _tf.height = 1000;
            _tf.autoSize = TextFieldAutoSize.LEFT;
            this._shadow.x = _tf.x - 1;
            this._shadow.y = _tf.y + 1;
            this._shadow.width = _tf.width;
            this._shadow.height = _tf.height;
            if(_html)
            {
               _tf.htmlText = _text;
               this._shadow.htmlText = _text;
            }
            else
            {
               _tf.text = _text;
               this._shadow.text = _text;
            }
            if(_editable)
            {
               _tf.mouseEnabled = true;
               _tf.selectable = true;
               _tf.type = TextFieldType.INPUT;
            }
            else
            {
               _tf.mouseEnabled = _selectable;
               _tf.selectable = _selectable;
               _tf.type = TextFieldType.DYNAMIC;
            }
            _tf.setTextFormat(_format);
            this._shadow.setTextFormat(this._shadowFormat);
            _panel.width = _width;
            _panel.height = _tf.textHeight;
            _panel.draw();
            _height = _tf.textHeight;
         }
         else
         {
            _tf.autoSize = TextFieldAutoSize.NONE;
            this._shadow.autoSize = _tf.autoSize;
            _panel.setSize(_width,_height);
            _panel.draw();
            _tf.x = 2;
            _tf.y = 2;
            _tf.width = _width - 4;
            _tf.height = _height - 4;
            this._shadow.x = _tf.x - 1;
            this._shadow.y = _tf.y + 1;
            this._shadow.width = _tf.width;
            this._shadow.height = _tf.height;
            if(_html)
            {
               _tf.htmlText = _text;
               this._shadow.htmlText = _text;
            }
            else
            {
               _tf.text = _text;
               this._shadow.text = _text;
            }
            if(_editable)
            {
               _tf.mouseEnabled = true;
               _tf.selectable = true;
               _tf.type = TextFieldType.INPUT;
            }
            else
            {
               _tf.mouseEnabled = _selectable;
               _tf.selectable = _selectable;
               _tf.type = TextFieldType.DYNAMIC;
            }
            _tf.setTextFormat(_format);
            this._shadow.setTextFormat(this._shadowFormat);
         }
         drawDebug();
      }
      
      override protected function onChange(event:Event) : void
      {
         _text = _tf.text;
         this._shadow.text = _tf.text;
         dispatchEvent(event);
      }
      
      override public function set size(sizeValue:uint) : void
      {
         _format.size = sizeValue;
         this._shadowFormat.size = sizeValue;
         invalidate();
      }
      
      override public function set leading(leadingValue:uint) : void
      {
         _format.leading = leadingValue;
         this._shadowFormat.leading = leadingValue;
         invalidate();
      }
      
      override public function set font(newFontName:String) : void
      {
         _font = newFontName;
         _format.font = font;
         this._shadowFormat.font = font;
         invalidate();
      }
   }
}

