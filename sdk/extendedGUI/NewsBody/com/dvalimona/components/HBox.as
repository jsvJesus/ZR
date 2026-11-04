package com.dvalimona.components
{
   import flash.display.DisplayObject;
   import flash.display.DisplayObjectContainer;
   import flash.events.Event;
   
   [Event(name="resize",type="flash.events.Event")]
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
      
      public function HBox(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0)
      {
         super(parent,xpos,ypos);
      }
      
      override public function addChild(child:DisplayObject) : DisplayObject
      {
         super.addChild(child);
         child.addEventListener(Event.RESIZE,this.onResize);
         this.draw();
         return child;
      }
      
      override public function addChildAt(child:DisplayObject, index:int) : DisplayObject
      {
         super.addChildAt(child,index);
         child.addEventListener(Event.RESIZE,this.onResize);
         this.draw();
         return child;
      }
      
      override public function removeChild(child:DisplayObject) : DisplayObject
      {
         super.removeChild(child);
         child.removeEventListener(Event.RESIZE,this.onResize);
         this.draw();
         return child;
      }
      
      override public function removeChildAt(index:int) : DisplayObject
      {
         var child:DisplayObject = super.removeChildAt(index);
         child.removeEventListener(Event.RESIZE,this.onResize);
         this.draw();
         return child;
      }
      
      protected function onResize(event:Event) : void
      {
         invalidate();
      }
      
      protected function doAlignment() : void
      {
         var i:int = 0;
         var child:DisplayObject = null;
         if(this._alignment != NONE)
         {
            for(i = 0; i < numChildren; i++)
            {
               child = getChildAt(i);
               if(this._alignment == TOP)
               {
                  child.y = 0 + this.shift;
               }
               else if(this._alignment == BOTTOM)
               {
                  child.y = _height - child.height + this.shift;
               }
               else if(this._alignment == MIDDLE)
               {
                  child.y = (_height - child.height) / 2 + this.shift;
               }
            }
         }
      }
      
      override public function draw() : void
      {
         var xpos:Number = NaN;
         var child:DisplayObject = null;
         var i:uint = 0;
         if(this.horizontalAlign == LEFT)
         {
            _width = this.fixedWidth;
            _height = this.fixedHeight;
            xpos = 0;
            for(i = 0; i < numChildren; i++)
            {
               child = getChildAt(i);
               child.x = (Boolean(this.tabStops.length) && Boolean(this.tabStops[i]) ? this.tabStops[i] : xpos) + (!!child.hasOwnProperty("paddingLeft") ? (child as Object).paddingLeft : 0);
               xpos += child.width + (!!child.hasOwnProperty("paddingLeft") ? (child as Object).paddingLeft : 0) + (!!child.hasOwnProperty("paddingRight") ? (child as Object).paddingRight : 0) + this._spacing;
               _height = Math.max(_height,child.height);
            }
            this.doAlignment();
            dispatchEvent(new Event(Event.RESIZE));
         }
         if(this.horizontalAlign == RIGHT)
         {
            _width = this.fixedWidth;
            _height = this.fixedHeight;
            xpos = _width;
            for(i = 0; i < numChildren; i++)
            {
               child = getChildAt(i);
               xpos -= child.width + (!!child.hasOwnProperty("paddingRight") ? (child as Object).paddingRight : 0) + (!!child.hasOwnProperty("paddingLeft") ? (child as Object).paddingLeft : 0);
               if(Boolean(this.tabStops.length) && Boolean(this.tabStops[i]))
               {
                  child.x = _width - this.tabStops[i] - child.width - (!!child.hasOwnProperty("paddingRight") ? (child as Object).paddingRight : 0);
               }
               else
               {
                  child.x = xpos;
               }
               xpos -= this._spacing;
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
      
      public function set spacing(s:Number) : void
      {
         this._spacing = s;
         invalidate();
      }
      
      public function get spacing() : Number
      {
         return this._spacing;
      }
      
      public function set alignment(value:String) : void
      {
         this._alignment = value;
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
      
      public function set backgroundColor(value:int) : void
      {
         this._backgroundColor = value;
         invalidate();
      }
      
      public function get backgroundAlpha() : Number
      {
         return this._backgroundAlpha;
      }
      
      public function set backgroundAlpha(value:Number) : void
      {
         this._backgroundAlpha = value;
         invalidate();
      }
      
      public function get fixedHeight() : uint
      {
         return this._fixedHeight;
      }
      
      public function set fixedHeight(value:uint) : void
      {
         this._fixedHeight = value;
         invalidate();
      }
      
      public function get fixedWidth() : uint
      {
         return this._fixedWidth;
      }
      
      public function set fixedWidth(value:uint) : void
      {
         this._fixedWidth = value;
         invalidate();
      }
      
      public function get shift() : Number
      {
         return this._shift;
      }
      
      public function set shift(value:Number) : void
      {
         this._shift = value;
         invalidate();
      }
      
      public function get tabStops() : Array
      {
         return this._tabs;
      }
      
      public function set tabStops(value:Array) : void
      {
         this._tabs = value;
         invalidate();
      }
      
      public function get horizontalAlign() : String
      {
         return this._horizontalAlign;
      }
      
      public function set horizontalAlign(value:String) : void
      {
         this._horizontalAlign = value;
         invalidate();
      }
   }
}

