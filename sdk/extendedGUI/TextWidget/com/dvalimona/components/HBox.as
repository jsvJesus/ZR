package com.dvalimona.components
{
   import flash.display.DisplayObject;
   import flash.display.DisplayObjectContainer;
   import flash.events.*;
   
   public class HBox extends Component
   {
      public static const TOP:String = "top";
      
      public static const BOTTOM:String = "bottom";
      
      public static const MIDDLE:String = "middle";
      
      public static const NONE:String = "none";
      
      public static const LEFT:String = "left";
      
      public static const RIGHT:String = "right";
      
      protected var _spacing:Number = 5;
      
      private var _alignment:String = "none";
      
      private var _backgroundColor:int = -1;
      
      private var _backgroundAlpha:Number = 1;
      
      private var _fixedHeight:uint;
      
      private var _fixedWidth:uint;
      
      private var _shift:Number = 0;
      
      private var _tabs:Array = [];
      
      private var _horizontalAlign:String = "left";
      
      public function HBox(param1:DisplayObjectContainer = null, param2:Number = 0, param3:Number = 0)
      {
         super(param1,param2,param3);
      }
      
      override public function addChild(param1:DisplayObject) : DisplayObject
      {
         super.addChild(param1);
         param1.addEventListener(Event.RESIZE,this.onResize);
         this.draw();
         return param1;
      }
      
      override public function addChildAt(param1:DisplayObject, param2:int) : DisplayObject
      {
         super.addChildAt(param1,param2);
         param1.addEventListener(Event.RESIZE,this.onResize);
         this.draw();
         return param1;
      }
      
      override public function removeChild(param1:DisplayObject) : DisplayObject
      {
         super.removeChild(param1);
         param1.removeEventListener(Event.RESIZE,this.onResize);
         this.draw();
         return param1;
      }
      
      override public function removeChildAt(param1:int) : DisplayObject
      {
         var _loc2_:DisplayObject = super.removeChildAt(param1);
         _loc2_.removeEventListener(Event.RESIZE,this.onResize);
         this.draw();
         return _loc2_;
      }
      
      protected function onResize(param1:Event) : void
      {
         invalidate();
      }
      
      protected function doAlignment() : void
      {
         var _loc1_:int = 0;
         var _loc2_:DisplayObject = null;
         if(this._alignment != NONE)
         {
            _loc1_ = 0;
            while(_loc1_ < numChildren)
            {
               _loc2_ = getChildAt(_loc1_);
               if(this._alignment == TOP)
               {
                  _loc2_.y = 0 + this.shift;
               }
               else if(this._alignment == BOTTOM)
               {
                  _loc2_.y = _height - _loc2_.height + this.shift;
               }
               else if(this._alignment == MIDDLE)
               {
                  _loc2_.y = (_height - _loc2_.height) / 2 + this.shift;
               }
               _loc1_++;
            }
         }
      }
      
      override public function draw() : void
      {
         var _loc1_:Number = NaN;
         var _loc2_:DisplayObject = null;
         var _loc3_:uint = 0;
         if(this.horizontalAlign == LEFT)
         {
            _width = this.fixedWidth;
            _height = this.fixedHeight;
            _loc1_ = 0;
            _loc3_ = 0;
            while(_loc3_ < numChildren)
            {
               _loc2_ = getChildAt(_loc3_);
               _loc2_.x = (Boolean(this.tabStops.length) && Boolean(this.tabStops[_loc3_]) ? this.tabStops[_loc3_] : _loc1_) + (!!_loc2_.hasOwnProperty("paddingLeft") ? (_loc2_ as Object).paddingLeft : 0);
               _loc1_ += _loc2_.width + (!!_loc2_.hasOwnProperty("paddingLeft") ? (_loc2_ as Object).paddingLeft : 0) + (!!_loc2_.hasOwnProperty("paddingRight") ? (_loc2_ as Object).paddingRight : 0) + this._spacing;
               _height = Math.max(_height,_loc2_.height);
               _loc3_++;
            }
            this.doAlignment();
            dispatchEvent(new Event(Event.RESIZE));
         }
         if(this.horizontalAlign == RIGHT)
         {
            _width = this.fixedWidth;
            _height = this.fixedHeight;
            _loc1_ = _width;
            _loc3_ = 0;
            while(_loc3_ < numChildren)
            {
               _loc2_ = getChildAt(_loc3_);
               _loc1_ -= _loc2_.width + (!!_loc2_.hasOwnProperty("paddingRight") ? (_loc2_ as Object).paddingRight : 0) + (!!_loc2_.hasOwnProperty("paddingLeft") ? (_loc2_ as Object).paddingLeft : 0);
               if(Boolean(this.tabStops.length) && Boolean(this.tabStops[_loc3_]))
               {
                  _loc2_.x = _width - this.tabStops[_loc3_] - _loc2_.width - (!!_loc2_.hasOwnProperty("paddingRight") ? (_loc2_ as Object).paddingRight : 0);
               }
               else
               {
                  _loc2_.x = _loc1_;
               }
               _loc1_ -= this._spacing;
               _loc3_++;
            }
            this.doAlignment();
            dispatchEvent(new Event(Event.RESIZE));
         }
         drawDebug();
         this.drawBackground();
      }
      
      private function drawBackground() : void
      {
         if(this.backgroundColor >= 0)
         {
            this.graphics.clear();
            this.graphics.beginFill(this.backgroundColor,this.backgroundAlpha);
            this.graphics.drawRect(0,0,_width,_height);
            this.graphics.endFill();
         }
      }
      
      public function set spacing(param1:Number) : void
      {
         this._spacing = param1;
         invalidate();
      }
      
      public function get spacing() : Number
      {
         return this._spacing;
      }
      
      public function set alignment(param1:String) : void
      {
         this._alignment = param1;
         invalidate();
      }
      
      public function get alignment() : String
      {
         return this._alignment;
      }
      
      public function get backgroundColor() : int
      {
         return this._backgroundColor;
      }
      
      public function set backgroundColor(param1:int) : void
      {
         this._backgroundColor = param1;
         invalidate();
      }
      
      public function get backgroundAlpha() : Number
      {
         return this._backgroundAlpha;
      }
      
      public function set backgroundAlpha(param1:Number) : void
      {
         this._backgroundAlpha = param1;
         invalidate();
      }
      
      public function get fixedHeight() : uint
      {
         return this._fixedHeight;
      }
      
      public function set fixedHeight(param1:uint) : void
      {
         this._fixedHeight = param1;
         invalidate();
      }
      
      public function get fixedWidth() : uint
      {
         return this._fixedWidth;
      }
      
      public function set fixedWidth(param1:uint) : void
      {
         this._fixedWidth = param1;
         invalidate();
      }
      
      public function get shift() : Number
      {
         return this._shift;
      }
      
      public function set shift(param1:Number) : void
      {
         this._shift = param1;
         invalidate();
      }
      
      public function get tabStops() : Array
      {
         return this._tabs;
      }
      
      public function set tabStops(param1:Array) : void
      {
         this._tabs = param1;
         invalidate();
      }
      
      public function get horizontalAlign() : String
      {
         return this._horizontalAlign;
      }
      
      public function set horizontalAlign(param1:String) : void
      {
         this._horizontalAlign = param1;
         invalidate();
      }
   }
}

