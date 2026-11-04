package com.greensock
{
   import com.greensock.core.Animation;
   import com.greensock.easing.Ease;
   import com.greensock.events.TweenEvent;
   import flash.events.Event;
   import flash.events.EventDispatcher;
   import flash.events.IEventDispatcher;
   
   public class TimelineMax extends TimelineLite implements IEventDispatcher
   {
      public static const version:String = "12.1.5";
      
      protected static var _listenerLookup:Object = {
         "onCompleteListener":TweenEvent.COMPLETE,
         "onUpdateListener":TweenEvent.UPDATE,
         "onStartListener":TweenEvent.START,
         "onRepeatListener":TweenEvent.REPEAT,
         "onReverseCompleteListener":TweenEvent.REVERSE_COMPLETE
      };
      
      protected static var _easeNone:Ease = new Ease(null,null,1,0);
      
      protected var _dispatcher:EventDispatcher;
      
      protected var _yoyo:Boolean;
      
      protected var _hasUpdateListener:Boolean;
      
      protected var _cycle:int = 0;
      
      protected var _locked:Boolean;
      
      protected var _repeatDelay:Number;
      
      protected var _repeat:int;
      
      public function TimelineMax(vars:Object = null)
      {
         super(vars);
         _repeat = int(this.vars.repeat) || 0;
         _repeatDelay = Number(this.vars.repeatDelay) || 0;
         _yoyo = this.vars.yoyo == true;
         _dirty = true;
         if(Boolean(this.vars.onCompleteListener) || Boolean(this.vars.onUpdateListener) || Boolean(this.vars.onStartListener) || Boolean(this.vars.onRepeatListener) || Boolean(this.vars.onReverseCompleteListener))
         {
            _initDispatcher();
         }
      }
      
      protected static function _getGlobalPaused(tween:Animation) : Boolean
      {
         while(Boolean(tween))
         {
            if(tween._paused)
            {
               return true;
            }
            tween = tween._timeline;
         }
         return false;
      }
      
      public function dispatchEvent(event:Event) : Boolean
      {
         return _dispatcher == null ? false : _dispatcher.dispatchEvent(event);
      }
      
      public function currentLabel(value:String = null) : *
      {
         if(!arguments.length)
         {
            return getLabelBefore(_time + 1e-8);
         }
         return seek(value,true);
      }
      
      public function hasEventListener(type:String) : Boolean
      {
         return _dispatcher == null ? false : _dispatcher.hasEventListener(type);
      }
      
      public function removeEventListener(type:String, listener:Function, useCapture:Boolean = false) : void
      {
         if(_dispatcher != null)
         {
            _dispatcher.removeEventListener(type,listener,useCapture);
         }
      }
      
      public function addCallback(callback:Function, position:*, params:Array = null) : TimelineMax
      {
         return add(TweenLite.delayedCall(0,callback,params),position) as TimelineMax;
      }
      
      public function tweenFromTo(fromPosition:*, toPosition:*, vars:Object = null) : TweenLite
      {
         vars ||= {};
         fromPosition = _parseTimeOrLabel(fromPosition);
         vars.startAt = {
            "onComplete":seek,
            "onCompleteParams":[fromPosition]
         };
         vars.immediateRender = vars.immediateRender !== false;
         var t:TweenLite = tweenTo(toPosition,vars);
         return t.duration(Math.abs(t.vars.time - fromPosition) / _timeScale || 0.001) as TweenLite;
      }
      
      public function addEventListener(type:String, listener:Function, useCapture:Boolean = false, priority:int = 0, useWeakReference:Boolean = false) : void
      {
         if(_dispatcher == null)
         {
            _dispatcher = new EventDispatcher(this);
         }
         if(type == TweenEvent.UPDATE)
         {
            _hasUpdateListener = true;
         }
         _dispatcher.addEventListener(type,listener,useCapture,priority,useWeakReference);
      }
      
      public function tweenTo(position:*, vars:Object = null) : TweenLite
      {
         var p:String = null;
         var duration:Number = NaN;
         var t:TweenLite = null;
         vars ||= {};
         var copy:Object = {
            "ease":_easeNone,
            "overwrite":(Boolean(vars.delay) ? 2 : 1),
            "useFrames":usesFrames(),
            "immediateRender":false
         };
         for(p in vars)
         {
            copy[p] = vars[p];
         }
         copy.time = _parseTimeOrLabel(position);
         duration = Number(Math.abs(Number(copy.time) - _time) / _timeScale || 0.001);
         t = new TweenLite(this,duration,copy);
         copy.onStart = function():void
         {
            t.target.paused(true);
            if(t.vars.time != t.target.time() && duration === t.duration())
            {
               t.duration(Math.abs(t.vars.time - t.target.time()) / t.target._timeScale);
            }
            if(Boolean(vars.onStart))
            {
               vars.onStart.apply(null,vars.onStartParams);
            }
         };
         return t;
      }
      
      public function repeat(value:Number = 0) : *
      {
         if(!arguments.length)
         {
            return _repeat;
         }
         _repeat = value;
         return _uncache(true);
      }
      
      public function getLabelBefore(time:Number = NaN) : String
      {
         if(!time)
         {
            if(time != 0)
            {
               time = _time;
            }
         }
         var labels:Array = getLabelsArray();
         var i:int = int(labels.length);
         while(--i > -1)
         {
            if(labels[i].time < time)
            {
               return labels[i].name;
            }
         }
         return null;
      }
      
      public function willTrigger(type:String) : Boolean
      {
         return _dispatcher == null ? false : _dispatcher.willTrigger(type);
      }
      
      override public function totalProgress(value:Number = NaN, suppressEvents:Boolean = true) : *
      {
         return !arguments.length ? _totalTime / totalDuration() : totalTime(totalDuration() * value,suppressEvents);
      }
      
      public function getLabelsArray() : Array
      {
         var p:String = null;
         var a:Array = [];
         var cnt:int = 0;
         for(p in _labels)
         {
            var _loc6_:* = cnt++;
            a[_loc6_] = {
               "time":_labels[p],
               "name":p
            };
         }
         a.sortOn("time",Array.NUMERIC);
         return a;
      }
      
      override public function render(time:Number, suppressEvents:Boolean = false, force:Boolean = false) : void
      {
         var tween:Animation = null;
         var isComplete:Boolean = false;
         var next:Animation = null;
         var dur:Number = NaN;
         var callback:String = null;
         var internalForce:Boolean = false;
         var cycleDuration:Number = NaN;
         var backwards:Boolean = false;
         var wrap:Boolean = false;
         var recTotalTime:Number = NaN;
         var recCycle:int = 0;
         var recRawPrevTime:Number = NaN;
         var recTime:Number = NaN;
         if(_gc)
         {
            _enabled(true,false);
         }
         var totalDur:Number = !_dirty ? _totalDuration : totalDuration();
         var prevTime:Number = _time;
         var prevTotalTime:Number = _totalTime;
         var prevStart:Number = _startTime;
         var prevTimeScale:Number = _timeScale;
         var prevRawPrevTime:Number = _rawPrevTime;
         var prevPaused:Boolean = _paused;
         var prevCycle:int = _cycle;
         if(time >= totalDur)
         {
            if(!_locked)
            {
               _totalTime = totalDur;
               _cycle = _repeat;
            }
            if(!_reversed)
            {
               if(!_hasPausedChild())
               {
                  isComplete = true;
                  callback = "onComplete";
                  if(_duration === 0)
                  {
                     if(time === 0 || _rawPrevTime < 0 || _rawPrevTime === _tinyNum)
                     {
                        if(_rawPrevTime !== time && _first != null)
                        {
                           internalForce = true;
                           if(_rawPrevTime > _tinyNum)
                           {
                              callback = "onReverseComplete";
                           }
                        }
                     }
                  }
               }
            }
            _rawPrevTime = _duration || !suppressEvents || time !== 0 || _rawPrevTime === time ? time : _tinyNum;
            if(_yoyo && (_cycle & 1) != 0)
            {
               _time = time = 0;
            }
            else
            {
               _time = _duration;
               time = _duration + 0.0001;
            }
         }
         else if(time < 1e-7)
         {
            if(!_locked)
            {
               _totalTime = _cycle = 0;
            }
            _time = 0;
            if(prevTime !== 0 || _duration === 0 && _rawPrevTime !== _tinyNum && (_rawPrevTime > 0 || time < 0 && _rawPrevTime >= 0) && !_locked)
            {
               callback = "onReverseComplete";
               isComplete = _reversed;
            }
            if(time < 0)
            {
               _active = false;
               if(_rawPrevTime >= 0 && Boolean(_first))
               {
                  internalForce = true;
               }
               _rawPrevTime = time;
            }
            else
            {
               _rawPrevTime = _duration || !suppressEvents || time !== 0 || _rawPrevTime === time ? time : _tinyNum;
               time = 0;
               if(!_initted)
               {
                  internalForce = true;
               }
            }
         }
         else
         {
            if(_duration === 0 && _rawPrevTime < 0)
            {
               internalForce = true;
            }
            _time = _rawPrevTime = time;
            if(!_locked)
            {
               _totalTime = time;
               if(_repeat != 0)
               {
                  cycleDuration = _duration + _repeatDelay;
                  _cycle = _totalTime / cycleDuration >> 0;
                  if(_cycle !== 0)
                  {
                     if(_cycle === _totalTime / cycleDuration)
                     {
                        --_cycle;
                     }
                  }
                  _time = _totalTime - _cycle * cycleDuration;
                  if(_yoyo)
                  {
                     if((_cycle & 1) != 0)
                     {
                        _time = _duration - _time;
                     }
                  }
                  if(_time > _duration)
                  {
                     _time = _duration;
                     time = _duration + 0.0001;
                  }
                  else if(_time < 0)
                  {
                     _time = time = 0;
                  }
                  else
                  {
                     time = _time;
                  }
               }
            }
         }
         if(_cycle != prevCycle)
         {
            if(!_locked)
            {
               backwards = _yoyo && (prevCycle & 1) !== 0;
               wrap = backwards == (_yoyo && (_cycle & 1) !== 0);
               recTotalTime = _totalTime;
               recCycle = _cycle;
               recRawPrevTime = _rawPrevTime;
               recTime = _time;
               _totalTime = prevCycle * _duration;
               if(_cycle < prevCycle)
               {
                  backwards = !backwards;
               }
               else
               {
                  _totalTime += _duration;
               }
               _time = prevTime;
               _rawPrevTime = prevRawPrevTime;
               _cycle = prevCycle;
               _locked = true;
               prevTime = backwards ? 0 : _duration;
               render(prevTime,suppressEvents,false);
               if(!suppressEvents)
               {
                  if(!_gc)
                  {
                     if(Boolean(vars.onRepeat))
                     {
                        vars.onRepeat.apply(null,vars.onRepeatParams);
                     }
                     if(Boolean(_dispatcher))
                     {
                        _dispatcher.dispatchEvent(new TweenEvent(TweenEvent.REPEAT));
                     }
                  }
               }
               if(wrap)
               {
                  prevTime = backwards ? _duration + 0.0001 : -0.0001;
                  render(prevTime,true,false);
               }
               _locked = false;
               if(_paused && !prevPaused)
               {
                  return;
               }
               _time = recTime;
               _totalTime = recTotalTime;
               _cycle = recCycle;
               _rawPrevTime = recRawPrevTime;
            }
         }
         if((_time == prevTime || !_first) && !force && !internalForce)
         {
            if(prevTotalTime !== _totalTime)
            {
               if(_onUpdate != null)
               {
                  if(!suppressEvents)
                  {
                     _onUpdate.apply(vars.onUpdateScope || this,vars.onUpdateParams);
                  }
               }
            }
            return;
         }
         if(!_initted)
         {
            _initted = true;
         }
         if(!_active)
         {
            if(!_paused && _totalTime !== prevTotalTime && time > 0)
            {
               _active = true;
            }
         }
         if(prevTotalTime == 0)
         {
            if(_totalTime != 0)
            {
               if(!suppressEvents)
               {
                  if(Boolean(vars.onStart))
                  {
                     vars.onStart.apply(this,vars.onStartParams);
                  }
                  if(Boolean(_dispatcher))
                  {
                     _dispatcher.dispatchEvent(new TweenEvent(TweenEvent.START));
                  }
               }
            }
         }
         if(_time >= prevTime)
         {
            tween = _first;
            while(Boolean(tween))
            {
               next = tween._next;
               if(_paused && !prevPaused)
               {
                  break;
               }
               if(tween._active || tween._startTime <= _time && !tween._paused && !tween._gc)
               {
                  if(!tween._reversed)
                  {
                     tween.render((time - tween._startTime) * tween._timeScale,suppressEvents,force);
                  }
                  else
                  {
                     tween.render((!tween._dirty ? tween._totalDuration : tween.totalDuration()) - (time - tween._startTime) * tween._timeScale,suppressEvents,force);
                  }
               }
               tween = next;
            }
         }
         else
         {
            tween = _last;
            while(Boolean(tween))
            {
               next = tween._prev;
               if(_paused && !prevPaused)
               {
                  break;
               }
               if(tween._active || tween._startTime <= prevTime && !tween._paused && !tween._gc)
               {
                  if(!tween._reversed)
                  {
                     tween.render((time - tween._startTime) * tween._timeScale,suppressEvents,force);
                  }
                  else
                  {
                     tween.render((!tween._dirty ? tween._totalDuration : tween.totalDuration()) - (time - tween._startTime) * tween._timeScale,suppressEvents,force);
                  }
               }
               tween = next;
            }
         }
         if(_onUpdate != null)
         {
            if(!suppressEvents)
            {
               _onUpdate.apply(null,vars.onUpdateParams);
            }
         }
         if(_hasUpdateListener)
         {
            if(!suppressEvents)
            {
               _dispatcher.dispatchEvent(new TweenEvent(TweenEvent.UPDATE));
            }
         }
         if(Boolean(callback))
         {
            if(!_locked)
            {
               if(!_gc)
               {
                  if(prevStart === _startTime || prevTimeScale !== _timeScale)
                  {
                     if(_time === 0 || totalDur >= totalDuration())
                     {
                        if(isComplete)
                        {
                           if(_timeline.autoRemoveChildren)
                           {
                              _enabled(false,false);
                           }
                           _active = false;
                        }
                        if(!suppressEvents)
                        {
                           if(Boolean(vars[callback]))
                           {
                              vars[callback].apply(null,vars[callback + "Params"]);
                           }
                           if(Boolean(_dispatcher))
                           {
                              _dispatcher.dispatchEvent(new TweenEvent(callback == "onComplete" ? TweenEvent.COMPLETE : TweenEvent.REVERSE_COMPLETE));
                           }
                        }
                     }
                  }
               }
            }
         }
      }
      
      public function removeCallback(callback:Function, position:* = null) : TimelineMax
      {
         var a:Array = null;
         var i:int = 0;
         var time:Number = NaN;
         if(callback != null)
         {
            if(position == null)
            {
               _kill(null,callback);
            }
            else
            {
               a = getTweensOf(callback,false);
               i = int(a.length);
               time = _parseTimeOrLabel(position);
               while(--i > -1)
               {
                  if(a[i]._startTime === time)
                  {
                     a[i]._enabled(false,false);
                  }
               }
            }
         }
         return this;
      }
      
      public function yoyo(value:Boolean = false) : *
      {
         if(!arguments.length)
         {
            return _yoyo;
         }
         _yoyo = value;
         return this;
      }
      
      override public function progress(value:Number = NaN, suppressEvents:Boolean = false) : *
      {
         return !arguments.length ? _time / duration() : totalTime(duration() * (_yoyo && (_cycle & 1) !== 0 ? 1 - value : value) + _cycle * (_duration + _repeatDelay),suppressEvents);
      }
      
      public function repeatDelay(value:Number = 0) : *
      {
         if(!arguments.length)
         {
            return _repeatDelay;
         }
         _repeatDelay = value;
         return _uncache(true);
      }
      
      override public function time(value:Number = NaN, suppressEvents:Boolean = false) : *
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
         if(_yoyo && (_cycle & 1) !== 0)
         {
            value = _duration - value + _cycle * (_duration + _repeatDelay);
         }
         else if(_repeat != 0)
         {
            value += _cycle * (_duration + _repeatDelay);
         }
         return totalTime(value,suppressEvents);
      }
      
      protected function _initDispatcher() : Boolean
      {
         var p:String = null;
         var found:Boolean = false;
         for(p in _listenerLookup)
         {
            if(p in vars)
            {
               if(vars[p] is Function)
               {
                  if(_dispatcher == null)
                  {
                     _dispatcher = new EventDispatcher(this);
                  }
                  _dispatcher.addEventListener(_listenerLookup[p],vars[p],false,0,true);
                  found = true;
               }
            }
         }
         return found;
      }
      
      override public function invalidate() : *
      {
         _yoyo = Boolean(this.vars.yoyo == true);
         _repeat = int(this.vars.repeat) || 0;
         _repeatDelay = Number(this.vars.repeatDelay) || 0;
         _hasUpdateListener = false;
         _initDispatcher();
         _uncache(true);
         return super.invalidate();
      }
      
      public function getActive(nested:Boolean = true, tweens:Boolean = true, timelines:Boolean = false) : Array
      {
         var i:int = 0;
         var tween:Animation = null;
         var a:Array = [];
         var all:Array = getChildren(nested,tweens,timelines);
         var cnt:int = 0;
         var l:int = int(all.length);
         for(i = 0; i < l; i++)
         {
            tween = all[i];
            if(!tween._paused)
            {
               if(tween._timeline._time >= tween._startTime)
               {
                  if(tween._timeline._time < tween._startTime + tween._totalDuration / tween._timeScale)
                  {
                     if(!_getGlobalPaused(tween._timeline))
                     {
                        var _loc10_:* = cnt++;
                        a[_loc10_] = tween;
                     }
                  }
               }
            }
         }
         return a;
      }
      
      public function getLabelAfter(time:Number = NaN) : String
      {
         var i:int = 0;
         if(!time)
         {
            if(time != 0)
            {
               time = _time;
            }
         }
         var labels:Array = getLabelsArray();
         var l:int = int(labels.length);
         for(i = 0; i < l; i++)
         {
            if(labels[i].time > time)
            {
               return labels[i].name;
            }
         }
         return null;
      }
      
      override public function totalDuration(value:Number = NaN) : *
      {
         if(!arguments.length)
         {
            if(_dirty)
            {
               super.totalDuration();
               _totalDuration = _repeat == -1 ? 999999999999 : _duration * (_repeat + 1) + _repeatDelay * _repeat;
            }
            return _totalDuration;
         }
         return _repeat == -1 ? this : duration((value - _repeat * _repeatDelay) / (_repeat + 1));
      }
   }
}

