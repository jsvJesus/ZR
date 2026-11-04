package ui.components
{
   import com.dvalimona.components.*;
   import com.greensock.*;
   import com.greensock.easing.*;
   import flash.display.*;
   import flash.events.*;
   import flash.geom.*;
   import flash.utils.*;
   
   public class Callout extends Component
   {
      public static const DIRECTION_ANY:String = "any";
      
      public static const DIRECTION_VERTICAL:String = "vertical";
      
      public static const DIRECTION_HORIZONTAL:String = "horizontal";
      
      public static const DIRECTION_UP:String = "up";
      
      public static const DIRECTION_DOWN:String = "down";
      
      public static const DIRECTION_LEFT:String = "left";
      
      public static const DIRECTION_RIGHT:String = "right";
      
      public static const ARROW_POSITION_TOP:String = "top";
      
      public static const ARROW_POSITION_RIGHT:String = "right";
      
      public static const ARROW_POSITION_BOTTOM:String = "bottom";
      
      public static const ARROW_POSITION_LEFT:String = "left";
      
      private static const HELPER_RECT:Rectangle = new Rectangle();
      
      private static const HELPER_POINT:Point = new Point();
      
      private static const HELPER:Dictionary = new Dictionary();
      
      public static var stagePaddingTop:Number = 0;
      
      public static var stagePaddingRight:Number = 0;
      
      public static var stagePaddingBottom:Number = 0;
      
      public static var stagePaddingLeft:Number = 0;
      
      public static const borderWidth:Number = 4;
      
      public static var callouts:Array = [];
      
      protected const DELAY_TIME:int = 3000;
      
      public var closeOnClickOutside:Boolean = true;
      
      protected var timer:Timer;
      
      private var contentHolder:Sprite;
      
      private var back:Sprite;
      
      private var arrow:Sprite;
      
      private var label:Label;
      
      private var target:DisplayObjectContainer;
      
      private var content:DisplayObject;
      
      private var text:String;
      
      private var _fixedHeight:uint;
      
      private var _fixedWidth:uint;
      
      private var backColor:uint = 0;
      
      private var useTargetAsParent:Boolean;
      
      public function Callout(param1:DisplayObjectContainer, param2:String, param3:uint = 0, param4:uint = 0, param5:Boolean = true)
      {
         this.useTargetAsParent = param5;
         ClearInstances();
         this.text = param2;
         this.target = param1;
         this.fixedWidth = param3;
         this.fixedHeight = Math.max(param4,40);
         super();
         callouts.push(this);
         if(param5)
         {
            param1.addChild(this);
         }
         else
         {
            Base.stage.addChild(this);
         }
         this.resetTimer();
      }
      
      public static function get stagePadding() : Number
      {
         return Callout.stagePaddingTop;
      }
      
      public static function set stagePadding(param1:Number) : void
      {
         Callout.stagePaddingTop = param1;
         Callout.stagePaddingRight = param1;
         Callout.stagePaddingBottom = param1;
         Callout.stagePaddingLeft = param1;
      }
      
      public static function ClearInstances() : void
      {
         var _loc1_:Callout = null;
         if(callouts.length)
         {
            while(callouts.length)
            {
               _loc1_ = callouts.pop() as Callout;
               if(_loc1_.parent)
               {
                  _loc1_.parent.removeChild(_loc1_);
                  _loc1_ = null;
               }
               else
               {
                  _loc1_ = null;
               }
            }
         }
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
      
      protected function resetTimer() : void
      {
         if(this.timer != null)
         {
            this.timer.reset();
         }
         else
         {
            this.timer = new Timer(this.DELAY_TIME,1);
            this.timer.addEventListener(TimerEvent.TIMER_COMPLETE,this.onTimerComplete);
            this.timer.start();
         }
      }
      
      protected function onTimerComplete(param1:TimerEvent) : void
      {
         this.timer.removeEventListener(TimerEvent.TIMER_COMPLETE,this.onTimerComplete);
         this.timer.stop();
         this.timer = null;
         this.parent.removeChild(this);
      }
      
      override protected function init() : void
      {
         super.init();
      }
      
      override protected function addChildren() : void
      {
         var _loc1_:Point = null;
         var _loc2_:int = 0;
         this.alpha = 0;
         this.back = new Sprite();
         this.addChild(this.back);
         this.label = new Label(this);
         this.label.autoSize = true;
         this.label.text = this.text;
         this.label.size = 20;
         if(this.useTargetAsParent)
         {
            _loc1_ = new Point(0,0);
         }
         else
         {
            _loc1_ = this.target.localToGlobal(new Point(0,0));
         }
         this.height = Math.max(this.target.height,42);
         this.label.text = this.text;
         this.label.draw();
         this.label.x = 10;
         this.label.y = this.height / 2 - this.label.height / 2;
         this.label.draw();
         this.width = this.label.x + this.label.width + 10;
         if(this.useTargetAsParent)
         {
            _loc2_ = 10 + _loc1_.x + this.target.width;
            this.x = _loc2_;
            this.y = _loc1_.y;
         }
         else
         {
            _loc2_ = _loc1_.x + this.target.width;
            this.x = _loc2_;
            this.y = _loc1_.y;
         }
         this.x = _loc2_ + 30;
         this.alpha = 0;
         TweenMax.to(this,0.3,{
            "alpha":1,
            "ease":Expo.easeOut,
            "x":_loc2_,
            "onComplete":null
         });
      }
      
      override public function draw() : void
      {
         this.back.graphics.clear();
         this.back.graphics.beginFill(this.backColor,0.8);
         this.back.graphics.lineStyle(borderWidth,16777215,0.5,true,LineScaleMode.NONE,CapsStyle.SQUARE,JointStyle.MITER);
         this.back.graphics.drawRoundRect(0 + borderWidth / 2,0 + borderWidth / 2,width - borderWidth,height - borderWidth,0,0);
         this.back.graphics.endFill();
      }
   }
}

