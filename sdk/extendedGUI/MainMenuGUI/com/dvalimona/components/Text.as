package com.dvalimona.components
{
   import flash.display.DisplayObjectContainer;
   import flash.events.*;
   import flash.text.*;
   
   public class Text extends Component
   {
      protected var _tf:TextField;
      
      protected var _text:String = "";
      
      protected var _editable:Boolean = true;
      
      protected var _panel:Panel;
      
      protected var _selectable:Boolean = true;
      
      protected var _html:Boolean = false;
      
      protected var _format:TextFormat;
      
      protected var _autoHeight:Boolean = true;
      
      protected var _size:Number = -1;
      
      protected var _font:String = Base.fontName;
      
      public function Text(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0, text:String = "")
      {
         this.text = text;
         super(parent,xpos,ypos);
         setSize(200,100);
      }
      
      override public function updateLocale(localized:String) : void
      {
         this.text = localized;
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
         this._format = new TextFormat(this.font,this._size > 0 ? this._size : Style.LABEL_FONT_SIZE,Style.LABEL_TEXT);
         this._tf = new TextField();
         this._tf.x = 2;
         this._tf.y = 2;
         this._tf.height = _height;
         this._tf.embedFonts = Style.embedFonts;
         this._tf.multiline = true;
         this._tf.wordWrap = true;
         this._tf.selectable = true;
         this._tf.type = TextFieldType.INPUT;
         this._tf.defaultTextFormat = this._format;
         this._tf.addEventListener(Event.CHANGE,this.onChange);
         this._tf.addEventListener(TextEvent.LINK,this.onLink);
         addChild(this._tf);
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
            if(this._html)
            {
               this._tf.setTextFormat(this._format);
               this._tf.htmlText = this._text;
            }
            else
            {
               this._tf.text = this._text;
               this._tf.setTextFormat(this._format);
            }
            if(this._editable)
            {
               this._tf.mouseEnabled = true;
               this._tf.selectable = true;
               this._tf.type = TextFieldType.INPUT;
            }
            else
            {
               this._tf.mouseEnabled = this._selectable;
               this._tf.selectable = this._selectable;
               this._tf.type = TextFieldType.DYNAMIC;
            }
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
            if(this._html)
            {
               this._tf.setTextFormat(this._format);
               this._tf.htmlText = this._text;
            }
            else
            {
               this._tf.text = this._text;
               this._tf.setTextFormat(this._format);
            }
            if(this._editable)
            {
               this._tf.mouseEnabled = true;
               this._tf.selectable = true;
               this._tf.type = TextFieldType.INPUT;
            }
            else
            {
               this._tf.mouseEnabled = this._selectable;
               this._tf.selectable = this._selectable;
               this._tf.type = TextFieldType.DYNAMIC;
            }
         }
         drawDebug();
      }
      
      protected function onChange(event:Event) : void
      {
         this._text = this._tf.text;
         dispatchEvent(event);
      }
      
      protected function onLink(event:TextEvent) : void
      {
      }
      
      public function set autoHeight(b:Boolean) : void
      {
         this._autoHeight = b;
         invalidate();
      }
      
      public function get autoHeight() : Boolean
      {
         return this._autoHeight;
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
      
      public function get textField() : TextField
      {
         return this._tf;
      }
      
      public function set editable(b:Boolean) : void
      {
         this._editable = b;
         invalidate();
      }
      
      public function get editable() : Boolean
      {
         return this._editable;
      }
      
      public function set selectable(b:Boolean) : void
      {
         this._selectable = b;
         invalidate();
      }
      
      public function get selectable() : Boolean
      {
         return this._selectable;
      }
      
      public function set html(b:Boolean) : void
      {
         this._html = b;
         invalidate();
      }
      
      public function get html() : Boolean
      {
         return this._html;
      }
      
      override public function set enabled(value:Boolean) : void
      {
         super.enabled = value;
         this._tf.tabEnabled = value;
      }
      
      public function set size(sizeValue:uint) : void
      {
         this._format.size = sizeValue;
         invalidate();
      }
      
      public function set color(colorValue:uint) : void
      {
         this._format.color = colorValue;
         invalidate();
      }
      
      public function set leading(leadingValue:uint) : void
      {
         this._format.leading = leadingValue;
         invalidate();
      }
      
      public function set font(newFontName:String) : void
      {
         this._font = newFontName;
         this._format.font = this.font;
         invalidate();
      }
      
      public function get font() : String
      {
         return this._font;
      }
   }
}

