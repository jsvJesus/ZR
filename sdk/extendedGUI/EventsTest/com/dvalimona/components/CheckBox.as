package com.dvalimona.components
{
   import com.greensock.TweenMax;
   import com.greensock.easing.Expo;
   import flash.display.DisplayObjectContainer;
   import flash.display.Sprite;
   import flash.events.Event;
   import flash.events.MouseEvent;
   
   public class CheckBox extends Component
   {
      protected var _back:Sprite;
      
      protected var _button:Sprite;
      
      protected var _label:Label;
      
      protected var _labelText:String = "";
      
      protected var _selected:Boolean = false;
      
      private var _checkWidth:Number = Style.CHECK_WIDTH;
      
      private var _checkHeight:Number = Style.CHECK_HEIGHT;
      
      private var _checkRoundness:Number = Style.CHECK_ROUNDNESS;
      
      private var _checkGap:Number = Style.CHECK_GAP;
      
      private var _checkBodyColor:uint = Style.CHECK_BODY_COLOR;
      
      private var _checkBorderWidth:uint = Style.CHECK_BORDER_WIDTH;
      
      private var _checkBodyAlpha:Number = Style.CHECK_BODY_ALPHA;
      
      private var _checkOnColor:uint = Style.CHECK_ON_COLOR;
      
      private var _checkOffColor:uint = Style.CHECK_OFF_COLOR;
      
      private var _checkOnAlpha:Number = Style.CHECK_ON_ALPHA;
      
      private var _checkOffAlpha:Number = Style.CHECK_OFF_ALPHA;
      
      private var _checkSpacing:Number = Style.CHECK_SPACING;
      
      public function CheckBox(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0, label:String = "", defaultHandler:Function = null)
      {
         this._labelText = label;
         super(parent,xpos,ypos);
         if(defaultHandler != null)
         {
            addEventListener(MouseEvent.CLICK,defaultHandler);
         }
      }
      
      override protected function init() : void
      {
         super.init();
         buttonMode = true;
         useHandCursor = true;
         mouseChildren = false;
      }
      
      override protected function addChildren() : void
      {
         this._back = new Sprite();
         this._back.filters = [getShadow(2,true)];
         addChild(this._back);
         this._button = new Sprite();
         this._button.filters = [getShadow(1)];
         addChild(this._button);
         this._label = new Label(this,0,0,this._labelText);
         this._label.size = 20;
         this.draw();
         addEventListener(MouseEvent.CLICK,this.onClick);
         addEventListener(MouseEvent.MOUSE_DOWN,this.onMouseDown);
      }
      
      protected function onMouseDown(event:MouseEvent) : void
      {
         event.stopImmediatePropagation();
      }
      
      override public function draw() : void
      {
         super.draw();
         this._back.graphics.clear();
         this._back.graphics.lineStyle(this.checkBorderWidth,this.checkBodyColor,this.checkBodyAlpha,false);
         this._back.graphics.beginFill(this.selected ? this.checkOnColor : this.checkOffColor,this.selected ? this.checkOnAlpha : this.checkOffAlpha);
         this._back.graphics.drawRoundRect(this.checkBorderWidth / 2,this.checkBorderWidth / 2,this.checkWidth - this.checkBorderWidth,this.checkHeight - this.checkBorderWidth,Math.min(this.checkHeight,this.checkWidth) * this.checkRoundness,Math.min(this.checkHeight,this.checkWidth) * this.checkRoundness);
         this._back.graphics.endFill();
         this._button.graphics.clear();
         this._button.graphics.beginFill(0,0);
         this._button.graphics.drawCircle(this.checkHeight / 2,this.checkHeight / 2,this.checkHeight / 2);
         this._button.graphics.beginFill(this.checkBodyColor,this.checkBodyAlpha);
         this._button.graphics.drawCircle(this.checkHeight / 2,this.checkHeight / 2,Math.min(this.checkHeight,this.checkWidth) / 2 - this.checkBorderWidth - this.checkSpacing);
         this._button.graphics.endFill();
         this._button.y = 0;
         this._label.text = this._labelText;
         this._label.draw();
         this._label.x = this.checkWidth + this.checkGap;
         this._label.y = (10 - this._label.height) / 2;
         _width = this.checkWidth + (this._label.width > 0 ? this.checkGap + this._label.width : 0);
         _height = this.checkHeight;
         TweenMax.killTweensOf(this._button);
         TweenMax.to(this._button,0.2,{
            "x":this.buttonPosition,
            "ease":Expo.easeIn
         });
         drawDebug();
      }
      
      protected function get buttonPosition() : Number
      {
         if(this.selected)
         {
            return this.checkWidth - this._button.width;
         }
         return 0;
      }
      
      protected function onClick(event:MouseEvent) : void
      {
         this._selected = !this._selected;
         invalidate();
         this.dispatchEvent(new Event(Event.CHANGE));
      }
      
      public function set label(str:String) : void
      {
         this._labelText = str;
         invalidate();
      }
      
      public function get label() : String
      {
         return this._labelText;
      }
      
      public function set selected(s:Boolean) : void
      {
         this._selected = s;
         invalidate();
      }
      
      public function get selected() : Boolean
      {
         return this._selected;
      }
      
      override public function set enabled(value:Boolean) : void
      {
         super.enabled = value;
         mouseChildren = false;
      }
      
      public function get checkWidth() : Number
      {
         return this._checkWidth;
      }
      
      public function set checkWidth(value:Number) : void
      {
         this._checkWidth = value;
         invalidate();
      }
      
      public function get checkHeight() : Number
      {
         return this._checkHeight;
      }
      
      public function set checkHeight(value:Number) : void
      {
         this._checkHeight = value;
         invalidate();
      }
      
      public function get checkRoundness() : Number
      {
         return this._checkRoundness;
      }
      
      public function set checkRoundness(value:Number) : void
      {
         this._checkRoundness = value < 0 ? 0 : (value > 1 ? 1 : value);
         invalidate();
      }
      
      public function get checkGap() : Number
      {
         return this._checkGap;
      }
      
      public function set checkGap(value:Number) : void
      {
         this._checkGap = value;
         invalidate();
      }
      
      public function get checkBodyColor() : uint
      {
         return this._checkBodyColor;
      }
      
      public function set checkBodyColor(value:uint) : void
      {
         this._checkBodyColor = value;
         invalidate();
      }
      
      public function get checkBorderWidth() : uint
      {
         return this._checkBorderWidth;
      }
      
      public function set checkBorderWidth(value:uint) : void
      {
         this._checkBorderWidth = value;
         invalidate();
      }
      
      public function get checkBodyAlpha() : Number
      {
         return this._checkBodyAlpha;
      }
      
      public function set checkBodyAlpha(value:Number) : void
      {
         this._checkBodyAlpha = value;
         invalidate();
      }
      
      public function get checkOnColor() : uint
      {
         return this._checkOnColor;
      }
      
      public function set checkOnColor(value:uint) : void
      {
         this._checkOnColor = value;
         invalidate();
      }
      
      public function get checkOffColor() : uint
      {
         return this._checkOffColor;
      }
      
      public function set checkOffColor(value:uint) : void
      {
         this._checkOffColor = value;
         invalidate();
      }
      
      public function get checkOnAlpha() : Number
      {
         return this._checkOnAlpha;
      }
      
      public function set checkOnAlpha(value:Number) : void
      {
         this._checkOnAlpha = value;
         invalidate();
      }
      
      public function get checkOffAlpha() : Number
      {
         return this._checkOffAlpha;
      }
      
      public function set checkOffAlpha(value:Number) : void
      {
         this._checkOffAlpha = value;
         invalidate();
      }
      
      public function get checkSpacing() : Number
      {
         return this._checkSpacing;
      }
      
      public function set checkSpacing(value:Number) : void
      {
         this._checkSpacing = value;
         invalidate();
      }
   }
}

