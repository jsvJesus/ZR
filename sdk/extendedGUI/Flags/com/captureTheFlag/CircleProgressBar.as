package com.captureTheFlag
{
   import flash.display.Graphics;
   import flash.display.Shape;
   import flash.display.Sprite;
   import flash.events.Event;
   
   public class CircleProgressBar extends Sprite
   {
      private var _percent:Number;
      
      private var _width:int;
      
      private var _height:int;
      
      private var _shape:Shape;
      
      private var _canvas:Graphics;
      
      private var _step:Number;
      
      private var _color:uint;
      
      private var _thickness:int;
      
      private var scale:Number;
      
      private var halfWidth:int;
      
      private var halfHeight:int;
      
      private var halfWidthWithoutThickness:int;
      
      private var halfHeightWidthoutThickness:int;
      
      public function CircleProgressBar(param1:int = 100, param2:int = 100, param3:int = 30, param4:uint = 6750054, param5:Number = 0.2, param6:Number = 0.7)
      {
         super();
         this._width = param1;
         this._height = param2;
         this._percent = param6;
         this._step = param5;
         this._shape = new Shape();
         this._canvas = this._shape.graphics;
         this._color = param4;
         addChild(this._shape);
         if(param3 >= 0 && param3 <= int(param1 / 2))
         {
            this._thickness = param3;
         }
         else
         {
            this._thickness = int(param1 / 2);
         }
         this.halfWidth = int(this._width / 2);
         this.halfHeight = int(this._width / 2);
         this.halfWidthWithoutThickness = int((this._width - 2 * this._thickness) / 2);
         this.halfHeightWidthoutThickness = int((this._width - 2 * this._thickness) / 2);
         this.scale = this._height / this._width;
         this.update();
      }
      
      private function update() : void
      {
         this._canvas.clear();
         this._canvas.beginFill(this._color);
         var _loc1_:Number = this._percent * 6.283185307179586;
         this._canvas.moveTo(this.halfWidth + Math.cos(_loc1_) * this.halfWidth,this.halfHeight + Math.sin(_loc1_) * this.halfHeight);
         var _loc2_:Number = _loc1_;
         while(_loc2_ > 0)
         {
            this._canvas.lineTo(this.halfWidth + Math.cos(_loc2_) * this.halfWidth,this.halfHeight + Math.sin(_loc2_) * this.halfHeight);
            _loc2_ -= this._step;
         }
         this._canvas.lineTo(this.halfWidth + Math.cos(0) * this.halfWidth,this.halfHeight + Math.sin(0) * this.halfHeight);
         var _loc3_:Number = 0;
         while(_loc3_ < _loc1_)
         {
            this._canvas.lineTo(this.halfWidth + Math.cos(_loc3_) * this.halfWidthWithoutThickness,this.halfHeight + Math.sin(_loc3_) * this.halfHeightWidthoutThickness);
            _loc3_ += this._step;
         }
         this._canvas.lineTo(this.halfWidth + Math.cos(_loc1_) * this.halfWidthWithoutThickness,this.halfHeight + Math.sin(_loc1_) * this.halfHeightWidthoutThickness);
         this._canvas.endFill();
         this._shape.scaleY = this.scale;
         dispatchEvent(new Event(Event.CHANGE));
      }
      
      public function set value(param1:Number) : void
      {
         if(param1 < 0 || param1 > 100)
         {
            return;
         }
         this._percent = param1 / 100;
         this.update();
      }
      
      public function get value() : Number
      {
         return this._percent;
      }
      
      public function set color(param1:Number) : void
      {
         this._color = param1;
         this.update();
      }
   }
}

