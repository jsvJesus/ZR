package com.option
{
   import com.colorPicker.ColorEvent;
   import com.colorPicker.ColorList;
   import com.greensock.TweenMax;
   import com.greensock.easing.*;
   import flash.display.MovieClip;
   import flash.events.Event;
   import flash.events.MouseEvent;
   import scaleform.clik.controls.Button;
   
   public class ColorsBar extends MovieClip
   {
      public var CloseBtn:Button;
      
      public var background:MovieClip;
      
      public var colorList:ColorList = new ColorList();
      
      protected var X:Number;
      
      protected var _currentColor:uint;
      
      protected var _startColor:uint;
      
      protected var _currentIndex:Number;
      
      protected var _showed:Boolean = false;
      
      public function ColorsBar()
      {
         super();
         this.CloseBtn.addEventListener(MouseEvent.CLICK,this.onCloseColors);
         this.X = this.x;
         this.CloseBtn.visible = false;
         addChild(this.colorList);
      }
      
      public function set showed(value:Boolean) : *
      {
         this._showed = value;
         if(this._showed)
         {
            dispatchEvent(new Event("COLORS_SHOW"));
         }
         else
         {
            dispatchEvent(new Event("COLORS_HIDE"));
         }
      }
      
      public function get showed() : Boolean
      {
         return this._showed;
      }
      
      public function Draw(colors:Array) : *
      {
         this.colorList.Draw(colors,this.width - 30);
         this.colorList.addEventListener(ColorEvent.SELECT,this.onColorSelect);
         this.colorList.x = (this.background.width - this.colorList.width) / 2;
         this.colorList.y = (this.background.height - this.colorList.height) / 2;
      }
      
      public function Show(color:uint, index:Number) : *
      {
         this._startColor = color;
         this._currentColor = color;
         if(this._currentIndex != index)
         {
            if(!this.showed)
            {
               this.showed = true;
               this._currentIndex = index;
               this.colorList.setColor(color);
               this.visible = true;
               TweenMax.to(this,0.2,{
                  "x":this.X + this.width - 3,
                  "ease":Expo.easeInOut,
                  "onComplete":this.BtnVisible
               });
            }
            else
            {
               this.reShow(color,index);
            }
         }
         else
         {
            this.Hide();
         }
      }
      
      protected function BtnVisible() : *
      {
         this.CloseBtn.visible = true;
      }
      
      protected function reShow(color:uint, index:Number) : *
      {
         this._currentIndex = index;
         this.CloseBtn.visible = false;
         this.colorList.setColor(color);
         this.visible = true;
         this.showed = true;
         TweenMax.to(this,0.2,{
            "x":this.X,
            "ease":Expo.easeInOut,
            "onComplete":this.Show2
         });
      }
      
      public function Show2() : *
      {
         Object(root).validateColors();
         TweenMax.to(this,0.2,{
            "x":this.X + this.width - 3,
            "ease":Expo.easeInOut,
            "onComplete":this.BtnVisible
         });
         this.showed = true;
      }
      
      public function Hide() : *
      {
         Object(root).validateColors();
         this.CloseBtn.visible = false;
         TweenMax.to(this,0.2,{
            "x":this.X,
            "ease":Expo.easeInOut,
            "onComplete":this.Hidden
         });
      }
      
      protected function onCloseColors(e:MouseEvent) : *
      {
         this.Hide();
      }
      
      private function Hidden() : *
      {
         this.showed = false;
         this.visible = false;
         this._currentIndex = -1;
      }
      
      internal function onColorSelect(e:ColorEvent) : *
      {
         this._currentColor = e.color;
         dispatchEvent(new ColorEvent(ColorEvent.SELECT,this._currentColor));
      }
   }
}

