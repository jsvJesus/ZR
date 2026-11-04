package com.dvalimona.components
{
   import flash.display.DisplayObject;
   import flash.display.DisplayObjectContainer;
   import flash.events.Event;
   
   [Event(name="resize",type="flash.events.Event")]
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
                     trace("child is component",child);
                     (child as Component).setWidth(_width - (!!child.hasOwnProperty("paddingLeft") ? (child as Object).paddingLeft : 0) - (!!child.hasOwnProperty("paddingRight") ? (child as Object).paddingRight : 0));
                  }
                  else
                  {
                     trace("child is not component",child);
                  }
               }
            }
         }
      }
      
      override public function draw() : void
      {
         var child:DisplayObject = null;
         _height = 0;
         _height += this.marginTop;
         var maxWidth:Number = 0;
         var ypos:Number = this.marginTop;
         for(var i:int = 0; i < numChildren; i++)
         {
            child = getChildAt(i);
            child.y = ypos + (!!child.hasOwnProperty("paddingTop") ? (child as Object).paddingTop : 0);
            ypos += child.height + (!!child.hasOwnProperty("paddingTop") ? (child as Object).paddingTop : 0) + (!!child.hasOwnProperty("paddingBottom") ? (child as Object).paddingBottom : 0) + this._spacing;
            _height += child.height + (!!child.hasOwnProperty("paddingTop") ? (child as Object).paddingTop : 0) + (!!child.hasOwnProperty("paddingBottom") ? (child as Object).paddingBottom : 0);
            if(this._alignment != JUSTIFY)
            {
               maxWidth = Math.max(maxWidth,this.marginLeft + child.width + (!!child.hasOwnProperty("paddingLeft") ? (child as Object).paddingLeft : 0) + (!!child.hasOwnProperty("paddingRight") ? (child as Object).paddingRight : 0) + this.marginRight);
            }
            else
            {
               _width = _width;
            }
         }
         if(this._alignment != JUSTIFY)
         {
            _width = maxWidth;
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

