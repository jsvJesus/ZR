package com.dvalimona.components
{
   import com.dvalimona.utils.StringUtils;
   import communication.Api;
   import events.ApiEvent;
   import flash.display.DisplayObjectContainer;
   import flash.display.Sprite;
   import flash.events.Event;
   import flash.events.FocusEvent;
   import flash.events.MouseEvent;
   import flash.text.TextField;
   import flash.text.TextFieldType;
   import flash.text.TextFormat;
   import flash.utils.setTimeout;
   import logging.Logger;
   
   public class InputText extends Component
   {
      protected var _back:Sprite;
      
      protected var _password:Boolean = false;
      
      protected var _text:String = "";
      
      protected var _tf:TextField;
      
      protected var _focusIn:Boolean = false;
      
      private var _font:String = Style.fontName;
      
      private var _focusInColor:uint = Style.INPUT_COLOR_IN;
      
      private var _focusOutColor:uint = Style.INPUT_COLOR_OUT;
      
      private var _focusInAlpha:Number = Style.INPUT_COLOR_ALPHA_IN;
      
      private var _focusOutAlpha:Number = Style.INPUT_COLOR_ALPHA_OUT;
      
      private var _focusOutLabelColor:Number = Style.INPUT_LABEL_COLOR_OUT;
      
      private var _focusInLabelColor:Number = Style.INPUT_LABEL_COLOR_IN;
      
      private var _focusOutLabelAlpha:Number = Style.INPUT_LABEL_COLOR_ALPHA_OUT;
      
      private var _focusInLabelAlpha:Number = Style.INPUT_LABEL_COLOR_ALPHA_IN;
      
      public function InputText(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0, text:String = "", defaultHandler:Function = null)
      {
         this.text = text;
         super(parent,xpos,ypos);
         if(defaultHandler != null)
         {
            addEventListener(Event.CHANGE,defaultHandler);
         }
      }
      
      override protected function init() : void
      {
         super.init();
         setSize(100,16);
      }
      
      override protected function addChildren() : void
      {
         this._back = new Sprite();
         addChild(this._back);
         this._tf = new TextField();
         this._tf.embedFonts = Style.embedFonts;
         this._tf.selectable = true;
         this._tf.type = TextFieldType.INPUT;
         this._tf.multiline = false;
         this._tf.wordWrap = false;
         this._tf.defaultTextFormat = new TextFormat(Style.fontName,Style.fontSize,Style.INPUT_TEXT);
         addChild(this._tf);
         this._tf.addEventListener(Event.CHANGE,this.onChange);
         this._tf.addEventListener(FocusEvent.FOCUS_IN,this.onFocusIn);
         this._tf.addEventListener(FocusEvent.FOCUS_OUT,this.onFocusOut);
         addEventListener(MouseEvent.MOUSE_DOWN,this.onMouseDown);
      }
      
      protected function onMouseDown(event:MouseEvent) : void
      {
         Base.stage.focus = this._tf;
         event.stopImmediatePropagation();
      }
      
      protected function onFocusOut(event:FocusEvent) : void
      {
         this._focusIn = false;
         invalidate();
         this.removeClipboardListener();
      }
      
      protected function onFocusIn(event:FocusEvent) : void
      {
         this._focusIn = true;
         invalidate();
         if(Boolean(Base.stage) && Boolean(Base.stage.focus))
         {
            Base.stage.focus = this.textField;
         }
         this.addClipboardListener();
      }
      
      private function addClipboardListener() : void
      {
         Api.self.addEventListener(Api.CTRLV,this.onCtrlV);
      }
      
      protected function onCtrlV(event:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"clipboard:",event.data.answer.clipboard);
         this.text += StringUtils.drop_RN(event.data.answer.clipboard);
         this.dispatchEvent(new Event(Event.CHANGE));
         setTimeout(this.setSelectionToEnd,30);
      }
      
      private function removeClipboardListener() : void
      {
         Api.self.removeEventListener(Api.CTRLV,this.onCtrlV);
      }
      
      private function setSelectionToEnd() : void
      {
         this.textField.setSelection(this.textField.length,this.textField.length);
      }
      
      override public function draw() : void
      {
         super.draw();
         this._back.graphics.clear();
         this._back.graphics.beginFill(this._focusIn ? this.focusInColor : this.focusOutColor,this._focusIn ? this.focusInAlpha : this.focusOutAlpha);
         this._back.graphics.drawRect(0,0,_width,_height);
         this._back.graphics.endFill();
         this._tf.displayAsPassword = this._password;
         var format:TextFormat = this._tf.defaultTextFormat;
         format.color = this._focusIn ? this.focusInLabelColor : this.focusOutLabelColor;
         this._tf.defaultTextFormat = format;
         this._tf.alpha = this._focusIn ? this.focusInLabelAlpha : this.focusOutLabelAlpha;
         if(this._text != null)
         {
            this._tf.text = this._text;
         }
         else
         {
            this._tf.text = "";
         }
         this._tf.width = _width - padding * 2;
         if(this._tf.text == "")
         {
            this._tf.text = "X";
            this._tf.height = Math.min(this._tf.textHeight + 4,_height);
            this._tf.text = "";
         }
         else
         {
            this._tf.height = Math.min(this._tf.textHeight + 4,_height);
         }
         this._tf.x = 10;
         this._tf.y = Math.round(_height / 2 - this._tf.height / 2);
      }
      
      protected function onChange(event:Event) : void
      {
         this._text = this._tf.text;
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
      
      public function set restrict(str:String) : void
      {
         this._tf.restrict = str;
      }
      
      public function get restrict() : String
      {
         return this._tf.restrict;
      }
      
      public function set maxChars(max:int) : void
      {
         this._tf.maxChars = max;
      }
      
      public function get maxChars() : int
      {
         return this._tf.maxChars;
      }
      
      public function set password(b:Boolean) : void
      {
         this._password = b;
         invalidate();
      }
      
      public function get password() : Boolean
      {
         return this._password;
      }
      
      override public function set enabled(value:Boolean) : void
      {
         super.enabled = value;
         this._tf.tabEnabled = value;
      }
      
      public function set size(sizeValue:uint) : void
      {
         var format:TextFormat = this._tf.defaultTextFormat;
         format.size = sizeValue;
         this._tf.defaultTextFormat = format;
         invalidate();
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
      
      public function get focusInColor() : uint
      {
         return this._focusInColor;
      }
      
      public function set focusInColor(value:uint) : void
      {
         this._focusInColor = value;
         invalidate();
      }
      
      public function get focusOutColor() : uint
      {
         return this._focusOutColor;
      }
      
      public function set focusOutColor(value:uint) : void
      {
         this._focusOutColor = value;
         invalidate();
      }
      
      public function get focusInAlpha() : Number
      {
         return this._focusInAlpha;
      }
      
      public function set focusInAlpha(value:Number) : void
      {
         this._focusInAlpha = value;
         invalidate();
      }
      
      public function get focusOutAlpha() : Number
      {
         return this._focusOutAlpha;
      }
      
      public function set focusOutAlpha(value:Number) : void
      {
         this._focusOutAlpha = value;
         invalidate();
      }
      
      public function get focusOutLabelColor() : Number
      {
         return this._focusOutLabelColor;
      }
      
      public function set focusOutLabelColor(value:Number) : void
      {
         this._focusOutLabelColor = value;
         invalidate();
      }
      
      public function get focusInLabelColor() : Number
      {
         return this._focusInLabelColor;
      }
      
      public function set focusInLabelColor(value:Number) : void
      {
         this._focusInLabelColor = value;
         invalidate();
      }
      
      public function get focusOutLabelAlpha() : Number
      {
         return this._focusOutLabelAlpha;
      }
      
      public function set focusOutLabelAlpha(value:Number) : void
      {
         this._focusOutLabelAlpha = value;
         invalidate();
      }
      
      public function get focusInLabelAlpha() : Number
      {
         return this._focusInLabelAlpha;
      }
      
      public function set focusInLabelAlpha(value:Number) : void
      {
         this._focusInLabelAlpha = value;
         invalidate();
      }
   }
}

