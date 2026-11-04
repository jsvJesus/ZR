package com.dvalimona.components
{
   import flash.display.*;
   
   public class Panel extends Component
   {
      protected var _background:Sprite;
      
      protected var _color:int = -1;
      
      protected var _colorAlpha:Number = 1;
      
      protected var _shadow:Boolean = true;
      
      protected var _gridSize:int = 10;
      
      protected var _showGrid:Boolean = false;
      
      protected var _gridColor:uint = 13684944;
      
      protected var _gridAlpha:Number = 0.2;
      
      public var content:Sprite;
      
      public function Panel(param1:DisplayObjectContainer = null, param2:Number = 0, param3:Number = 0)
      {
         super(param1,param2,param3);
      }
      
      public function set backgroundAlpha(param1:Number) : void
      {
         this._background.alpha = param1;
      }
      
      override protected function init() : void
      {
         super.init();
         setSize(100,100);
      }
      
      override protected function addChildren() : void
      {
         this._background = new Sprite();
         super.addChild(this._background);
         this.content = new Sprite();
         super.addChild(this.content);
      }
      
      override public function addChild(param1:DisplayObject) : DisplayObject
      {
         this.content.addChild(param1);
         return param1;
      }
      
      public function addRawChild(param1:DisplayObject) : DisplayObject
      {
         super.addChild(param1);
         return param1;
      }
      
      override public function draw() : void
      {
         super.draw();
         this._background.graphics.clear();
         this._background.graphics.lineStyle(1,0,0.1);
         if(this._color == -1)
         {
            this._background.graphics.beginFill(Style.PANEL,this.colorAlpha);
         }
         else
         {
            this._background.graphics.beginFill(this._color,this.colorAlpha);
         }
         this._background.graphics.drawRect(0,0,_width,_height);
         this._background.graphics.endFill();
         this.drawGrid();
      }
      
      protected function drawGrid() : void
      {
         if(!this._showGrid)
         {
            return;
         }
         this._background.graphics.lineStyle(0,this._gridColor,this._gridAlpha);
         var _loc1_:int = 0;
         while(_loc1_ < _width)
         {
            this._background.graphics.moveTo(_loc1_,0);
            this._background.graphics.lineTo(_loc1_,_height);
            _loc1_ += this._gridSize;
         }
         _loc1_ = 0;
         while(_loc1_ < _height)
         {
            this._background.graphics.moveTo(0,_loc1_);
            this._background.graphics.lineTo(_width,_loc1_);
            _loc1_ += this._gridSize;
         }
      }
      
      public function set shadow(param1:Boolean) : void
      {
         this._shadow = param1;
         if(!this._shadow)
         {
         }
      }
      
      public function get shadow() : Boolean
      {
         return this._shadow;
      }
      
      public function set color(param1:int) : void
      {
         this._color = param1;
         invalidate();
      }
      
      public function get color() : int
      {
         return this._color;
      }
      
      public function set gridSize(param1:int) : void
      {
         this._gridSize = param1;
         invalidate();
      }
      
      public function get gridSize() : int
      {
         return this._gridSize;
      }
      
      public function set showGrid(param1:Boolean) : void
      {
         this._showGrid = param1;
         invalidate();
      }
      
      public function get showGrid() : Boolean
      {
         return this._showGrid;
      }
      
      public function set gridColor(param1:uint) : void
      {
         this._gridColor = param1;
         invalidate();
      }
      
      public function get gridColor() : uint
      {
         return this._gridColor;
      }
      
      public function get colorAlpha() : Number
      {
         return this._colorAlpha;
      }
      
      public function set colorAlpha(param1:Number) : void
      {
         this._colorAlpha = param1;
      }
   }
}

