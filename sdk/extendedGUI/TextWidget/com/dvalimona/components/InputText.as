package com.dvalimona.components
{
   import com.dvalimona.utils.*;
   import communication.*;
   import events.ApiEvent;
   import flash.display.*;
   import flash.events.*;
   import flash.text.*;
   import flash.utils.*;
   import logging.*;
   
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
      
      public function InputText(param1:DisplayObjectContainer = null, param2:Number = 0, param3:Number = 0, param4:String = "", param5:Function = null)
      {
         this.text = param4;
         super(param1,param2,param3);
         if(param5 != null)
         {
            addEventListener(Event.CHANGE,param5);
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
      
      protected function onMouseDown(param1:MouseEvent) : void
      {
         Base.stage.focus = this._tf;
         param1.stopImmediatePropagation();
      }
      
      protected function onFocusOut(param1:FocusEvent) : void
      {
         this._focusIn = false;
         invalidate();
      }
      
      protected function onFocusIn(param1:FocusEvent) : void
      {
         this._focusIn = true;
         invalidate();
         if(Boolean(Base.stage) && Boolean(Base.stage.focus))
         {
            Base.stage.focus = this.textField;
         }
      }
      
      private function addClipboardListener() : void
      {
         Api.self.addEventListener(Api.CTRLV,this.onCtrlV);
      }
      
      protected function onCtrlV(param1:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"clipboard:",param1.data.answer.clipboard);
         this.text += StringUtils.drop_RN(param1.data.answer.clipboard);
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
         this._back.graphics.beginFill(!!this._focusIn ? this.focusInColor : this.focusOutColor,!!this._focusIn ? this.focusInAlpha : this.focusOutAlpha);
         this._back.graphics.drawRect(0,0,_width,_height);
         this._back.graphics.endFill();
         this._tf.displayAsPassword = this._password;
         var _loc1_:TextFormat = this._tf.defaultTextFormat;
         _loc1_.color = !!this._focusIn ? this.focusInLabelColor : this.focusOutLabelColor;
         this._tf.defaultTextFormat = _loc1_;
         this._tf.alpha = !!this._focusIn ? this.focusInLabelAlpha : this.focusOutLabelAlpha;
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
      
      protected function onChange(param1:Event) : void
      {
         this._text = this._tf.text;
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
      
      public function set restrict(param1:String) : void
      {
         this._tf.restrict = param1;
      }
      
      public function get restrict() : String
      {
         return this._tf.restrict;
      }
      
      public function set maxChars(param1:int) : void
      {
         this._tf.maxChars = param1;
      }
      
      public function get maxChars() : int
      {
         return this._tf.maxChars;
      }
      
      public function set password(param1:Boolean) : void
      {
         this._password = param1;
         invalidate();
      }
      
      public function get password() : Boolean
      {
         return this._password;
      }
      
      override public function set enabled(param1:Boolean) : void
      {
         super.enabled = param1;
         this._tf.tabEnabled = param1;
      }
      
      public function set size(param1:uint) : void
      {
         var _loc2_:TextFormat = this._tf.defaultTextFormat;
         _loc2_.size = param1;
         this._tf.defaultTextFormat = _loc2_;
         invalidate();
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
      
      public function get focusInColor() : uint
      {
         return this._focusInColor;
      }
      
      public function set focusInColor(param1:uint) : void
      {
         this._focusInColor = param1;
         invalidate();
      }
      
      public function get focusOutColor() : uint
      {
         return this._focusOutColor;
      }
      
      public function set focusOutColor(param1:uint) : void
      {
         this._focusOutColor = param1;
         invalidate();
      }
      
      public function get focusInAlpha() : Number
      {
         return this._focusInAlpha;
      }
      
      public function set focusInAlpha(param1:Number) : void
      {
         this._focusInAlpha = param1;
         invalidate();
      }
      
      public function get focusOutAlpha() : Number
      {
         return this._focusOutAlpha;
      }
      
      public function set focusOutAlpha(param1:Number) : void
      {
         this._focusOutAlpha = param1;
         invalidate();
      }
      
      public function get focusOutLabelColor() : Number
      {
         return this._focusOutLabelColor;
      }
      
      public function set focusOutLabelColor(param1:Number) : void
      {
         this._focusOutLabelColor = param1;
         invalidate();
      }
      
      public function get focusInLabelColor() : Number
      {
         return this._focusInLabelColor;
      }
      
      public function set focusInLabelColor(param1:Number) : void
      {
         this._focusInLabelColor = param1;
         invalidate();
      }
      
      public function get focusOutLabelAlpha() : Number
      {
         return this._focusOutLabelAlpha;
      }
      
      public function set focusOutLabelAlpha(param1:Number) : void
      {
         this._focusOutLabelAlpha = param1;
         invalidate();
      }
      
      public function get focusInLabelAlpha() : Number
      {
         return this._focusInLabelAlpha;
      }
      
      public function set focusInLabelAlpha(param1:Number) : void
      {
         this._focusInLabelAlpha = param1;
         invalidate();
      }
   }
}

