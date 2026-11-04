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
      
      public function Panel(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0)
      {
         super(parent,xpos,ypos);
      }
      
      public function set backgroundAlpha(value:Number) : void
      {
         this._background.alpha = value;
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
      
      override public function addChild(child:DisplayObject) : DisplayObject
      {
         this.content.addChild(child);
         return child;
      }
      
      public function addRawChild(child:DisplayObject) : DisplayObject
      {
         super.addChild(child);
         return child;
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
         for(var i:int = 0; i < _width; i += this._gridSize)
         {
            this._background.graphics.moveTo(i,0);
            this._background.graphics.lineTo(i,_height);
         }
         for(i = 0; i < _height; i += this._gridSize)
         {
            this._background.graphics.moveTo(0,i);
            this._background.graphics.lineTo(_width,i);
         }
      }
      
      public function set shadow(b:Boolean) : void
      {
         this._shadow = b;
         if(!this._shadow)
         {
         }
      }
      
      public function get shadow() : Boolean
      {
         return this._shadow;
      }
      
      public function set color(c:int) : void
      {
         this._color = c;
         invalidate();
      }
      
      public function get color() : int
      {
         return this._color;
      }
      
      public function set gridSize(value:int) : void
      {
         this._gridSize = value;
         invalidate();
      }
      
      public function get gridSize() : int
      {
         return this._gridSize;
      }
      
      public function set showGrid(value:Boolean) : void
      {
         this._showGrid = value;
         invalidate();
      }
      
      public function get showGrid() : Boolean
      {
         return this._showGrid;
      }
      
      public function set gridColor(value:uint) : void
      {
         this._gridColor = value;
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
      
      public function set colorAlpha(value:Number) : void
      {
         this._colorAlpha = value;
      }
   }
}

