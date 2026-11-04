package com.captureTheFlag
{
   import com.GameCommunication;
   import flash.display.MovieClip;
   import flash.events.MouseEvent;
   import flash.text.TextField;
   import flash.text.TextFormat;
   import scaleform.clik.controls.Button;
   
   public class CircleFlagIndicator extends MovieClip
   {
      protected const NOT_MY_COLOR:* = 7368816;
      
      protected const MY_COLOR:* = 16777215;
      
      protected const VALUE_PERCENT:* = 0.8;
      
      protected const SIZE:* = 50;
      
      protected var _minimum:Number = 0;
      
      protected var _maximum:Number = 100;
      
      protected var _value:Number = 0;
      
      protected var _isMy:Boolean = false;
      
      protected var _am_i_invader:Boolean = false;
      
      protected var _id:String = "null";
      
      protected var _state:String = "onPlay";
      
      public var background:MovieClip;
      
      public var flagMyBar:CircleProgressBar;
      
      public var flagNotMyBar:CircleProgressBar;
      
      public var Name:TextField;
      
      public var buyBtn:Button;
      
      private var flag100:Boolean = false;
      
      public function CircleFlagIndicator()
      {
         super();
         this.flagMyBar = new CircleProgressBar(this.SIZE,this.SIZE,this.SIZE / 8,this.MY_COLOR,0.2,0);
         this.flagNotMyBar = new CircleProgressBar(this.SIZE,this.SIZE,this.SIZE / 8,this.NOT_MY_COLOR,0.2,0);
         this.flagNotMyBar.rotation = -90;
         this.flagMyBar.rotation = -90;
         this.flagNotMyBar.y = this.height;
         this.flagMyBar.y = this.height;
         this.flagNotMyBar.x = 0;
         this.flagMyBar.x = 0;
         this.background.graphics.clear();
         addChild(this.flagNotMyBar);
         addChild(this.flagMyBar);
      }
      
      public function set myFlag(param1:Boolean) : *
      {
         var _loc2_:TextFormat = new TextFormat();
         if(param1)
         {
            _loc2_.color = this.MY_COLOR;
         }
         else
         {
            _loc2_.color = this.NOT_MY_COLOR;
         }
         this.Name.setTextFormat(_loc2_);
         this._isMy = param1;
         this.updateFlagThumb();
      }
      
      public function get myFlag() : Boolean
      {
         return this._isMy;
      }
      
      public function set id(param1:String) : *
      {
         this._id = param1;
      }
      
      public function get id() : String
      {
         return this._id;
      }
      
      public function set value(param1:Number) : *
      {
         this._value = param1;
         this.updateFlagThumb();
      }
      
      public function get value() : Number
      {
         return this._value;
      }
      
      public function set flagName(param1:String) : *
      {
         this.Name.text = param1;
      }
      
      public function get flagName() : String
      {
         return this.Name.text;
      }
      
      public function set am_i_invader(param1:Boolean) : *
      {
         var _loc2_:Error = null;
         if(this.flagName == "E")
         {
         }
         if(this._am_i_invader && !param1)
         {
            _loc2_ = new Error();
         }
         this._am_i_invader = param1;
         this.updateFlagThumb();
      }
      
      public function get am_i_invader() : Boolean
      {
         return this._am_i_invader;
      }
      
      protected function onClick(param1:MouseEvent) : *
      {
      }
      
      protected function updateFlagThumb() : *
      {
         var _loc1_:Number = (this._value - this._minimum) / (this._maximum - this._minimum) * 100;
         var _loc2_:Error = new Error();
         if(this.flagName == "E")
         {
         }
         if(this.am_i_invader)
         {
            setChildIndex(this.background,0);
            setChildIndex(this.flagMyBar,0);
            setChildIndex(this.flagNotMyBar,0);
            this.background.graphics.clear();
            this.background.graphics.lineStyle(1,268435455);
            this.background.graphics.drawCircle(this.SIZE,this.SIZE,this.SIZE * 3 / 4);
            this.flagMyBar.value = _loc1_;
            this.flagNotMyBar.value = 0;
         }
         else
         {
            this.background.graphics.clear();
            this.background.graphics.lineStyle(1,3223857);
            this.background.graphics.drawCircle(this.SIZE,this.SIZE,this.SIZE * 3 / 4);
            setChildIndex(this.background,0);
            setChildIndex(this.flagNotMyBar,0);
            setChildIndex(this.flagMyBar,0);
            this.flagMyBar.value = 0;
            this.flagNotMyBar.value = _loc1_;
         }
      }
      
      public function onStatePlay() : *
      {
         gotoAndStop("onPlay");
      }
      
      public function addBuy() : *
      {
         this.background.graphics.clear();
         gotoAndStop("onBuy");
         this.buyBtn.addEventListener(MouseEvent.CLICK,this.onBuyClick);
      }
      
      public function addShow() : *
      {
         this.Name.addEventListener(MouseEvent.CLICK,this.onShowClick);
      }
      
      protected function onShowClick(param1:MouseEvent) : *
      {
         GameCommunication.goToFlag(this.flagName);
      }
      
      protected function onBuyClick(param1:MouseEvent) : *
      {
         this.buyBtn.removeEventListener(MouseEvent.CLICK,this.onBuyClick);
         GameCommunication.buyFlag(this.flagName);
      }
      
      public function invalidate() : *
      {
         this.updateFlagThumb();
      }
   }
}

