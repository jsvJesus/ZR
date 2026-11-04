package com.dvalimona.components
{
   import flash.display.DisplayObject;
   import flash.display.DisplayObjectContainer;
   import flash.events.*;
   
   public class VBox extends Component
   {
      public static const JUSTIFY:String = "justify";
      
      public static const LEFT:String = "left";
      
      public static const RIGHT:String = "right";
      
      public static const CENTER:String = "center";
      
      public static const NONE:String = "none";
      
      protected var _spacing:Number = 5;
      
      private var _alignment:String = "none";
      
      private var _backgroundColor:int = -1;
      
      private var _backgroundAlpha:Number = 1;
      
      public function VBox(param1:DisplayObjectContainer = null, param2:Number = 0, param3:Number = 0)
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
               if(this._alignment == LEFT)
               {
                  _loc2_.x = this.marginLeft + (!!_loc2_.hasOwnProperty("paddingLeft") ? (_loc2_ as Object).paddingLeft : 0);
               }
               else if(this._alignment == RIGHT)
               {
                  _loc2_.x = _width - _loc2_.width - (!!_loc2_.hasOwnProperty("paddingRight") ? (_loc2_ as Object).paddingRight : 0) - this.marginRight;
               }
               else if(this._alignment == CENTER)
               {
                  _loc2_.x = (_width - _loc2_.width) / 2;
               }
               else if(this._alignment == JUSTIFY)
               {
                  _loc2_.x = !!_loc2_.hasOwnProperty("paddingLeft") ? Number((_loc2_ as Object).paddingLeft) : 0;
                  if(_loc2_ is Component)
                  {
                     (_loc2_ as Component).setWidth(_width - (!!_loc2_.hasOwnProperty("paddingLeft") ? (_loc2_ as Object).paddingLeft : 0) - (!!_loc2_.hasOwnProperty("paddingRight") ? (_loc2_ as Object).paddingRight : 0));
                  }
               }
               _loc1_++;
            }
         }
      }
      
      override public function draw() : void
      {
         var _loc4_:DisplayObject = null;
         _height = 0;
         _height += this.marginTop;
         var _loc1_:Number = 0;
         var _loc2_:Number = this.marginTop;
         var _loc3_:int = 0;
         while(_loc3_ < numChildren)
         {
            _loc4_ = getChildAt(_loc3_);
            _loc4_.y = _loc2_ + (!!_loc4_.hasOwnProperty("paddingTop") ? (_loc4_ as Object).paddingTop : 0);
            _loc2_ += _loc4_.height + (!!_loc4_.hasOwnProperty("paddingTop") ? (_loc4_ as Object).paddingTop : 0) + (!!_loc4_.hasOwnProperty("paddingBottom") ? (_loc4_ as Object).paddingBottom : 0) + this._spacing;
            _height += _loc4_.height + (!!_loc4_.hasOwnProperty("paddingTop") ? (_loc4_ as Object).paddingTop : 0) + (!!_loc4_.hasOwnProperty("paddingBottom") ? (_loc4_ as Object).paddingBottom : 0);
            if(this._alignment != JUSTIFY)
            {
               _loc1_ = Math.max(_loc1_,this.marginLeft + _loc4_.width + (!!_loc4_.hasOwnProperty("paddingLeft") ? (_loc4_ as Object).paddingLeft : 0) + (!!_loc4_.hasOwnProperty("paddingRight") ? (_loc4_ as Object).paddingRight : 0) + this.marginRight);
            }
            else
            {
               _width = _width;
            }
            _loc3_++;
         }
         if(this._alignment != JUSTIFY)
         {
            _width = _loc1_;
         }
         this.doAlignment();
         _height += this._spacing * (numChildren - 1);
         _height += marginBottom;
         _height = Math.round(_height);
         drawDebug();
         this.drawBackground();
         this.dispatchEvent(new Event(Event.RESIZE));
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
   }
}

