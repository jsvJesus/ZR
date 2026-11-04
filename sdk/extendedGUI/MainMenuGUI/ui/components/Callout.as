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
      
      public function Callout(target:DisplayObjectContainer, text:String, fixedWidth:uint = 0, fixedHeight:uint = 0, useTargetAsParent:Boolean = true)
      {
         this.useTargetAsParent = useTargetAsParent;
         ClearInstances();
         this.text = text;
         this.target = target;
         this.fixedWidth = fixedWidth;
         this.fixedHeight = Math.max(fixedHeight,40);
         super();
         callouts.push(this);
         if(useTargetAsParent)
         {
            target.addChild(this);
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
      
      public static function set stagePadding(value:Number) : void
      {
         Callout.stagePaddingTop = value;
         Callout.stagePaddingRight = value;
         Callout.stagePaddingBottom = value;
         Callout.stagePaddingLeft = value;
      }
      
      public static function ClearInstances() : void
      {
         var oldCallout:Callout = null;
         if(callouts.length)
         {
            while(callouts.length)
            {
               oldCallout = callouts.pop() as Callout;
               if(oldCallout.parent)
               {
                  oldCallout.parent.removeChild(oldCallout);
                  oldCallout = null;
               }
               else
               {
                  oldCallout = null;
               }
            }
         }
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
      
      protected function onTimerComplete(event:TimerEvent) : void
      {
         this.timer.removeEventListener(TimerEvent.TIMER_COMPLETE,this.onTimerComplete);
         this.timer.stop();
         this.timer = null;
         if(this.parent != null)
         {
            this.parent.removeChild(this);
         }
      }
      
      override protected function init() : void
      {
         super.init();
      }
      
      override protected function addChildren() : void
      {
         var pnt:Point = null;
         var targetX:int = 0;
         this.alpha = 0;
         this.back = new Sprite();
         this.addChild(this.back);
         this.label = new Label(this);
         this.label.autoSize = true;
         this.label.text = this.text;
         this.label.size = 20;
         if(this.useTargetAsParent)
         {
            pnt = new Point(0,0);
         }
         else
         {
            pnt = this.target.localToGlobal(new Point(0,0));
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
            targetX = 10 + pnt.x + this.target.width;
            this.x = targetX;
            this.y = pnt.y;
         }
         else
         {
            targetX = pnt.x + this.target.width;
            this.x = targetX;
            this.y = pnt.y;
         }
         this.x = targetX + 30;
         this.alpha = 0;
         TweenMax.to(this,0.3,{
            "alpha":1,
            "ease":Expo.easeOut,
            "x":targetX,
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

