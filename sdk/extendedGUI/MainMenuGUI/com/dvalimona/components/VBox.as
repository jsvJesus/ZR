package com.dvalimona.components
{
   import flash.display.*;
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
      
      private var background:Sprite;
      
      public var _debug:Boolean = false;
      
      public var fixedWidth:Number = 0;
      
      public var fixedHeight:Number = -1;
      
      public var ignoreInvisibleChildren:Boolean = false;
      
      private var _backgroundColor:int = -1;
      
      private var _backgroundAlpha:Number = 1;
      
      public function VBox(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0)
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
               if(this._alignment == LEFT)
               {
                  child.x = this.marginLeft + (!!child.hasOwnProperty("paddingLeft") ? (child as Object).paddingLeft : 0);
               }
               else if(this._alignment == RIGHT)
               {
                  child.x = _width - child.width - (!!child.hasOwnProperty("paddingRight") ? (child as Object).paddingRight : 0) - this.marginRight;
               }
               else if(this._alignment == CENTER)
               {
                  child.x = (_width - child.width) / 2;
               }
               else if(this._alignment == JUSTIFY)
               {
                  child.x = !!child.hasOwnProperty("paddingLeft") ? Number((child as Object).paddingLeft) : 0;
                  if(child is Component)
                  {
                     (child as Component).setWidth(_width - (!!child.hasOwnProperty("paddingLeft") ? (child as Object).paddingLeft : 0) - (!!child.hasOwnProperty("paddingRight") ? (child as Object).paddingRight : 0));
                  }
               }
            }
         }
      }
      
      public function getChildrenHeight() : int
      {
         var child:DisplayObject = null;
         var tmp_height:Number = 0;
         tmp_height += this.marginTop;
         var ypos:Number = this.marginTop;
         for(var i:int = 0; i < numChildren; i++)
         {
            child = getChildAt(i);
            if(!(this.ignoreInvisibleChildren && !child.visible))
            {
               child.y = ypos + (!!child.hasOwnProperty("paddingTop") ? (child as Object).paddingTop : 0);
               ypos += child.height + (!!child.hasOwnProperty("paddingTop") ? (child as Object).paddingTop : 0) + (!!child.hasOwnProperty("paddingBottom") ? (child as Object).paddingBottom : 0) + this._spacing;
               tmp_height += child.height + (!!child.hasOwnProperty("paddingTop") ? (child as Object).paddingTop : 0) + (!!child.hasOwnProperty("paddingBottom") ? (child as Object).paddingBottom : 0);
            }
         }
         tmp_height += this._spacing * (numChildren - 1);
         tmp_height += marginBottom;
         return Math.round(tmp_height);
      }
      
      override public function draw() : void
      {
         var child:DisplayObject = null;
         var maxWidth:Number = this.fixedWidth;
         var maxHeight:Number = this.fixedHeight;
         this.background.graphics.clear();
         this.background.width = 0;
         this.background.height = 0;
         var j:int = 0;
         for(var i:int = 0; i < numChildren; i++)
         {
            child = getChildAt(i);
            if(child != this.background)
            {
               if(this._alignment != JUSTIFY)
               {
                  maxWidth = Math.max(maxWidth,this.marginLeft + child.width + (!!child.hasOwnProperty("paddingLeft") ? (child as Object).paddingLeft : 0) + (!!child.hasOwnProperty("paddingRight") ? (child as Object).paddingRight : 0) + this.marginRight);
               }
               else
               {
                  _width = _width;
               }
            }
         }
         if(this._alignment != JUSTIFY)
         {
            _width = maxWidth;
         }
         if(this.fixedHeight >= 0)
         {
            _height = Math.min(this.getChildrenHeight(),this.fixedHeight);
         }
         else
         {
            _height = this.getChildrenHeight();
         }
         this.doAlignment();
         drawDebug();
         this.drawBackground();
         this.dispatchEvent(new Event(Event.RESIZE));
      }
      
      override protected function addChildren() : void
      {
         this.background = new Sprite();
         this.background.y = 0;
         this.background.x = 0;
         this.addChild(this.background);
      }
      
      private function drawBackground() : void
      {
         if(this.backgroundColor >= 0)
         {
            this.setChildIndex(this.background,0);
            this.background.graphics.clear();
            this.background.graphics.beginBitmapFill(Style.backgroundBitmap);
            this.background.graphics.drawRect(0,0,_width,_height);
            this.background.graphics.endFill();
            this.background.alpha = this.backgroundAlpha;
            this.background.width = _width;
            this.background.height = _height;
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
   }
}

