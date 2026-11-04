package com.dvalimona.components
{
   import flash.display.*;
   import flash.events.*;
   
   public class PushButton extends Component
   {
      protected var _back:Sprite;
      
      protected var _face:Sprite;
      
      protected var _label:Label;
      
      protected var _labelText:String = "";
      
      protected var _over:Boolean = false;
      
      protected var _down:Boolean = false;
      
      protected var _selected:Boolean = false;
      
      protected var _toggle:Boolean = false;
      
      protected var _autoWidth:Boolean = false;
      
      private var _upColor:uint = Style.BUTTON_UP;
      
      private var _downColor:uint = Style.BUTTON_DOWN;
      
      private var _overColor:uint = Style.BUTTON_OVER;
      
      private var _upColorAlpha:Number = Style.BUTTON_UP_ALPHA;
      
      private var _downColorAlpha:Number = Style.BUTTON_DOWN_ALPHA;
      
      private var _overColorAlpha:Number = Style.BUTTON_OVER_ALPHA;
      
      private var _labelUpColor:uint = Style.BUTTON_UP_LABEL_COLOR;
      
      private var _labelDownColor:uint = Style.BUTTON_DOWN_LABEL_COLOR;
      
      private var _labelOverColor:uint = Style.BUTTON_OVER_LABEL_COLOR;
      
      private var _labelColorAlpha:Number = Style.BUTTON_UP_LABEL_ALPHA;
      
      private var _labelDownColorAlpha:Number = Style.BUTTON_DOWN_LABEL_ALPHA;
      
      private var _labelOverColorAlpha:Number = Style.BUTTON_OVER_LABEL_ALPHA;
      
      private var _align:String = Label.CENTER;
      
      public function PushButton(param1:DisplayObjectContainer = null, param2:Number = 0, param3:Number = 0, param4:String = "", param5:Function = null)
      {
         super(param1,param2,param3);
         if(param5 != null)
         {
            addEventListener(MouseEvent.CLICK,param5);
         }
         this.label = param4;
      }
      
      override public function updateLocale(param1:String) : void
      {
         this.label = param1;
         invalidate();
      }
      
      override protected function init() : void
      {
         super.init();
         buttonMode = true;
         useHandCursor = true;
         setSize(100,20);
      }
      
      override protected function addChildren() : void
      {
         this._back = new Sprite();
         this._back.mouseEnabled = false;
         addChild(this._back);
         this._face = new Sprite();
         this._face.mouseEnabled = false;
         addChild(this._face);
         this._label = new Label(this,0,0,"",22);
         this._label.clipContent = true;
         addEventListener(MouseEvent.MOUSE_DOWN,this.onMouseGoDown);
         addEventListener(MouseEvent.ROLL_OVER,this.onMouseOver);
      }
      
      protected function drawFace() : void
      {
         this._face.graphics.clear();
         if(this._over)
         {
            if(this._down)
            {
               this._label.color = this.labelDownColor;
               this._label.alpha = this.labelDownColorAlpha;
               this._face.graphics.beginFill(this.downColor,this.downColorAlpha);
            }
            else
            {
               this._label.color = this.labelOverColor;
               this._label.alpha = this.labelOverColorAlpha;
               this._face.graphics.beginFill(this.overColor,this.overColorAlpha);
            }
         }
         else
         {
            this._label.color = this.labelUpColor;
            this._label.alpha = enabled ? this.labelColorAlpha : disabledLabelAlpha;
            this._face.graphics.beginFill(this.upColor,this.upColorAlpha);
         }
         if(this._autoWidth)
         {
            _width = this._label.width;
         }
         this._face.graphics.drawRect(0,0,_width,_height);
         this._face.graphics.endFill();
      }
      
      override public function draw() : void
      {
         super.draw();
         this.drawFace();
         switch(this.align)
         {
            case Label.LEFT:
               this.alignLeft();
               break;
            case Label.RIGHT:
               this.alignRight();
               break;
            case Label.CENTER:
               this.alignCenter();
               break;
            case Label.JUSTIFY:
               this.alignJustify();
         }
         drawDebug();
      }
      
      private function alignLeft() : void
      {
         this._label.text = this._labelText;
         this._label.draw();
         this._label.width = width;
         if(this._label.width > width - marginLeft - marginRight)
         {
            this._label.width = width - marginLeft - marginRight;
         }
         this._label.draw();
         this._label.move(marginLeft,_height / 2 - this._label.height / 2);
      }
      
      private function alignRight() : void
      {
         this._label.text = this._labelText;
         this._label.draw();
         this._label.width = width;
         if(this._label.width > width - marginLeft - marginRight)
         {
            this._label.width = width - marginLeft - marginRight;
         }
         this._label.draw();
         this._label.move(width - this._label.width - marginRight,_height / 2 - this._label.height / 2);
      }
      
      private function alignCenter() : void
      {
         this._label.text = this._labelText;
         this._label.draw();
         this._label.width = width;
         if(this._label.width > width - marginLeft - marginRight)
         {
            this._label.autoSize = false;
            this._label.width = _width - marginLeft - marginRight;
         }
         else
         {
            this._label.autoSize = true;
         }
         this._label.draw();
         this._label.move(_width / 2 - this._label.width / 2,_height / 2 - this._label.height / 2);
      }
      
      private function alignJustify() : void
      {
      }
      
      protected function onMouseOver(param1:MouseEvent) : void
      {
         this._over = true;
         this.drawFace();
         addEventListener(MouseEvent.ROLL_OUT,this.onMouseOut);
      }
      
      protected function onMouseOut(param1:MouseEvent) : void
      {
         this._over = false;
         if(this._down)
         {
         }
         this.drawFace();
         removeEventListener(MouseEvent.ROLL_OUT,this.onMouseOut);
      }
      
      protected function onMouseGoDown(param1:MouseEvent) : void
      {
         this._down = true;
         this.drawFace();
         stage.addEventListener(MouseEvent.MOUSE_UP,this.onMouseGoUp);
      }
      
      protected function onMouseGoUp(param1:MouseEvent) : void
      {
         if(Boolean(this._toggle) && Boolean(this._over))
         {
            this._selected = !this._selected;
         }
         this._down = this._selected;
         this.drawFace();
         stage.removeEventListener(MouseEvent.MOUSE_UP,this.onMouseGoUp);
      }
      
      public function get labelComponent() : Label
      {
         return this._label;
      }
      
      public function set label(param1:String) : void
      {
         this._labelText = param1;
         this.draw();
      }
      
      public function get label() : String
      {
         return this._labelText;
      }
      
      public function set selected(param1:Boolean) : void
      {
         if(!this._toggle)
         {
            param1 = false;
         }
         this._selected = param1;
         this._down = this._selected;
         this.drawFace();
      }
      
      public function get selected() : Boolean
      {
         return this._selected;
      }
      
      public function set toggle(param1:Boolean) : void
      {
         this._toggle = param1;
      }
      
      public function get toggle() : Boolean
      {
         return this._toggle;
      }
      
      public function set size(param1:uint) : void
      {
         this._label.size = param1;
         invalidate();
      }
      
      public function set font(param1:String) : void
      {
         this._label.font = param1;
         invalidate();
      }
      
      public function set underline(param1:Boolean) : void
      {
         this._label.underline = param1;
         invalidate();
      }
      
      public function set autoWidth(param1:Boolean) : void
      {
         this._autoWidth = param1;
         invalidate();
      }
      
      public function get upColor() : uint
      {
         return this._upColor;
      }
      
      public function set upColor(param1:uint) : void
      {
         this._upColor = param1;
         invalidate();
      }
      
      public function get downColor() : uint
      {
         return this._downColor;
      }
      
      public function set downColor(param1:uint) : void
      {
         this._downColor = param1;
         invalidate();
      }
      
      public function get overColor() : uint
      {
         return this._overColor;
      }
      
      public function set overColor(param1:uint) : void
      {
         this._overColor = param1;
         invalidate();
      }
      
      public function get upColorAlpha() : Number
      {
         return this._upColorAlpha;
      }
      
      public function set upColorAlpha(param1:Number) : void
      {
         this._upColorAlpha = param1;
         invalidate();
      }
      
      public function get downColorAlpha() : Number
      {
         return this._downColorAlpha;
      }
      
      public function set downColorAlpha(param1:Number) : void
      {
         this._downColorAlpha = param1;
         invalidate();
      }
      
      public function get overColorAlpha() : Number
      {
         return this._overColorAlpha;
      }
      
      public function set overColorAlpha(param1:Number) : void
      {
         this._overColorAlpha = param1;
         invalidate();
      }
      
      public function get labelUpColor() : uint
      {
         return this._labelUpColor;
      }
      
      public function set labelUpColor(param1:uint) : void
      {
         this._labelUpColor = param1;
         invalidate();
      }
      
      public function get labelDownColor() : uint
      {
         return this._labelDownColor;
      }
      
      public function set labelDownColor(param1:uint) : void
      {
         this._labelDownColor = param1;
         invalidate();
      }
      
      public function get labelOverColor() : uint
      {
         return this._labelOverColor;
      }
      
      public function set labelOverColor(param1:uint) : void
      {
         this._labelOverColor = param1;
         invalidate();
      }
      
      public function get labelColorAlpha() : Number
      {
         return this._labelColorAlpha;
      }
      
      public function set labelColorAlpha(param1:Number) : void
      {
         this._labelColorAlpha = param1;
         invalidate();
      }
      
      public function get labelDownColorAlpha() : Number
      {
         return this._labelDownColorAlpha;
      }
      
      public function set labelDownColorAlpha(param1:Number) : void
      {
         this._labelDownColorAlpha = param1;
         invalidate();
      }
      
      public function get labelOverColorAlpha() : Number
      {
         return this._labelOverColorAlpha;
      }
      
      public function set labelOverColorAlpha(param1:Number) : void
      {
         this._labelOverColorAlpha = param1;
         invalidate();
      }
      
      public function set align(param1:String) : void
      {
         this._align = param1;
         this._label.align = this.align;
      }
      
      public function get align() : String
      {
         return this._align;
      }
   }
}

