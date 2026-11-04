package com.captureTheFlag
{
   import com.GameCommunication;
   import com.greensock.easing.*;
   import flash.display.MovieClip;
   import flash.display.Sprite;
   import flash.events.MouseEvent;
   import flash.events.TimerEvent;
   import flash.external.ExternalInterface;
   import flash.text.TextField;
   import flash.text.TextFieldAutoSize;
   import flash.utils.Timer;
   import flash.utils.setTimeout;
   import scaleform.clik.controls.Button;
   
   public class CaptureTheFlag extends Sprite
   {
      protected var _baseName:String = "";
      
      protected var _owner:String = "";
      
      protected var _status:int;
      
      protected var _status_gui:int;
      
      protected var _timer:Timer;
      
      protected var _time_stop:Number = 0;
      
      protected var _time_duration:Number = 0;
      
      protected var _days:int = 0;
      
      protected var _hours:int = 0;
      
      protected var _mins:int = 0;
      
      protected var _secs:int = 0;
      
      protected const STATUS_FREE:* = 0;
      
      protected const STATUS_PACIFIC:* = 1;
      
      protected const STATUS_CAPTURE:* = 2;
      
      protected const STATUS_NORMAL:* = 0;
      
      protected const STATUS_BUY_FLAGS:* = 1;
      
      protected const STATUS_BUY_GUARDS:* = 2;
      
      protected const ICO_SIZE:* = 20;
      
      protected const ANIM_DURATION:* = 0.2;
      
      public var Loc:Object = {
         "FREE":"База свободна",
         "START_ATTACK":"до начала захвата",
         "END_ATTACK":"до окончания захвата",
         "STOP_BUY":"Завершить покупку",
         "DAY0":"день",
         "DAY1":"дня",
         "DAY2":"дней"
      };
      
      protected const ONE_SECOND:* = 1000;
      
      public var BaseName:TextField;
      
      public var Clock:TextField;
      
      public var BaseOwner:TextField;
      
      public var SubClockField:TextField;
      
      public var StopBuyBtn:Button;
      
      public var frameR:MovieClip;
      
      public var frameL:MovieClip;
      
      public var frameOwnR:MovieClip;
      
      public var frameOwnL:MovieClip;
      
      public var doubleMoneySymb:MovieClip;
      
      public var moneyText:TextField;
      
      public var ListFlags:FlagList = new FlagList();
      
      protected var API:GameCommunication = new GameCommunication();
      
      public function CaptureTheFlag()
      {
         this._status = this.STATUS_FREE;
         this._status_gui = this.STATUS_NORMAL;
         this._timer = new Timer(this.ONE_SECOND);
         super();
         this._timer.addEventListener(TimerEvent.TIMER,this.timerTick);
         this.StopBuyBtn.visible = false;
         this.moneyText.visible = false;
         this.doubleMoneySymb.visible = false;
         this.StopBuyBtn.focused = 0;
         this.frameR.visible = false;
         this.frameL.visible = false;
         this.BaseName.autoSize = TextFieldAutoSize.CENTER;
         this.BaseName.wordWrap = true;
         this.BaseOwner.autoSize = TextFieldAutoSize.CENTER;
         this.BaseOwner.wordWrap = true;
         this.StopBuyBtn.addEventListener(MouseEvent.CLICK,this.stop_buy);
         ExternalInterface.addCallback("fully_update_component",this.setBase);
         ExternalInterface.addCallback("flag_height",this.setFlagValue);
         ExternalInterface.addCallback("flag_clan_owner",this.setFlagOwner);
         ExternalInterface.addCallback("change_status",this.setStatus);
         ExternalInterface.addCallback("leave_base",this.leaveBase);
         ExternalInterface.addCallback("base_owner",this.setOwner);
         ExternalInterface.addCallback("start_flag_buying",this.startByingFlags);
         ExternalInterface.addCallback("stop_flag_buying",this.stopByingFlags);
         ExternalInterface.addCallback("localized_resource",this.setLocalizedResourse);
         ExternalInterface.addCallback("buy_flag",this.sayBuyingFlag);
         addChild(this.ListFlags);
         setTimeout(this.getLocalizedResourse,50);
      }
      
      public function setMoneyIcon(param1:String, param2:String, param3:Boolean) : *
      {
         this.moneyText.text = param1 + "\n" + param2;
         if(this.StopBuyBtn.visible)
         {
            this.moneyText.visible = true;
            this.doubleMoneySymb.visible = true;
         }
         this.doubleMoneySymb.setGold(param3);
      }
      
      protected function setLocalizedResourse(param1:*) : *
      {
         this.Loc = param1["CaptureTheFlag"];
         this.API.ready();
         this.draw();
      }
      
      protected function sayBuyingFlag(param1:*) : *
      {
         this.ListFlags.buyedFlag(param1.name,param1.success);
      }
      
      protected function getLocalizedResourse() : *
      {
         this.API.getLocal(["CaptureTheFlag"]);
      }
      
      protected function stop_buy(param1:MouseEvent) : *
      {
         this.StopBuyBtn.selected = false;
         this.StopBuyBtn.focused = 0;
         this.stopByingFlags();
         GameCommunication.come_back_to_earth();
         this._status_gui = this.STATUS_NORMAL;
      }
      
      protected function ClickBaseName(param1:MouseEvent) : *
      {
         GameCommunication.go_to_main_plan();
      }
      
      protected function startByingFlags(param1:*) : *
      {
         this._status_gui = this.STATUS_BUY_FLAGS;
         Object(root).stopResize();
         this.ListFlags.visible = true;
         this.ListFlags.ByedFlags(param1);
         this.StopBuyBtn.visible = true;
         this.moneyText.visible = true;
         this.doubleMoneySymb.visible = true;
         this.moneyText.y = this.ListFlags.y + this.ListFlags.height + 12;
         this.doubleMoneySymb.y = this.moneyText.y + 3;
         this.StopBuyBtn.y = this.moneyText.y + this.moneyText.height + 3;
         this.BaseName.addEventListener(MouseEvent.CLICK,this.ClickBaseName);
         this.BaseName.addEventListener(MouseEvent.MOUSE_OVER,this.overBaseName);
         this.BaseName.addEventListener(MouseEvent.MOUSE_OUT,this.outBaseName);
         this.frameR.visible = true;
         this.frameR.y = this.BaseName.y;
         this.frameR.x = this.BaseName.x + this.BaseName.width / 2 + this.BaseName.textWidth / 2 + 4;
         this.frameL.visible = true;
         this.frameL.y = this.BaseName.y;
         this.frameL.x = this.BaseName.x + this.BaseName.width / 2 - this.BaseName.textWidth / 2 - 4;
      }
      
      protected function stopByingFlags() : *
      {
         Object(root).startResize();
         this.BaseName.removeEventListener(MouseEvent.CLICK,this.ClickBaseName);
         this.BaseName.removeEventListener(MouseEvent.MOUSE_OVER,this.overBaseName);
         this.BaseName.removeEventListener(MouseEvent.MOUSE_OUT,this.outBaseName);
         this.ListFlags.visible = false;
         this.StopBuyBtn.visible = false;
         this.moneyText.visible = false;
         this.doubleMoneySymb.visible = false;
         this.frameR.visible = false;
         this.frameL.visible = false;
         this._status_gui = this.STATUS_NORMAL;
      }
      
      protected function outBaseName(param1:MouseEvent) : *
      {
         this.frameR.gotoAndStop("up");
         this.frameL.gotoAndStop("up");
      }
      
      protected function overBaseName(param1:MouseEvent) : *
      {
         this.frameR.gotoAndStop("over");
         this.frameL.gotoAndStop("over");
      }
      
      protected function setOwner(param1:String) : *
      {
         this._owner = param1;
         this.BaseOwner.text = param1;
         this.draw();
      }
      
      protected function leaveBase() : *
      {
         this.visible = false;
         this._timer.stop();
      }
      
      protected function set status(param1:int) : *
      {
         this._status = param1;
         Object(root).validNotification();
         this.draw();
      }
      
      public function setFlagValue(param1:*) : *
      {
         this.ListFlags.setFlagValue(param1.name,param1.height);
      }
      
      public function setFlagOwner(param1:*) : *
      {
         this.ListFlags.setFlagOwner(param1.name,param1.is_my_flag,param1.am_i_invader);
      }
      
      protected function setTime(param1:Number) : *
      {
         this.start(param1);
      }
      
      protected function setStatus(param1:*) : *
      {
         this.status = param1.state_code;
         this.setTime(param1.next_time);
      }
      
      public function setBase(param1:*) : *
      {
         this.visible = true;
         this.BaseOwner.text = param1.owner;
         this.BaseName.text = param1.name;
         var _loc2_:Date = new Date();
         this._time_duration = param1.next_time;
         this.ListFlags.flagsData(param1.flags);
         this.status = param1.state;
      }
      
      public function start(param1:Number) : *
      {
         var _loc2_:Date = new Date();
         this._time_stop = _loc2_.time / 1000 + param1;
         this._time_duration = param1;
         if(this._time_stop >= 0)
         {
            this.updateTimerText();
         }
         this._timer.start();
      }
      
      private function _getRussianDay(param1:String) : String
      {
         var _loc2_:String = " ";
         switch(param1)
         {
            case "0":
               _loc2_ = this.Loc.DAY2;
               break;
            case "1":
               _loc2_ = this.Loc.DAY0;
               break;
            case "2":
               _loc2_ = this.Loc.DAY1;
               break;
            case "3":
               _loc2_ = this.Loc.DAY1;
               break;
            case "4":
               _loc2_ = this.Loc.DAY1;
               break;
            case "5":
               _loc2_ = this.Loc.DAY2;
               break;
            case "6":
               _loc2_ = this.Loc.DAY2;
               break;
            case "7":
               _loc2_ = this.Loc.DAY2;
               break;
            case "8":
               _loc2_ = this.Loc.DAY2;
               break;
            case "9":
               _loc2_ = this.Loc.DAY2;
         }
         return _loc2_;
      }
      
      private function updateTimerText() : *
      {
         var _loc1_:String = null;
         var _loc2_:String = null;
         var _loc3_:String = null;
         var _loc5_:Number = NaN;
         var _loc4_:Date = new Date();
         _loc5_ = this._time_stop - _loc4_.time / 1000;
         if(_loc5_ <= 0)
         {
            this._timer.stop();
            _loc5_ = 0;
            stage.visible = false;
         }
         else
         {
            stage.visible = true;
         }
         this._days = _loc5_ / 60 / 60 / 24;
         if(this._days >= 1)
         {
            _loc3_ = this._days.toString();
            this.Clock.text = this._days.toString() + " " + this._getRussianDay(_loc3_.charAt(_loc3_.length - 1));
            return;
         }
         this._hours = _loc5_ / 60 / 60 % 24;
         this._mins = _loc5_ / 60 % 60;
         this._secs = _loc5_ % 60;
         if(this._mins.toString().length == 1)
         {
            _loc1_ = "0" + this._mins;
         }
         else
         {
            _loc1_ = this._mins.toString();
         }
         if(this._secs.toString().length == 1)
         {
            _loc2_ = "0" + this._secs;
         }
         else
         {
            _loc2_ = this._secs.toString();
         }
         this.Clock.text = this._days + this._hours + ":" + _loc1_ + ":" + _loc2_;
      }
      
      protected function timerTick(param1:TimerEvent) : *
      {
         this.updateTimerText();
      }
      
      public function Show() : *
      {
      }
      
      protected function draw() : *
      {
         var _loc1_:Number = 0;
         this.BaseName.y = _loc1_;
         _loc1_ += this.BaseName.height;
         this.BaseOwner.y = _loc1_;
         _loc1_ += this.BaseOwner.height;
         this.Clock.y = _loc1_;
         _loc1_ += this.Clock.height;
         this.frameOwnR.y = this.BaseOwner.y;
         this.frameOwnR.x = this.BaseOwner.x + this.BaseOwner.width / 2 + this.BaseOwner.textWidth / 2 + 16;
         this.frameOwnL.visible = true;
         this.frameOwnL.y = this.BaseOwner.y;
         this.frameOwnL.x = this.BaseOwner.x + this.BaseOwner.width / 2 - this.BaseOwner.textWidth / 2 - 16;
         if(this.frameOwnL.x < this.BaseOwner.x)
         {
            this.frameOwnL.x = this.BaseOwner.x;
         }
         if(this.frameOwnR.x > this.BaseOwner.x + this.BaseOwner.width)
         {
            this.frameOwnR.x = this.BaseOwner.x + this.BaseOwner.width;
         }
         this.frameOwnR.height = this.BaseOwner.height;
         this.frameOwnL.height = this.BaseOwner.height;
         this.frameR.height = this.BaseName.height;
         this.frameL.height = this.BaseName.height;
         switch(this._status)
         {
            case this.STATUS_FREE:
               this._timer.stop();
               this._time_stop = 0;
               this.Clock.text = this.Loc.FREE;
               this.SubClockField.text = "";
               this.SubClockField.visible = false;
               this.ListFlags.visible = false;
               break;
            case this.STATUS_PACIFIC:
               this.SubClockField.text = this.Loc.START_ATTACK;
               this.SubClockField.visible = true;
               this.SubClockField.y = _loc1_;
               _loc1_ += this.SubClockField.height;
               this.ListFlags.visible = false;
               this.start(this._time_duration);
               break;
            case this.STATUS_CAPTURE:
               this.SubClockField.text = this.Loc.END_ATTACK;
               this.SubClockField.visible = true;
               this.SubClockField.y = _loc1_;
               _loc1_ += this.SubClockField.height;
               this.ListFlags.visible = true;
               this.start(this._time_duration);
         }
         this.ListFlags.y = _loc1_ - 2;
         this.ListFlags.x = 14;
         _loc1_ += this.ListFlags.height;
         this.StopBuyBtn.y = _loc1_ + 14;
         this.StopBuyBtn.x = this.width / 2 - this.StopBuyBtn.width / 2;
         this.StopBuyBtn.label = this.Loc.STOP_BUY;
      }
      
      public function Hide() : *
      {
      }
   }
}

