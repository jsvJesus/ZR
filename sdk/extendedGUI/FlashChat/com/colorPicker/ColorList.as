package com.colorPicker
{
   import flash.display.MovieClip;
   
   public class ColorList extends MovieClip
   {
      private var _colors:Object = new Object();
      
      public function ColorList()
      {
         super();
      }
      
      public function setColor(value:uint) : *
      {
         var i:* = undefined;
         if(this._colors[value.toString(16)] != null)
         {
            for(i in this._colors)
            {
               this._colors[i].selected = false;
            }
            this._colors[value.toString(16)].selected = true;
         }
      }
      
      public function Draw(arr:Array, limit:Number = 0) : *
      {
         var str:String = null;
         var X:Number = 0;
         var Y:Number = 0;
         for(var i:* = 0; i < arr.length; i++)
         {
            str = arr[i].toString(16);
            this._colors[str] = new ColorItem();
            this._colors[str].addEventListener(ColorEvent.SELECT,this.onClick);
            this._colors[str].x = X;
            this._colors[str].y = Y;
            addChild(this._colors[str]);
            this._colors[str].color = arr[i];
            X += 13;
            if(X >= limit)
            {
               Y += 13;
               X = 0;
            }
         }
      }
      
      internal function onClick(e:ColorEvent) : *
      {
         dispatchEvent(new ColorEvent(ColorEvent.SELECT,e.color));
         this.setColor(e.color);
      }
   }
}

