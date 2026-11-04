package com.greensock
{
   import com.greensock.core.Animation;
   import com.greensock.core.SimpleTimeline;
   
   public class TimelineLite extends SimpleTimeline
   {
      public static const version:String = "12.1.5";
      
      protected var _labels:Object;
      
      public function TimelineLite(vars:Object = null)
      {
         var val:Object = null;
         var p:String = null;
         super(vars);
         _labels = {};
         autoRemoveChildren = this.vars.autoRemoveChildren == true;
         smoothChildTiming = this.vars.smoothChildTiming == true;
         _sortChildren = true;
         _onUpdate = this.vars.onUpdate;
         for(p in this.vars)
         {
            val = this.vars[p];
            if(val is Array)
            {
               if(val.join("").indexOf("{self}") !== -1)
               {
                  this.vars[p] = _swapSelfInParams(val as Array);
               }
            }
         }
         if(this.vars.tweens is Array)
         {
            this.add(this.vars.tweens,0,this.vars.align || "normal",Number(this.vars.stagger) || 0);
         }
      }
      
      protected static function _copy(vars:Object) : Object
      {
         var p:String = null;
         var copy:Object = {};
         for(p in vars)
         {
            copy[p] = vars[p];
         }
         return copy;
      }
      
      protected static function _prepVars(vars:Object) : Object
      {
         return Boolean(vars._isGSVars) ? vars.vars : vars;
      }
      
      public static function exportRoot(vars:Object = null, omitDelayedCalls:Boolean = true) : TimelineLite
      {
         var next:Animation = null;
         vars ||= {};
         if(!("smoothChildTiming" in vars))
         {
            vars.smoothChildTiming = true;
         }
         var tl:TimelineLite = new TimelineLite(vars);
         var root:SimpleTimeline = tl._timeline;
         root._remove(tl,true);
         tl._startTime = 0;
         tl._rawPrevTime = tl._time = tl._totalTime = root._time;
         var tween:Animation = root._first;
         while(Boolean(tween))
         {
            next = tween._next;
            if(!omitDelayedCalls || !(tween is TweenLite && TweenLite(tween).target == tween.vars.onComplete))
            {
               tl.add(tween,tween._startTime - tween._delay);
            }
            tween = next;
         }
         root.add(tl,0);
         return tl;
      }
      
      public function stop() : *
      {
         return paused(true);
      }
      
      public function remove(value:*) : *
      {
         var i:Number = NaN;
         if(value is Animation)
         {
            return _remove(value,false);
         }
         if(value is Array)
         {
            i = Number(value.length);
            while(--i > -1)
            {
               remove(value[i]);
            }
            return this;
         }
         if(typeof value == "string")
         {
            return removeLabel(String(value));
         }
         return kill(null,value);
      }
      
      public function fromTo(target:Object, duration:Number, fromVars:Object, toVars:Object, position:* = "+=0") : *
      {
         return Boolean(duration) ? add(TweenLite.fromTo(target,duration,fromVars,toVars),position) : this.set(target,toVars,position);
      }
      
      public function staggerTo(targets:Array, duration:Number, vars:Object, stagger:Number, position:* = "+=0", onCompleteAll:Function = null, onCompleteAllParams:Array = null) : *
      {
         var tl:TimelineLite = new TimelineLite({
            "onComplete":onCompleteAll,
            "onCompleteParams":onCompleteAllParams,
            "smoothChildTiming":this.smoothChildTiming
         });
         for(var i:int = 0; i < targets.length; i++)
         {
            if(vars.startAt != null)
            {
               vars.startAt = _copy(vars.startAt);
            }
            tl.to(targets[i],duration,_copy(vars),i * stagger);
         }
         return add(tl,position);
      }
      
      override public function _enabled(enabled:Boolean, ignoreTimeline:Boolean = false) : Boolean
      {
         var tween:Animation = null;
         if(enabled == _gc)
         {
            tween = _first;
            while(Boolean(tween))
            {
               tween._enabled(enabled,true);
               tween = tween._next;
            }
         }
         return super._enabled(enabled,ignoreTimeline);
      }
      
      public function appendMultiple(tweens:Array, offsetOrLabel:* = 0, align:String = "normal", stagger:Number = 0) : *
      {
         return add(tweens,_parseTimeOrLabel(null,offsetOrLabel,true,tweens),align,stagger);
      }
      
      public function clear(labels:Boolean = true) : *
      {
         var tweens:Array = null;
         var i:int = 0;
         tweens = getChildren(false,true,true);
         i = int(tweens.length);
         _time = _totalTime = 0;
         while(--i > -1)
         {
            tweens[i]._enabled(false,false);
         }
         if(labels)
         {
            _labels = {};
         }
         return _uncache(true);
      }
      
      public function gotoAndPlay(position:*, suppressEvents:Boolean = true) : *
      {
         return play(position,suppressEvents);
      }
      
      public function removeLabel(label:String) : *
      {
         delete _labels[label];
         return this;
      }
      
      public function staggerFromTo(targets:Array, duration:Number, fromVars:Object, toVars:Object, stagger:Number = 0, position:* = "+=0", onCompleteAll:Function = null, onCompleteAllParams:Array = null) : *
      {
         toVars = _prepVars(toVars);
         fromVars = _prepVars(fromVars);
         toVars.startAt = fromVars;
         toVars.immediateRender = toVars.immediateRender != false && fromVars.immediateRender != false;
         return staggerTo(targets,duration,toVars,stagger,position,onCompleteAll,onCompleteAllParams);
      }
      
      public function addLabel(label:String, position:* = "+=0") : *
      {
         _labels[label] = _parseTimeOrLabel(position);
         return this;
      }
      
      override public function duration(value:Number = NaN) : *
      {
         if(!arguments.length)
         {
            if(_dirty)
            {
               totalDuration();
            }
            return _duration;
         }
         switch(value)
         {
            default:
               timeScale(_duration / value);
               break;
            case 0:
            case 0:
         }
         return this;
      }
      
      public function addPause(position:* = "+=0", callback:Function = null, params:Array = null) : *
      {
         return call(_pauseCallback,["{self}",callback,params],position);
      }
      
      public function gotoAndStop(position:*, suppressEvents:Boolean = true) : *
      {
         return pause(position,suppressEvents);
      }
      
      public function usesFrames() : Boolean
      {
         var tl:SimpleTimeline = _timeline;
         while(Boolean(tl._timeline))
         {
            tl = tl._timeline;
         }
         return tl == _rootFramesTimeline;
      }
      
      public function append(value:*, offsetOrLabel:* = 0) : *
      {
         return add(value,_parseTimeOrLabel(null,offsetOrLabel,true,value));
      }
      
      protected function _pauseCallback(tween:TweenLite, callback:Function = null, params:Array = null) : void
      {
         pause(tween._startTime);
         if(callback != null)
         {
            callback.apply(null,params);
         }
      }
      
      override public function render(time:Number, suppressEvents:Boolean = false, force:Boolean = false) : void
      {
         var prevTime:Number = NaN;
         var tween:Animation = null;
         var isComplete:Boolean = false;
         var next:Animation = null;
         var callback:String = null;
         var internalForce:Boolean = false;
         if(_gc)
         {
            _enabled(true,false);
         }
         var totalDur:Number = !_dirty ? _totalDuration : totalDuration();
         prevTime = _time;
         var prevStart:Number = _startTime;
         var prevTimeScale:Number = _timeScale;
         var prevPaused:Boolean = _paused;
         if(time >= totalDur)
         {
            _totalTime = _time = totalDur;
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
            _rawPrevTime = _duration !== 0 || !suppressEvents || time !== 0 || _rawPrevTime === time ? time : _tinyNum;
            time = totalDur + 0.0001;
         }
         else if(time < 1e-7)
         {
            _totalTime = _time = 0;
            if(prevTime !== 0 || _duration === 0 && _rawPrevTime !== _tinyNum && (_rawPrevTime > 0 || time < 0 && _rawPrevTime >= 0))
            {
               callback = "onReverseComplete";
               isComplete = _reversed;
            }
            if(time < 0)
            {
               _active = false;
               if(_rawPrevTime >= 0 && _first != null)
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
            _totalTime = _time = _rawPrevTime = time;
         }
         if((_time == prevTime || !_first) && !force && !internalForce)
         {
            return;
         }
         if(!_initted)
         {
            _initted = true;
         }
         if(!_active)
         {
            if(!_paused && _time !== prevTime && time > 0)
            {
               _active = true;
            }
         }
         if(prevTime == 0)
         {
            if(Boolean(vars.onStart))
            {
               if(_time != 0)
               {
                  if(!suppressEvents)
                  {
                     vars.onStart.apply(null,vars.onStartParams);
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
         if(Boolean(callback))
         {
            if(!_gc)
            {
               if(prevStart == _startTime || prevTimeScale != _timeScale)
               {
                  if(_time == 0 || totalDur >= totalDuration())
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
                     }
                  }
               }
            }
         }
      }
      
      override public function add(value:*, position:* = "+=0", align:String = "normal", stagger:Number = 0) : *
      {
         var i:int = 0;
         var curTime:Number = NaN;
         var l:Number = NaN;
         var child:* = undefined;
         var tl:SimpleTimeline = null;
         var beforeRawTime:Boolean = false;
         if(typeof position !== "number")
         {
            position = _parseTimeOrLabel(position,0,true,value);
         }
         if(!(value is Animation))
         {
            if(value is Array)
            {
               curTime = Number(position);
               l = Number(value.length);
               for(i = 0; i < l; i++)
               {
                  child = value[i];
                  if(child is Array)
                  {
                     child = new TimelineLite({"tweens":child});
                  }
                  add(child,curTime);
                  if(!(typeof child === "string" || typeof child === "function"))
                  {
                     if(align === "sequence")
                     {
                        curTime = child._startTime + child.totalDuration() / child._timeScale;
                     }
                     else if(align === "start")
                     {
                        child._startTime -= child.delay();
                     }
                  }
                  curTime += stagger;
               }
               return _uncache(true);
            }
            if(typeof value === "string")
            {
               return addLabel(String(value),position);
            }
            if(typeof value !== "function")
            {
               trace("Cannot add " + value + " into the TimelineLite/Max: it is not a tween, timeline, function, or string.");
               return this;
            }
            value = TweenLite.delayedCall(0,value);
         }
         super.add(value,position);
         if(_gc || _time === _duration)
         {
            if(!_paused)
            {
               if(_duration < duration())
               {
                  tl = this;
                  beforeRawTime = tl.rawTime() > value._startTime;
                  while(Boolean(tl._timeline))
                  {
                     if(beforeRawTime && tl._timeline.smoothChildTiming)
                     {
                        tl.totalTime(tl._totalTime,true);
                     }
                     else if(tl._gc)
                     {
                        tl._enabled(true,false);
                     }
                     tl = tl._timeline;
                  }
               }
            }
         }
         return this;
      }
      
      public function set(target:Object, vars:Object, position:* = "+=0") : *
      {
         position = _parseTimeOrLabel(position,0,true);
         vars = _prepVars(vars);
         if(vars.immediateRender == null)
         {
            vars.immediateRender = position === _time && !_paused;
         }
         return add(new TweenLite(target,0,vars),position);
      }
      
      override public function _remove(tween:Animation, skipDisable:Boolean = false) : *
      {
         super._remove(tween,skipDisable);
         if(_last == null)
         {
            _time = _totalTime = _duration = _totalDuration = 0;
         }
         else if(_time > _last._startTime + _last._totalDuration / _last._timeScale)
         {
            _time = duration();
            _totalTime = _totalDuration;
         }
         return this;
      }
      
      public function getTweensOf(target:Object, nested:Boolean = true) : Array
      {
         var tweens:Array = null;
         var i:int = 0;
         var disabled:Boolean = this._gc;
         var a:Array = [];
         var cnt:int = 0;
         if(disabled)
         {
            _enabled(true,true);
         }
         tweens = TweenLite.getTweensOf(target);
         i = int(tweens.length);
         while(--i > -1)
         {
            if(tweens[i].timeline === this || nested && _contains(tweens[i]))
            {
               var _loc8_:* = cnt++;
               a[_loc8_] = tweens[i];
            }
         }
         if(disabled)
         {
            _enabled(false,true);
         }
         return a;
      }
      
      public function call(callback:Function, params:Array = null, position:* = "+=0") : *
      {
         return add(TweenLite.delayedCall(0,callback,params),position);
      }
      
      public function shiftChildren(amount:Number, adjustLabels:Boolean = false, ignoreBeforeTime:Number = 0) : *
      {
         var p:String = null;
         var tween:Animation = _first;
         while(Boolean(tween))
         {
            if(tween._startTime >= ignoreBeforeTime)
            {
               tween._startTime += amount;
            }
            tween = tween._next;
         }
         if(adjustLabels)
         {
            for(p in _labels)
            {
               if(_labels[p] >= ignoreBeforeTime)
               {
                  _labels[p] += amount;
               }
            }
         }
         _uncache(true);
         return this;
      }
      
      public function insertMultiple(tweens:Array, timeOrLabel:* = 0, align:String = "normal", stagger:Number = 0) : *
      {
         return add(tweens,timeOrLabel || 0,align,stagger);
      }
      
      public function from(target:Object, duration:Number, vars:Object, position:* = "+=0") : *
      {
         return add(TweenLite.from(target,duration,vars),position);
      }
      
      public function getLabelTime(label:String) : Number
      {
         return label in _labels ? Number(_labels[label]) : -1;
      }
      
      override public function rawTime() : Number
      {
         return _paused ? _totalTime : (_timeline.rawTime() - _startTime) * _timeScale;
      }
      
      protected function _parseTimeOrLabel(timeOrLabel:*, offsetOrLabel:* = 0, appendIfAbsent:Boolean = false, ignore:Object = null) : Number
      {
         var i:int = 0;
         if(ignore is Animation && ignore.timeline === this)
         {
            remove(ignore);
         }
         else if(ignore is Array)
         {
            i = int(ignore.length);
            while(--i > -1)
            {
               if(ignore[i] is Animation && ignore[i].timeline === this)
               {
                  remove(ignore[i]);
               }
            }
         }
         if(typeof offsetOrLabel === "string")
         {
            return _parseTimeOrLabel(offsetOrLabel,appendIfAbsent && typeof timeOrLabel === "number" && !(offsetOrLabel in _labels) ? timeOrLabel - duration() : 0,appendIfAbsent);
         }
         offsetOrLabel ||= 0;
         if(typeof timeOrLabel === "string" && (isNaN(timeOrLabel) || timeOrLabel in _labels))
         {
            i = int(timeOrLabel.indexOf("="));
            if(i === -1)
            {
               if(!(timeOrLabel in _labels))
               {
                  return appendIfAbsent ? (_labels[timeOrLabel] = duration() + offsetOrLabel) : offsetOrLabel;
               }
               return _labels[timeOrLabel] + offsetOrLabel;
            }
            offsetOrLabel = parseInt(timeOrLabel.charAt(i - 1) + "1",10) * Number(timeOrLabel.substr(i + 1));
            timeOrLabel = i > 1 ? _parseTimeOrLabel(timeOrLabel.substr(0,i - 1),0,appendIfAbsent) : duration();
         }
         else if(timeOrLabel == null)
         {
            timeOrLabel = duration();
         }
         return Number(timeOrLabel) + offsetOrLabel;
      }
      
      override public function invalidate() : *
      {
         var tween:Animation = _first;
         while(Boolean(tween))
         {
            tween.invalidate();
            tween = tween._next;
         }
         return this;
      }
      
      public function getChildren(nested:Boolean = true, tweens:Boolean = true, timelines:Boolean = true, ignoreBeforeTime:Number = -9999999999) : Array
      {
         var a:Array = [];
         var tween:Animation = _first;
         var cnt:int = 0;
         while(Boolean(tween))
         {
            if(tween._startTime >= ignoreBeforeTime)
            {
               if(tween is TweenLite)
               {
                  if(tweens)
                  {
                     var _loc8_:* = cnt++;
                     a[_loc8_] = tween;
                  }
               }
               else
               {
                  if(timelines)
                  {
                     _loc8_ = cnt++;
                     a[_loc8_] = tween;
                  }
                  if(nested)
                  {
                     a = a.concat(TimelineLite(tween).getChildren(true,tweens,timelines));
                     cnt = int(a.length);
                  }
               }
            }
            tween = tween._next;
         }
         return a;
      }
      
      public function staggerFrom(targets:Array, duration:Number, vars:Object, stagger:Number = 0, position:* = "+=0", onCompleteAll:Function = null, onCompleteAllParams:Array = null) : *
      {
         vars = _prepVars(vars);
         if(!("immediateRender" in vars))
         {
            vars.immediateRender = true;
         }
         vars.runBackwards = true;
         return staggerTo(targets,duration,vars,stagger,position,onCompleteAll,onCompleteAllParams);
      }
      
      override public function seek(position:*, suppressEvents:Boolean = true) : *
      {
         return totalTime(typeof position === "number" ? Number(position) : _parseTimeOrLabel(position),suppressEvents);
      }
      
      override public function insert(value:*, timeOrLabel:* = 0) : *
      {
         return add(value,timeOrLabel || 0);
      }
      
      public function _hasPausedChild() : Boolean
      {
         var tween:Animation = _first;
         while(Boolean(tween))
         {
            if(tween._paused || tween is TimelineLite && TimelineLite(tween)._hasPausedChild())
            {
               return true;
            }
            tween = tween._next;
         }
         return false;
      }
      
      public function to(target:Object, duration:Number, vars:Object, position:* = "+=0") : *
      {
         return Boolean(duration) ? add(new TweenLite(target,duration,vars),position) : this.set(target,vars,position);
      }
      
      override public function _kill(vars:Object = null, target:Object = null) : Boolean
      {
         if(vars == null)
         {
            if(target == null)
            {
               return _enabled(false,false);
            }
         }
         var tweens:Array = target == null ? getChildren(true,true,false) : getTweensOf(target);
         var i:int = int(tweens.length);
         var changed:Boolean = false;
         while(--i > -1)
         {
            if(Boolean(tweens[i]._kill(vars,target)))
            {
               changed = true;
            }
         }
         return changed;
      }
      
      private function _contains(tween:Animation) : Boolean
      {
         var tl:SimpleTimeline = tween.timeline;
         while(Boolean(tl))
         {
            if(tl == this)
            {
               return true;
            }
            tl = tl.timeline;
         }
         return false;
      }
      
      override public function totalDuration(value:Number = NaN) : *
      {
         var max:Number = NaN;
         var tween:Animation = null;
         var prevStart:Number = NaN;
         var prev:Animation = null;
         var end:Number = NaN;
         if(!arguments.length)
         {
            if(_dirty)
            {
               max = 0;
               tween = _last;
               prevStart = Infinity;
               while(Boolean(tween))
               {
                  prev = tween._prev;
                  if(tween._dirty)
                  {
                     tween.totalDuration();
                  }
                  if(tween._startTime > prevStart && _sortChildren && !tween._paused)
                  {
                     add(tween,tween._startTime - tween._delay);
                  }
                  else
                  {
                     prevStart = tween._startTime;
                  }
                  if(tween._startTime < 0 && !tween._paused)
                  {
                     max -= tween._startTime;
                     if(_timeline.smoothChildTiming)
                     {
                        _startTime += tween._startTime / _timeScale;
                     }
                     shiftChildren(-tween._startTime,false,-9999999999);
                     prevStart = 0;
                  }
                  end = tween._startTime + tween._totalDuration / tween._timeScale;
                  if(end > max)
                  {
                     max = end;
                  }
                  tween = prev;
               }
               _duration = _totalDuration = max;
               _dirty = false;
            }
            return _totalDuration;
         }
         if(totalDuration() != 0)
         {
            if(value != 0)
            {
               timeScale(_totalDuration / value);
            }
         }
         return this;
      }
   }
}

