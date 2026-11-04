package com.greensock
{
   import com.greensock.core.Animation;
   import com.greensock.easing.*;
   import com.greensock.events.*;
   import flash.events.*;
   
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
      
      protected var _repeat:int;
      
      protected var _repeatDelay:Number;
      
      protected var _cycle:int = 0;
      
      protected var _locked:Boolean;
      
      protected var _dispatcher:EventDispatcher;
      
      protected var _hasUpdateListener:Boolean;
      
      protected var _yoyo:Boolean;
      
      public function TimelineMax(vars:Object = null)
      {
         super(vars);
         this._repeat = int(this.vars.repeat) || 0;
         this._repeatDelay = Number(this.vars.repeatDelay) || 0;
         this._yoyo = this.vars.yoyo == true;
         _dirty = true;
         if(Boolean(this.vars.onCompleteListener) || Boolean(this.vars.onUpdateListener) || Boolean(this.vars.onStartListener) || Boolean(this.vars.onRepeatListener) || Boolean(this.vars.onReverseCompleteListener))
         {
            this._initDispatcher();
         }
      }
      
      protected static function _getGlobalPaused(tween:Animation) : Boolean
      {
         while(tween)
         {
            if(tween._paused)
            {
               return true;
            }
            tween = tween._timeline;
         }
         return false;
      }
      
      override public function invalidate() : *
      {
         this._yoyo = Boolean(this.vars.yoyo == true);
         this._repeat = int(this.vars.repeat) || 0;
         this._repeatDelay = Number(this.vars.repeatDelay) || 0;
         this._hasUpdateListener = false;
         this._initDispatcher();
         _uncache(true);
         return super.invalidate();
      }
      
      public function addCallback(callback:Function, position:*, params:Array = null) : TimelineMax
      {
         return add(TweenLite.delayedCall(0,callback,params),position) as TimelineMax;
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
      
      public function tweenTo(position:*, vars:Object = null) : TweenLite
      {
         var p:String = null;
         var duration:Number = NaN;
         var t:TweenLite = null;
         vars ||= {};
         var copy:Object = {
            "ease":_easeNone,
            "overwrite":(!!vars.delay ? 2 : 1),
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
            if(vars.onStart)
            {
               vars.onStart.apply(null,vars.onStartParams);
            }
         };
         return t;
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
         var t:TweenLite = this.tweenTo(toPosition,vars);
         return t.duration(Math.abs(t.vars.time - fromPosition) / _timeScale || 0.001) as TweenLite;
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
         var backwards:* = false;
         var wrap:* = false;
         var recTotalTime:Number = NaN;
         var recCycle:int = 0;
         var recRawPrevTime:Number = NaN;
         var recTime:Number = NaN;
         if(_gc)
         {
            _enabled(true,false);
         }
         var totalDur:Number = !_dirty ? _totalDuration : this.totalDuration();
         var prevTime:Number = _time;
         var prevTotalTime:Number = _totalTime;
         var prevStart:Number = _startTime;
         var prevTimeScale:Number = _timeScale;
         var prevRawPrevTime:Number = _rawPrevTime;
         var prevPaused:Boolean = _paused;
         var prevCycle:int = int(this._cycle);
         if(time >= totalDur)
         {
            if(!this._locked)
            {
               _totalTime = totalDur;
               this._cycle = this._repeat;
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
            if(Boolean(this._yoyo) && (this._cycle & 1) != 0)
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
            if(!this._locked)
            {
               _totalTime = this._cycle = 0;
            }
            _time = 0;
            if(prevTime !== 0 || _duration === 0 && _rawPrevTime !== _tinyNum && (_rawPrevTime > 0 || time < 0 && _rawPrevTime >= 0) && !this._locked)
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
            if(!this._locked)
            {
               _totalTime = time;
               if(this._repeat != 0)
               {
                  cycleDuration = _duration + this._repeatDelay;
                  this._cycle = _totalTime / cycleDuration >> 0;
                  if(this._cycle !== 0)
                  {
                     if(this._cycle === _totalTime / cycleDuration)
                     {
                        --this._cycle;
                     }
                  }
                  _time = _totalTime - this._cycle * cycleDuration;
                  if(this._yoyo)
                  {
                     if((this._cycle & 1) != 0)
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
         if(this._cycle != prevCycle)
         {
            if(!this._locked)
            {
               backwards = Boolean(this._yoyo) && (prevCycle & 1) !== 0;
               wrap = backwards == (this._yoyo && (this._cycle & 1) !== 0);
               recTotalTime = _totalTime;
               recCycle = int(this._cycle);
               recRawPrevTime = _rawPrevTime;
               recTime = _time;
               _totalTime = prevCycle * _duration;
               if(this._cycle < prevCycle)
               {
                  backwards = !backwards;
               }
               else
               {
                  _totalTime += _duration;
               }
               _time = prevTime;
               _rawPrevTime = prevRawPrevTime;
               this._cycle = prevCycle;
               this._locked = true;
               prevTime = backwards ? 0 : _duration;
               this.render(prevTime,suppressEvents,false);
               if(!suppressEvents)
               {
                  if(!_gc)
                  {
                     if(vars.onRepeat)
                     {
                        vars.onRepeat.apply(null,vars.onRepeatParams);
                     }
                     if(this._dispatcher)
                     {
                        this._dispatcher.dispatchEvent(new TweenEvent(TweenEvent.REPEAT));
                     }
                  }
               }
               if(wrap)
               {
                  prevTime = backwards ? _duration + 0.0001 : -0.0001;
                  this.render(prevTime,true,false);
               }
               this._locked = false;
               if(_paused && !prevPaused)
               {
                  return;
               }
               _time = recTime;
               _totalTime = recTotalTime;
               this._cycle = recCycle;
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
                  if(vars.onStart)
                  {
                     vars.onStart.apply(this,vars.onStartParams);
                  }
                  if(this._dispatcher)
                  {
                     this._dispatcher.dispatchEvent(new TweenEvent(TweenEvent.START));
                  }
               }
            }
         }
         if(_time >= prevTime)
         {
            tween = _first;
            while(tween)
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
            while(tween)
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
         if(this._hasUpdateListener)
         {
            if(!suppressEvents)
            {
               this._dispatcher.dispatchEvent(new TweenEvent(TweenEvent.UPDATE));
            }
         }
         if(callback)
         {
            if(!this._locked)
            {
               if(!_gc)
               {
                  if(prevStart === _startTime || prevTimeScale !== _timeScale)
                  {
                     if(_time === 0 || totalDur >= this.totalDuration())
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
                           if(vars[callback])
                           {
                              vars[callback].apply(null,vars[callback + "Params"]);
                           }
                           if(this._dispatcher)
                           {
                              this._dispatcher.dispatchEvent(new TweenEvent(callback == "onComplete" ? TweenEvent.COMPLETE : TweenEvent.REVERSE_COMPLETE));
                           }
                        }
                     }
                  }
               }
            }
         }
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
                        var _loc10_:*;
                        a[_loc10_ = cnt++] = tween;
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
         var labels:Array = this.getLabelsArray();
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
      
      public function getLabelBefore(time:Number = NaN) : String
      {
         if(!time)
         {
            if(time != 0)
            {
               time = _time;
            }
         }
         var labels:Array = this.getLabelsArray();
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
      
      public function getLabelsArray() : Array
      {
         var p:String = null;
         var a:Array = [];
         var cnt:int = 0;
         for(p in _labels)
         {
            var _loc6_:*;
            a[_loc6_ = cnt++] = {
               "time":_labels[p],
               "name":p
            };
         }
         a.sortOn("time",Array.NUMERIC);
         return a;
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
                  if(this._dispatcher == null)
                  {
                     this._dispatcher = new EventDispatcher(this);
                  }
                  this._dispatcher.addEventListener(_listenerLookup[p],vars[p],false,0,true);
                  found = true;
               }
            }
         }
         return found;
      }
      
      public function addEventListener(type:String, listener:Function, useCapture:Boolean = false, priority:int = 0, useWeakReference:Boolean = false) : void
      {
         if(this._dispatcher == null)
         {
            this._dispatcher = new EventDispatcher(this);
         }
         if(type == TweenEvent.UPDATE)
         {
            this._hasUpdateListener = true;
         }
         this._dispatcher.addEventListener(type,listener,useCapture,priority,useWeakReference);
      }
      
      public function removeEventListener(type:String, listener:Function, useCapture:Boolean = false) : void
      {
         if(this._dispatcher != null)
         {
            this._dispatcher.removeEventListener(type,listener,useCapture);
         }
      }
      
      public function hasEventListener(type:String) : Boolean
      {
         return this._dispatcher == null ? false : Boolean(this._dispatcher.hasEventListener(type));
      }
      
      public function willTrigger(type:String) : Boolean
      {
         return this._dispatcher == null ? false : Boolean(this._dispatcher.willTrigger(type));
      }
      
      public function dispatchEvent(event:Event) : Boolean
      {
         return this._dispatcher == null ? false : Boolean(this._dispatcher.dispatchEvent(event));
      }
      
      override public function progress(value:Number = NaN, suppressEvents:Boolean = false) : *
      {
         return !arguments.length ? _time / duration() : totalTime(duration() * (Boolean(this._yoyo) && (this._cycle & 1) !== 0 ? 1 - value : value) + this._cycle * (_duration + this._repeatDelay),suppressEvents);
      }
      
      override public function totalProgress(value:Number = NaN, suppressEvents:Boolean = true) : *
      {
         return !arguments.length ? _totalTime / this.totalDuration() : totalTime(this.totalDuration() * value,suppressEvents);
      }
      
      override public function totalDuration(value:Number = NaN) : *
      {
         if(!arguments.length)
         {
            if(_dirty)
            {
               super.totalDuration();
               _totalDuration = this._repeat == -1 ? 999999999999 : _duration * (this._repeat + 1) + this._repeatDelay * this._repeat;
            }
            return _totalDuration;
         }
         return this._repeat == -1 ? this : duration((value - this._repeat * this._repeatDelay) / (this._repeat + 1));
      }
      
      override public function time(value:Number = NaN, suppressEvents:Boolean = false) : *
      {
         if(!arguments.length)
         {
            return _time;
         }
         if(_dirty)
         {
            this.totalDuration();
         }
         if(value > _duration)
         {
            value = _duration;
         }
         if(Boolean(this._yoyo) && (this._cycle & 1) !== 0)
         {
            value = _duration - value + this._cycle * (_duration + this._repeatDelay);
         }
         else if(this._repeat != 0)
         {
            value += this._cycle * (_duration + this._repeatDelay);
         }
         return totalTime(value,suppressEvents);
      }
      
      public function repeat(value:Number = 0) : *
      {
         if(!arguments.length)
         {
            return this._repeat;
         }
         this._repeat = value;
         return _uncache(true);
      }
      
      public function repeatDelay(value:Number = 0) : *
      {
         if(!arguments.length)
         {
            return this._repeatDelay;
         }
         this._repeatDelay = value;
         return _uncache(true);
      }
      
      public function yoyo(value:Boolean = false) : *
      {
         if(!arguments.length)
         {
            return this._yoyo;
         }
         this._yoyo = value;
         return this;
      }
      
      public function currentLabel(value:String = null) : *
      {
         if(!arguments.length)
         {
            return this.getLabelBefore(_time + 1e-8);
         }
         return seek(value,true);
      }
   }
}

