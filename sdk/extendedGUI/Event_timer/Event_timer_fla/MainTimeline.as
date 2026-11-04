package Event_timer_fla
{
   import adobe.utils.*;
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
   
   public dynamic class MainTimeline extends MovieClip
   {
      public var additionalText:TextField;
      
      public var failText:TextField;
      
      public var mainText:TextField;
      
      public var Local:Object;
      
      public var timer:Timer;
      
      public var _time_stop:Number;
      
      public var _time_duration:Number;
      
      public var _days:int;
      
      public var _hours:int;
      
      public var _mins:int;
      
      public var _secs:int;
      
      public var mode:Boolean;
      
      public var is_win:Boolean;
      
      public function MainTimeline()
      {
         super();
      }
      
      public function onResizeScreen(event:Event) : *
      {
         this.mainText.y = (Object(root).height - stage.stageHeight) / 2;
         this.failText.y = this.mainText.y;
         this.additionalText.y = (Object(root).height - stage.stageHeight) / 2 + 158;
      }
      
      public function set_local(arg:*) : *
      {
         this.Local = arg;
         this.mainText.text = arg.success;
         this.failText.text = arg.fail;
      }
      
      public function start(time_stop:Number, is_win:Boolean) : *
      {
         var date:Date = new Date();
         this.mainText.visible = is_win;
         this.failText.visible = !is_win;
         this._time_stop = date.time / 1000 + time_stop;
         this._time_duration = time_stop;
         if(this._time_stop >= 0)
         {
            this.additionalText.visible = true;
            this.updateTimerText();
         }
         this.timer.start();
      }
      
      public function set_timer(arg:*) : *
      {
         this.mode = arg.event_mode;
         this.is_win = arg.is_win;
         trace("set_timer",arg.time,this.is_win,this.mode);
         if(arg.time <= 0)
         {
            this.timer.stop();
            this.additionalText.visible = false;
            this.mainText.visible = this.is_win;
            this.failText.visible = !this.is_win;
            return;
         }
         this.start(arg.time,this.is_win);
      }
      
      public function timerTick(e:TimerEvent) : *
      {
         this.updateTimerText();
      }
      
      public function ready() : *
      {
         ExternalInterface.call("ready",{});
      }
      
      public function onTimerStop() : *
      {
         this.timer.stop();
         ExternalInterface.call("on_timer_stop",{});
      }
      
      public function updateTimerText() : *
      {
         var sMin:String = null;
         var sSec:String = null;
         var sDay:String = null;
         var current_time:Number = NaN;
         var date:Date = new Date();
         current_time = this._time_stop - date.time / 1000;
         if(current_time <= 0)
         {
            current_time = 0;
            this.additionalText.visible = false;
            this.onTimerStop();
         }
         this._hours = current_time / 60 / 60 % 24;
         this._mins = current_time / 60 % 60;
         this._secs = current_time % 60;
         if(this._mins.toString().length == 1)
         {
            sMin = "0" + this._mins;
         }
         else
         {
            sMin = this._mins.toString();
         }
         if(this._secs.toString().length == 1)
         {
            sSec = "0" + this._secs;
         }
         else
         {
            sSec = this._secs.toString();
         }
         if(this.Local.end_timer_text == null)
         {
            this.additionalText.text = "";
            return;
         }
         if(this.mode)
         {
            if(this.is_win)
            {
               this.additionalText.text = this.Local.dung_success + " " + sMin.toString() + ":" + sSec.toString();
            }
            else
            {
               this.additionalText.text = this.Local.dung_fail;
            }
         }
         else
         {
            this.additionalText.text = this.Local.end_timer_text + " " + sMin.toString() + ":" + sSec.toString();
         }
      }
   }
}

