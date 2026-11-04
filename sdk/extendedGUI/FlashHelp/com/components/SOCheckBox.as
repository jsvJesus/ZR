package com.components
{
   import flash.display.Sprite;
   import flash.events.MouseEvent;
   import flash.filters.DropShadowFilter;
   import flash.text.TextField;
   import flash.text.TextFieldAutoSize;
   import flash.text.TextFieldType;
   import flash.text.TextFormat;
   
   public class SOCheckBox extends Component
   {
      protected var _checkOverlay:Sprite;
      
      protected var _checkFrame:Sprite;
      
      protected var _label:TextField;
      
      protected var _background:Sprite;
      
      protected var _checked:Boolean = false;
      
      protected var _over:Boolean = false;
      
      protected var _down:Boolean = false;
      
      protected var CHECK_SIZE:* = 12;
      
      public var backgroundColor:uint = 0;
      
      public var backgroundAplha:Number = 0;
      
      public var checkColor:uint = 11184551;
      
      public var checkFrameColor:uint;
      
      public function SOCheckBox()
      {
         super();
         this._checkOverlay = new Sprite();
         this._checkFrame = new Sprite();
         this._label = new TextField();
         this._background = new Sprite();
         this.height = 20;
         this.width = 154;
         this.drawAll(154,20);
         addChild(this._background);
         addChild(this._label);
         addChild(this._checkOverlay);
         addChild(this._checkFrame);
         this._label.filters = this.getTextFilters();
         this._label.defaultTextFormat = new TextFormat("Arial",12,10066329);
         this._label.autoSize = TextFieldAutoSize.NONE;
         this._label.width = 154 - 4 - this.CHECK_SIZE;
         this._label.height = 20;
         this._label.text = "dasfg";
         this._label.selectable = false;
         this._label.background = false;
         this._label.type = TextFieldType.DYNAMIC;
         this._checkOverlay.visible = false;
         this.addEventListener(MouseEvent.CLICK,this.onMouseHandleClick);
      }
      
      public function get over() : Boolean
      {
         return this._over;
      }
      
      public function set over(value:Boolean) : void
      {
         this._over = value;
      }
      
      public function get checked() : Boolean
      {
         return this._checked;
      }
      
      public function set checked(value:Boolean) : void
      {
         this._checked = value;
         this.drawAll(width,height);
      }
      
      public function get down() : Boolean
      {
         return this._down;
      }
      
      public function set down(value:Boolean) : void
      {
         this._down = value;
      }
      
      public function get text() : String
      {
         return this._label.text;
      }
      
      public function set text(value:String) : void
      {
         this._label.text = value;
      }
      
      override public function set width(value:Number) : void
      {
         super.width = value;
         this.drawAll(value,this.height);
      }
      
      override public function set height(value:Number) : void
      {
         super.height = value;
         this.drawAll(width,value);
      }
      
      public function set defaultTextFormat(value:TextFormat) : *
      {
         this._label.defaultTextFormat = value;
         var tmp:String = this._label.text;
         this._label.text = "";
         this._label.text = tmp;
      }
      
      protected function getTextFilters() : Array
      {
         var filter:DropShadowFilter = new DropShadowFilter();
         filter.blurX = 2;
         filter.blurY = 2;
         filter.angle = 45;
         filter.distance = 0;
         return [filter];
      }
      
      protected function drawBackground(w:Number, h:Number) : *
      {
         var X:* = undefined;
         var Y:Number = NaN;
         var overX:* = undefined;
         var overY:Number = NaN;
         this._background.graphics.clear();
         this._background.x = 0;
         this._background.y = 0;
         this._background.graphics.beginFill(this.backgroundColor,this.backgroundAplha);
         this._background.graphics.drawRect(0,0,w,h);
         this._background.graphics.endFill();
         X = w - 2 - this.CHECK_SIZE - 1;
         Y = h / 2 - this.CHECK_SIZE / 2;
         this._background.graphics.beginFill(10027008,1);
         this._background.graphics.drawRect(X,Y,1,1);
         this._background.graphics.drawRect(X + this.CHECK_SIZE,Y,1,1);
         this._background.graphics.drawRect(X,Y + this.CHECK_SIZE,1,1);
         this._background.graphics.beginFill(this.checkColor,1);
         this._background.graphics.drawRect(X,Y,1,this.CHECK_SIZE);
         this._background.graphics.drawRect(X,Y,this.CHECK_SIZE,1);
         this._background.graphics.drawRect(X + this.CHECK_SIZE,Y,1,this.CHECK_SIZE);
         this._background.graphics.drawRect(X,Y + this.CHECK_SIZE,this.CHECK_SIZE,1);
         this._background.graphics.drawRect(X,Y,1,1);
         this._background.graphics.drawRect(X + this.CHECK_SIZE,Y + this.CHECK_SIZE,1,1);
         overX = X - (this.CHECK_SIZE - 1) / 2 - (this.CHECK_SIZE - 1) / 4 + this.CHECK_SIZE;
         overY = Y - (this.CHECK_SIZE - 1) / 2 - (this.CHECK_SIZE - 1) / 4 + this.CHECK_SIZE;
         this._checkOverlay.graphics.clear();
         this._checkOverlay.graphics.beginFill(this.checkColor,1);
         this._checkOverlay.graphics.drawRect(overX,overY,(this.CHECK_SIZE - 1) / 2,(this.CHECK_SIZE - 1) / 2);
         this._checkOverlay.graphics.endFill();
         this._checkOverlay.visible = this.checked;
      }
      
      protected function drawCheck() : *
      {
         var X:* = undefined;
         var Y:Number = NaN;
         X = this.width - 2 - this.CHECK_SIZE;
         Y = this.height / 2 - this.CHECK_SIZE / 2;
         this._checkFrame.x = X;
         this._checkFrame.y = Y;
         this._checkFrame.graphics.clear();
         this._checkFrame.graphics.beginFill(this.checkColor,1);
         this._checkFrame.graphics.drawRect(0,0,1,this.CHECK_SIZE);
         this._checkFrame.graphics.drawRect(X,Y,this.CHECK_SIZE,1);
         this._checkFrame.graphics.drawRect(X + this.CHECK_SIZE - 1,Y,1,this.CHECK_SIZE);
         this._checkFrame.graphics.endFill();
      }
      
      protected function onMouseHandleClick(e:MouseEvent) : *
      {
         this.checked = !this.checked;
         this._checkOverlay.visible = this.checked;
      }
      
      protected function drawAll(w:Number, h:Number) : *
      {
         this.drawBackground(w,h);
         this._label.width = this.width - 4 - this.CHECK_SIZE;
      }
   }
}

