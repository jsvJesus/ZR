package Flags_fla
{
   import adobe.utils.*;
   import com.greensock.TweenMax;
   import com.greensock.easing.*;
   import flash.accessibility.*;
   import flash.desktop.*;
   import flash.display.*;
   import flash.errors.*;
   import flash.events.*;
   import flash.external.*;
   import flash.filters.*;
   import flash.geom.*;
   import flash.globalization.*;
   import flash.media.*;
   import flash.net.*;
   import flash.net.drm.*;
   import flash.printing.*;
   import flash.profiler.*;
   import flash.sampler.*;
   import flash.sensors.*;
   import flash.system.*;
   import flash.text.*;
   import flash.text.engine.*;
   import flash.text.ime.*;
   import flash.ui.*;
   import flash.utils.*;
   import flash.xml.*;
   import scaleform.clik.controls.Button1;
   
   public dynamic class MainTimeline extends MovieClip
   {
      public var base:MainBaseInterace;
      
      public var compact_state:Boolean;
      
      public var compactBtn:Button1;
      
      public var w:*;
      
      public var h:Number;
      
      public function MainTimeline()
      {
         super();
      }
      
      public function set_money(param1:*) : *
      {
         var _loc2_:Object = param1.flag;
         this.base.MainFlags.setMoneyIcon(_loc2_.cost,_loc2_.base_money,_loc2_.is_gold);
      }
      
      public function validNotification() : *
      {
         if(this.compact_state)
         {
            this.compactBtn._State = "notification";
         }
         else
         {
            this.compactBtn._State = "";
         }
         this.compactBtn.setState("up");
      }
      
      public function stopResize() : *
      {
         this.compactBtn.removeEventListener(MouseEvent.CLICK,this.onCompactClick);
      }
      
      public function startResize() : *
      {
         this.compactBtn.addEventListener(MouseEvent.CLICK,this.onCompactClick);
      }
      
      public function onCompactClick(param1:MouseEvent) : *
      {
         this.compact_state = !this.compact_state;
         ExternalInterface.call("compact_state",{"value":this.compact_state});
         this.base.alpha = 0;
      }
      
      public function onResize(param1:*) : *
      {
         var _loc8_:Number = NaN;
         var _loc9_:* = undefined;
         var _loc10_:Number = NaN;
         var _loc11_:* = undefined;
         if(param1.compact_state != null)
         {
            this.compact_state = param1.compact_state;
         }
         var _loc2_:Number = Number(param1.size[0]);
         var _loc3_:Number = Number(param1.size[1]);
         var _loc4_:Number = (Object(root).height + stage.stageHeight) / 2;
         var _loc5_:Number = (Object(root).width + stage.stageWidth) / 2;
         var _loc6_:Number = (Object(root).height - stage.stageHeight) / 2;
         var _loc7_:Number = 0;
         if((Object(root).width + stage.stageWidth) / 2 > 1920)
         {
            _loc7_ = (1920 - _loc2_) / 2;
         }
         else
         {
            _loc7_ = (Object(root).width - stage.stageWidth) / 2;
         }
         if(this.compact_state)
         {
            this.base.visible = false;
            this.compactBtn.label = "+";
         }
         else
         {
            this.compactBtn.label = "-";
            this.base.visible = true;
            this.validNotification();
            _loc8_ = this.base.width / this.base.height;
            _loc9_ = _loc8_ * param1.height;
            if(_loc9_ > param1.width)
            {
               this.base.width = param1.width;
               this.base.height = 1 / _loc8_ * param1.width;
            }
            else
            {
               this.base.height = param1.height;
               this.base.width = param1.height * _loc8_;
            }
            for(_loc11_ in param1)
            {
            }
            this.base.y = _loc6_ + _loc3_ * param1.corner[1] + 3;
            this.base.x = _loc7_ + _loc2_ * param1.corner[0] + param1.width / 2 - this.base.width / 2;
            TweenMax.to(this.base,0.2,{
               "alpha":1,
               "delay":0,
               "ease":Expo.easeIn
            });
         }
         this.compactBtn.y = _loc6_ + _loc3_ * param1.corner[1] + 7;
         this.compactBtn.x = _loc7_ + _loc2_ * param1.corner[0] + param1.width / 2 - this.compactBtn.width / 2;
      }
   }
}

