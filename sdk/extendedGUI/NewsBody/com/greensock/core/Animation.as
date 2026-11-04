package com.greensock.core
{
   import flash.display.Shape;
   import flash.events.Event;
   import flash.utils.getTimer;
   
   public class Animation
   {
      public static var _rootFramesTimeline:SimpleTimeline;
      
      public static var _rootTimeline:SimpleTimeline;
      
      public static const version:String = "12.1.1";
      
      public static var ticker:Shape = new Shape();
      
      protected static var _rootFrame:Number = -1;
      
      protected static var _tickEvent:Event = new Event("tick");
      
      protected static var _tinyNum:Number = 1e-10;
      
      public var _delay:Number;
      
      public var _prev:Animation;
      
      public var _reversed:Boolean;
      
      public var _active:Boolean;
      
      public var _timeline:SimpleTimeline;
      
      public var _rawPrevTime:Number;
      
      public var data:*;
      
      public var vars:Object;
      
      public var _totalTime:Number;
      
      public var _time:Number;
      
      public var timeline:SimpleTimeline;
      
      public var _initted:Boolean;
      
      public var _paused:Boolean;
      
      public var _startTime:Number;
      
      public var _dirty:Boolean;
      
      public var _next:Animation;
      
      protected var _onUpdate:Function;
      
      public var _pauseTime:Number;
      
      public var _duration:Number;
      
      public var _totalDuration:Number;
      
      public var _gc:Boolean;
      
      public var _timeScale:Number;
      
      public function Animation(duration:Number = 0, vars:Object = null)
      {
         super();
         this.vars = vars || {};
         if(Boolean(this.vars._isGSVars))
         {
            this.vars = this.vars.vars;
         }
         _duration = _totalDuration = duration || 0;
         _delay = Number(this.vars.delay) || 0;
         _timeScale = 1;
         _totalTime = _time = 0;
         data = this.vars.data;
         _rawPrevTime = -1;
         if(_rootTimeline == null)
         {
            if(_rootFrame != -1)
            {
               return;
            }
            _rootFrame = 0;
            _rootFramesTimeline = new SimpleTimeline();
            _rootTimeline = new SimpleTimeline();
            _rootTimeline._startTime = getTimer() / 1000;
            _rootFramesTimeline._startTime = 0;
            _rootTimeline._active = _rootFramesTimeline._active = true;
            ticker.addEventListener("enterFrame",_updateRoot,false,0,true);
         }
         var tl:SimpleTimeline = Boolean(this.vars.useFrames) ? _rootFramesTimeline : _rootTimeline;
         tl.add(this,tl._time);
         _reversed = this.vars.reversed == true;
         if(Boolean(this.vars.paused))
         {
            paused(true);
         }
      }
      
      public static function _updateRoot(event:Event = null) : void
      {
         ++_rootFrame;
         _rootTimeline.render((getTimer() / 1000 - _rootTimeline._startTime) * _rootTimeline._timeScale,false,false);
         _rootFramesTimeline.render((_rootFrame - _rootFramesTimeline._startTime) * _rootFramesTimeline._timeScale,false,false);
         ticker.dispatchEvent(_tickEvent);
      }
      
      public function delay(value:Number = NaN) : *
      {
         if(!arguments.length)
         {
            return _delay;
         }
         if(_timeline.smoothChildTiming)
         {
            startTime(_startTime + value - _delay);
         }
         _delay = value;
         return this;
      }
      
      public function totalDuration(value:Number = NaN) : *
      {
         _dirty = false;
         return !arguments.length ? _totalDuration : duration(value);
      }
      
      public function _enabled(enabled:Boolean, ignoreTimeline:Boolean = false) : Boolean
      {
         _gc = !enabled;
         _active = Boolean(enabled && !_paused && _totalTime > 0 && _totalTime < _totalDuration);
         if(!ignoreTimeline)
         {
            if(enabled && timeline == null)
            {
               _timeline.add(this,_startTime - _delay);
            }
            else if(!enabled && timeline != null)
            {
               _timeline._remove(this,true);
            }
         }
         return false;
      }
      
      public function timeScale(value:Number = NaN) : *
      {
         var t:Number = NaN;
         if(!arguments.length)
         {
            return _timeScale;
         }
         value ||= 0.000001;
         if(Boolean(_timeline) && _timeline.smoothChildTiming)
         {
            t = Boolean(_pauseTime) || _pauseTime == 0 ? _pauseTime : _timeline._totalTime;
            _startTime = t - (t - _startTime) * _timeScale / value;
         }
         _timeScale = value;
         return _uncache(false);
      }
      
      protected function _swapSelfInParams(params:Array) : Array
      {
         var i:int = int(params.length);
         var copy:Array = params.concat();
         while(--i > -1)
         {
            if(params[i] === "{self}")
            {
               copy[i] = this;
            }
         }
         return copy;
      }
      
      public function totalProgress(value:Number = NaN, suppressEvents:Boolean = false) : *
      {
         return !arguments.length ? _time / duration() : totalTime(duration() * value,suppressEvents);
      }
      
      public function duration(value:Number = NaN) : *
      {
         if(!arguments.length)
         {
            _dirty = false;
            return _duration;
         }
         _duration = _totalDuration = value;
         _uncache(true);
         if(_timeline.smoothChildTiming)
         {
            if(_time > 0)
            {
               if(_time < _duration)
               {
                  if(value != 0)
                  {
                     totalTime(_totalTime * (value / _duration),true);
                  }
               }
            }
         }
         return this;
      }
      
      public function restart(includeDelay:Boolean = false, suppressEvents:Boolean = true) : *
      {
         reversed(false);
         paused(false);
         return totalTime(includeDelay ? -_delay : 0,suppressEvents,true);
      }
      
      public function render(time:Number, suppressEvents:Boolean = false, force:Boolean = false) : void
      {
      }
      
      public function resume(from:* = null, suppressEvents:Boolean = true) : *
      {
         if(from != null)
         {
            seek(from,suppressEvents);
         }
         return paused(false);
      }
      
      public function paused(value:Boolean = false) : *
      {
         var raw:Number = NaN;
         var elapsed:Number = NaN;
         if(!arguments.length)
         {
            return _paused;
         }
         if(value != _paused)
         {
            if(Boolean(_timeline))
            {
               raw = _timeline.rawTime();
               elapsed = raw - _pauseTime;
               if(!value && _timeline.smoothChildTiming)
               {
                  _startTime += elapsed;
                  _uncache(false);
               }
               _pauseTime = value ? raw : NaN;
               _paused = value;
               _active = !value && _totalTime > 0 && _totalTime < _totalDuration;
               if(!value && elapsed != 0 && _initted && duration() !== 0)
               {
                  render(_timeline.smoothChildTiming ? _totalTime : (raw - _startTime) / _timeScale,true,true);
               }
            }
         }
         if(_gc && !value)
         {
            _enabled(true,false);
         }
         return this;
      }
      
      public function totalTime(time:Number = NaN, suppressEvents:Boolean = false, uncapped:Boolean = false) : *
      {
         var tl:SimpleTimeline = null;
         if(!arguments.length)
         {
            return _totalTime;
         }
         if(Boolean(_timeline))
         {
            if(time < 0 && !uncapped)
            {
               time += totalDuration();
            }
            if(_timeline.smoothChildTiming)
            {
               if(_dirty)
               {
                  totalDuration();
               }
               if(time > _totalDuration && !uncapped)
               {
                  time = _totalDuration;
               }
               tl = _timeline;
               _startTime = (_paused ? _pauseTime : tl._time) - (!_reversed ? time : _totalDuration - time) / _timeScale;
               if(!_timeline._dirty)
               {
                  _uncache(false);
               }
               if(tl._timeline != null)
               {
                  while(Boolean(tl._timeline))
                  {
                     if(tl._timeline._time !== (tl._startTime + tl._totalTime) / tl._timeScale)
                     {
                        tl.totalTime(tl._totalTime,true);
                     }
                     tl = tl._timeline;
                  }
               }
            }
            if(_gc)
            {
               _enabled(true,false);
            }
            if(_totalTime != time || _duration === 0)
            {
               render(time,suppressEvents,false);
            }
         }
         return this;
      }
      
      public function play(from:* = null, suppressEvents:Boolean = true) : *
      {
         if(from != null)
         {
            seek(from,suppressEvents);
         }
         reversed(false);
         return paused(false);
      }
      
      public function invalidate() : *
      {
         return this;
      }
      
      public function progress(value:Number = NaN, suppressEvents:Boolean = false) : *
      {
         return !arguments.length ? _time / duration() : totalTime(duration() * value,suppressEvents);
      }
      
      public function _kill(vars:Object = null, target:Object = null) : Boolean
      {
         return _enabled(false,false);
      }
      
      public function reversed(value:Boolean = false) : *
      {
         if(!arguments.length)
         {
            return _reversed;
         }
         if(value != _reversed)
         {
            _reversed = value;
            totalTime(Boolean(_timeline) && !_timeline.smoothChildTiming ? totalDuration() - _totalTime : _totalTime,true);
         }
         return this;
      }
      
      public function startTime(value:Number = NaN) : *
      {
         if(!arguments.length)
         {
            return _startTime;
         }
         if(value != _startTime)
         {
            _startTime = value;
            if(Boolean(timeline))
            {
               if(timeline._sortChildren)
               {
                  timeline.add(this,value - _delay);
               }
            }
         }
         return this;
      }
      
      protected function _uncache(includeSelf:Boolean) : *
      {
         var tween:Animation = includeSelf ? this : timeline;
         while(Boolean(tween))
         {
            tween._dirty = true;
            tween = tween.timeline;
         }
         return this;
      }
      
      public function isActive() : Boolean
      {
         var rawTime:Number = NaN;
         var tl:SimpleTimeline = _timeline;
         return tl == null || !_gc && !_paused && tl.isActive() && (rawTime = tl.rawTime()) >= _startTime && rawTime < _startTime + totalDuration() / _timeScale;
      }
      
      public function time(value:Number = NaN, suppressEvents:Boolean = false) : *
      {
         if(!arguments.length)
         {
            return _time;
         }
         if(_dirty)
         {
            totalDuration();
         }
         if(value > _duration)
         {
            value = _duration;
         }
         return totalTime(value,suppressEvents);
      }
      
      public function kill(vars:Object = null, target:Object = null) : *
      {
         _kill(vars,target);
         return this;
      }
      
      public function reverse(from:* = null, suppressEvents:Boolean = true) : *
      {
         if(from != null)
         {
            seek(from || totalDuration(),suppressEvents);
         }
         reversed(true);
         return paused(false);
      }
      
      public function seek(time:*, suppressEvents:Boolean = true) : *
      {
         return totalTime(Number(time),suppressEvents);
      }
      
      public function pause(atTime:* = null, suppressEvents:Boolean = true) : *
      {
         if(atTime != null)
         {
            seek(atTime,suppressEvents);
         }
         return paused(true);
      }
      
      public function eventCallback(type:String, callback:Function = null, params:Array = null) : *
      {
         if(type == null)
         {
            return null;
         }
         if(type.substr(0,2) == "on")
         {
            if(arguments.length == 1)
            {
               return vars[type];
            }
            if(callback == null)
            {
               delete vars[type];
            }
            else
            {
               vars[type] = callback;
               vars[type + "Params"] = params is Array && params.join("").indexOf("{self}") !== -1 ? _swapSelfInParams(params) : params;
            }
            if(type == "onUpdate")
            {
               _onUpdate = callback;
            }
         }
         return this;
      }
   }
}

