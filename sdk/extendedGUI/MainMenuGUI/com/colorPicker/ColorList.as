package com.colorPicker
{
   import com.dvalimona.components.Component;
   import flash.display.*;
   import flash.system.*;
   
   public class ColorList extends Component
   {
      private var _colors:Object = new Object();
      
      private var allData:Array = new Array();
      
      private var colorList:Array = new Array();
      
      private var chanceList:Array = new Array();
      
      public var otherData:String = "";
      
      protected var background:Sprite = new Sprite();
      
      public function ColorList(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0)
      {
         super(parent,xpos,ypos);
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
      
      public function get allColors() : Array
      {
         return this.allData;
      }
      
      public function getColor() : uint
      {
         var i:* = undefined;
         for(i in this._colors)
         {
            if(this._colors[i].selected)
            {
               return this._colors[i].color;
            }
         }
         return 0;
      }
      
      override protected function addChildren() : void
      {
         super.addChildren();
      }
      
      private function clear() : void
      {
         while(this.numChildren > 0)
         {
            this.removeChildAt(0);
         }
      }
      
      public function getRandomColor(exception:Array = null, mode:String = "normal") : uint
      {
         var i:* = undefined;
         var current_chance:Number = Math.random() * 100;
         var selected_indexes:Array = new Array();
         var _tmp_chances:Array = new Array();
         for each(i in this.chanceList)
         {
            _tmp_chances.push(i);
         }
         if(exception != null)
         {
            switch(mode)
            {
               case "over":
                  for(i in this.colorList)
                  {
                     if(exception.indexOf(this.colorList[i]) != -1)
                     {
                        _tmp_chances[i] += 50;
                     }
                  }
                  break;
               case "below":
                  for(i in this.colorList)
                  {
                     if(exception.indexOf(this.colorList[i]) != -1)
                     {
                        _tmp_chances[i] = 0;
                     }
                  }
            }
         }
         for(i in _tmp_chances)
         {
            if(_tmp_chances[i] >= current_chance)
            {
               selected_indexes.push(i);
            }
         }
         if(mode != "normal")
         {
         }
         var selected_index:Number = Math.floor(Math.random() * selected_indexes.length);
         selected_index = Number(selected_indexes[selected_index]);
         selected_indexes = null;
         _tmp_chances = null;
         System.gc();
         return this.colorList[selected_index];
      }
      
      public function setData(arr:Array) : *
      {
         var color:uint = 0;
         var chance:Number = NaN;
         var str:String = null;
         var tmp:Array = null;
         var str_color:String = null;
         this.clear();
         this.background.graphics.clear();
         this.background.graphics.beginFill(3866392,0);
         this.background.graphics.drawRect(0,0,this.width,this.height);
         this.background.graphics.endFill();
         addChild(this.background);
         this.allData = arr;
         this.colorList = new Array();
         this.chanceList = new Array();
         var X:Number = 0;
         var Y:Number = 0;
         var t:Number = 1;
         for(var i:* = 0; i < arr.length; i++)
         {
            if(X + 24 >= this.width)
            {
               Y += 24 + 7;
               X = 0;
            }
            color = 0;
            chance = 0;
            str = arr[i].toString();
            tmp = str.split(":");
            color = uint(parseInt(tmp[0],16));
            if(tmp.length > 1)
            {
               chance = Number(parseFloat(tmp[1]));
            }
            this.colorList.push(color);
            this.chanceList.push(chance);
            str_color = color.toString(16);
            this._colors[str_color] = new ColorItem(null,X,Y);
            this._colors[str_color].width = 24;
            this._colors[str_color].height = 24;
            this._colors[str_color].color = color;
            this._colors[str_color].visible = true;
            this._colors[str_color].addEventListener(ColorEvent.SELECT,this.onClick);
            this.addChild(this._colors[str_color]);
            X += 24 + 7;
            t++;
         }
         this.height = Y + 24;
      }
      
      internal function onClick(e:ColorEvent) : *
      {
         dispatchEvent(new ColorEvent(ColorEvent.SELECT,e.color,this));
         this.setColor(e.color);
      }
   }
}

