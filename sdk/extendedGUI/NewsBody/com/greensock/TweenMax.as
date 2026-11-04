package com.greensock
{
   import com.greensock.core.Animation;
   import com.greensock.core.PropTween;
   import com.greensock.core.SimpleTimeline;
   import com.greensock.events.TweenEvent;
   import com.greensock.plugins.*;
   import flash.display.DisplayObject;
   import flash.display.DisplayObjectContainer;
   import flash.display.Shape;
   import flash.events.Event;
   import flash.events.EventDispatcher;
   import flash.events.IEventDispatcher;
   import flash.utils.getTimer;
   
   public class TweenMax extends TweenLite implements IEventDispatcher
   {
      public static const version:String = "12.1.5";
      
      protected static var _listenerLookup:Object = {
         "onCompleteListener":TweenEvent.COMPLETE,
         "onUpdateListener":TweenEvent.UPDATE,
         "onStartListener":TweenEvent.START,
         "onRepeatListener":TweenEvent.REPEAT,
         "onReverseCompleteListener":TweenEvent.REVERSE_COMPLETE
      };
      
      public static var ticker:Shape = Animation.ticker;
      
      public static var allTo:Function = staggerTo;
      
      public static var allFrom:Function = staggerFrom;
      
      public static var allFromTo:Function = staggerFromTo;
      
      TweenPlugin.activate([AutoAlphaPlugin,EndArrayPlugin,FramePlugin,RemoveTintPlugin,TintPlugin,VisiblePlugin,VolumePlugin,BevelFilterPlugin,BezierPlugin,BezierThroughPlugin,BlurFilterPlugin,ColorMatrixFilterPlugin,ColorTransformPlugin,DropShadowFilterPlugin,FrameLabelPlugin,GlowFilterPlugin,HexColorsPlugin,RoundPropsPlugin,ShortRotationPlugin]);
      
      protected var _dispatcher:EventDispatcher;
      
      public var _yoyo:Boolean;
      
      protected var _hasUpdateListener:Boolean;
      
      protected var _cycle:int = 0;
      
      protected var _repeatDelay:Number = 0;
      
      protected var _repeat:int = 0;
      
      public function TweenMax(target:Object, duration:Number, vars:Object)
      {
         super(target,duration,vars);
         _yoyo = this.vars.yoyo == true;
         _repeat = int(this.vars.repeat);
         _repeatDelay = Number(this.vars.repeatDelay) || 0;
         _dirty = true;
         if(Boolean(this.vars.onCompleteListener) || Boolean(this.vars.onUpdateListener) || Boolean(this.vars.onStartListener) || Boolean(this.vars.onRepeatListener) || Boolean(this.vars.onReverseCompleteListener))
         {
            _initDispatcher();
            if(_duration == 0)
            {
               if(_delay == 0)
               {
                  if(Boolean(this.vars.immediateRender))
                  {
                     _dispatcher.dispatchEvent(new TweenEvent(TweenEvent.UPDATE));
                     _dispatcher.dispatchEvent(new TweenEvent(TweenEvent.COMPLETE));
                  }
               }
            }
         }
      }
      
      public static function resumeAll(tweens:Boolean = true, delayedCalls:Boolean = true, timelines:Boolean = true) : void
      {
         _changePause(false,tweens,delayedCalls,timelines);
      }
      
      public static function fromTo(target:Object, duration:Number, fromVars:Object, toVars:Object) : TweenMax
      {
         toVars = _prepVars(toVars,false);
         fromVars = _prepVars(fromVars,false);
         toVars.startAt = fromVars;
         toVars.immediateRender = toVars.immediateRender != false && fromVars.immediateRender != false;
         return new TweenMax(target,duration,toVars);
      }
      
      public static function staggerTo(targets:Array, duration:Number, vars:Object, stagger:Number = 0, onCompleteAll:Function = null, onCompleteAllParams:Array = null) : Array
      {
         var copy:Object = null;
         var i:int = 0;
         var p:String = null;
         vars = _prepVars(vars,false);
         var a:Array = [];
         var l:int = int(targets.length);
         var delay:Number = Number(Number(vars.delay) || 0);
         for(i = 0; i < l; i++)
         {
            copy = {};
            for(p in vars)
            {
               copy[p] = vars[p];
            }
            copy.delay = delay;
            if(i == l - 1)
            {
               if(onCompleteAll != null)
               {
                  copy.onComplete = function():void
                  {
                     if(Boolean(vars.onComplete))
                     {
                        vars.onComplete.apply(null,arguments);
                     }
                     onCompleteAll.apply(null,onCompleteAllParams);
                  };
               }
            }
            a[i] = new TweenMax(targets[i],duration,copy);
            delay += stagger;
         }
         return a;
      }
      
      public static function pauseAll(tweens:Boolean = true, delayedCalls:Boolean = true, timelines:Boolean = true) : void
      {
         _changePause(true,tweens,delayedCalls,timelines);
      }
      
      public static function staggerFromTo(targets:Array, duration:Number, fromVars:Object, toVars:Object, stagger:Number = 0, onCompleteAll:Function = null, onCompleteAllParams:Array = null) : Array
      {
         toVars = _prepVars(toVars,false);
         fromVars = _prepVars(fromVars,false);
         toVars.startAt = fromVars;
         toVars.immediateRender = toVars.immediateRender != false && fromVars.immediateRender != false;
         return staggerTo(targets,duration,toVars,stagger,onCompleteAll,onCompleteAllParams);
      }
      
      public static function getTweensOf(target:*, onlyActive:Boolean = false) : Array
      {
         return TweenLite.getTweensOf(target,onlyActive);
      }
      
      public static function killTweensOf(target:*, onlyActive:* = false, vars:Object = null) : void
      {
         TweenLite.killTweensOf(target,onlyActive,vars);
      }
      
      public static function delayedCall(delay:Number, callback:Function, params:Array = null, useFrames:Boolean = false) : TweenMax
      {
         return new TweenMax(callback,0,{
            "delay":delay,
            "onComplete":callback,
            "onCompleteParams":params,
            "onReverseComplete":callback,
            "onReverseCompleteParams":params,
            "immediateRender":false,
            "useFrames":useFrames,
            "overwrite":0
         });
      }
      
      public static function isTweening(target:Object) : Boolean
      {
         return TweenLite.getTweensOf(target,true).length > 0;
      }
      
      public static function killAll(complete:Boolean = false, tweens:Boolean = true, delayedCalls:Boolean = true, timelines:Boolean = true) : void
      {
         var isDC:Boolean = false;
         var tween:Animation = null;
         var i:int = 0;
         var a:Array = getAllTweens(timelines);
         var l:int = int(a.length);
         var allTrue:Boolean = tweens && delayedCalls && timelines;
         for(i = 0; i < l; i++)
         {
            tween = a[i];
            if(allTrue || tween is SimpleTimeline || (isDC = TweenLite(tween).target == TweenLite(tween).vars.onComplete) && delayedCalls || tweens && !isDC)
            {
               if(complete)
               {
                  tween.totalTime(tween._reversed ? 0 : tween.totalDuration());
               }
               else
               {
                  tween._enabled(false,false);
               }
            }
         }
      }
      
      public static function killChildTweensOf(parent:DisplayObjectContainer, complete:Boolean = false) : void
      {
         var i:int = 0;
         var a:Array = getAllTweens(false);
         var l:int = int(a.length);
         for(i = 0; i < l; i++)
         {
            if(_containsChildOf(parent,a[i].target))
            {
               if(complete)
               {
                  a[i].totalTime(a[i].totalDuration());
               }
               else
               {
                  a[i]._enabled(false,false);
               }
            }
         }
      }
      
      private static function _changePause(pause:Boolean, tweens:Boolean = true, delayedCalls:Boolean = false, timelines:Boolean = true) : void
      {
         var isDC:Boolean = false;
         var tween:Animation = null;
         var a:Array = getAllTweens(timelines);
         var allTrue:Boolean = tweens && delayedCalls && timelines;
         var i:int = int(a.length);
         while(--i > -1)
         {
            tween = a[i];
            isDC = tween is TweenLite && TweenLite(tween).target == tween.vars.onComplete;
            if(allTrue || tween is SimpleTimeline || isDC && delayedCalls || tweens && !isDC)
            {
               tween.paused(pause);
            }
         }
      }
      
      public static function set(target:Object, vars:Object) : TweenMax
      {
         return new TweenMax(target,0,vars);
      }
      
      public static function from(target:Object, duration:Number, vars:Object) : TweenMax
      {
         vars = _prepVars(vars,true);
         vars.runBackwards = true;
         return new TweenMax(target,duration,vars);
      }
      
      public static function killDelayedCallsTo(func:Function) : void
      {
         TweenLite.killTweensOf(func);
      }
      
      public static function globalTimeScale(value:Number = NaN) : Number
      {
         if(!arguments.length)
         {
            return _rootTimeline == null ? 1 : _rootTimeline._timeScale;
         }
         value ||= 0.0001;
         if(_rootTimeline == null)
         {
            TweenLite.to({},0,{});
         }
         var tl:SimpleTimeline = _rootTimeline;
         var t:Number = getTimer() / 1000;
         tl._startTime = t - (t - tl._startTime) * tl._timeScale / value;
         tl = _rootFramesTimeline;
         t = _rootFrame;
         tl._startTime = t - (t - tl._startTime) * tl._timeScale / value;
         _rootFramesTimeline._timeScale = _rootTimeline._timeScale = value;
         return value;
      }
      
      public static function getAllTweens(includeTimelines:Boolean = false) : Array
      {
         var a:Array = _getChildrenOf(_rootTimeline,includeTimelines);
         return a.concat(_getChildrenOf(_rootFramesTimeline,includeTimelines));
      }
      
      protected static function _getChildrenOf(timeline:SimpleTimeline, includeTimelines:Boolean) : Array
      {
         if(timeline == null)
         {
            return [];
         }
         var a:Array = [];
         var cnt:int = 0;
         var tween:Animation = timeline._first;
         while(Boolean(tween))
         {
            if(tween is TweenLite)
            {
               var _loc6_:* = cnt++;
               a[_loc6_] = tween;
            }
            else
            {
               if(includeTimelines)
               {
                  _loc6_ = cnt++;
                  a[_loc6_] = tween;
               }
               a = a.concat(_getChildrenOf(SimpleTimeline(tween),includeTimelines));
               cnt = int(a.length);
            }
            tween = tween._next;
         }
         return a;
      }
      
      private static function _containsChildOf(parent:DisplayObjectContainer, obj:Object) : Boolean
      {
         var i:int = 0;
         var curParent:DisplayObjectContainer = null;
         if(obj is Array)
         {
            i = int(obj.length);
            while(--i > -1)
            {
               if(_containsChildOf(parent,obj[i]))
               {
                  return true;
               }
            }
         }
         else if(obj is DisplayObject)
         {
            curParent = obj.parent;
            while(Boolean(curParent))
            {
               if(curParent == parent)
               {
                  return true;
               }
               curParent = curParent.parent;
            }
         }
         return false;
      }
      
      public static function staggerFrom(targets:Array, duration:Number, vars:Object, stagger:Number = 0, onCompleteAll:Function = null, onCompleteAllParams:Array = null) : Array
      {
         vars = _prepVars(vars,true);
         vars.runBackwards = true;
         if(vars.immediateRender != false)
         {
            vars.immediateRender = true;
         }
         return staggerTo(targets,duration,vars,stagger,onCompleteAll,onCompleteAllParams);
      }
      
      public static function to(target:Object, duration:Number, vars:Object) : TweenMax
      {
         return new TweenMax(target,duration,vars);
      }
      
      public function dispatchEvent(event:Event) : Boolean
      {
         return _dispatcher == null ? false : _dispatcher.dispatchEvent(event);
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
      
      public function removeEventListener(type:String, listener:Function, useCapture:Boolean = false) : void
      {
         if(Boolean(_dispatcher))
         {
            _dispatcher.removeEventListener(type,listener,useCapture);
         }
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
      
      public function willTrigger(type:String) : Boolean
      {
         return _dispatcher == null ? false : _dispatcher.willTrigger(type);
      }
      
      override public function duration(value:Number = NaN) : *
      {
         if(!arguments.length)
         {
            return this._duration;
         }
         return super.duration(value);
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
      
      override public function render(time:Number, suppressEvents:Boolean = false, force:Boolean = false) : void
      {
         var isComplete:Boolean = false;
         var callback:String = null;
         var pt:PropTween = null;
         var rawPrevTime:Number = NaN;
         var cycleDuration:Number = NaN;
         var r:Number = NaN;
         var type:int = 0;
         var pow:int = 0;
         if(!_initted)
         {
            if(_duration === 0 && Boolean(vars.repeat))
            {
               invalidate();
            }
         }
         var totalDur:Number = !_dirty ? _totalDuration : totalDuration();
         var prevTime:Number = _time;
         var prevTotalTime:Number = _totalTime;
         var prevCycle:Number = _cycle;
         if(time >= totalDur)
         {
            _totalTime = totalDur;
            _cycle = _repeat;
            if(_yoyo && (_cycle & 1) != 0)
            {
               _time = 0;
               ratio = _ease._calcEnd ? _ease.getRatio(0) : 0;
            }
            else
            {
               _time = _duration;
               ratio = _ease._calcEnd ? _ease.getRatio(1) : 1;
            }
            if(!_reversed)
            {
               isComplete = true;
               callback = "onComplete";
            }
            if(_duration == 0)
            {
               rawPrevTime = _rawPrevTime;
               if(_startTime === _timeline._duration)
               {
                  time = 0;
               }
               if(time === 0 || rawPrevTime < 0 || rawPrevTime === _tinyNum)
               {
                  if(rawPrevTime !== time)
                  {
                     force = true;
                     if(rawPrevTime > _tinyNum)
                     {
                        callback = "onReverseComplete";
                     }
                  }
               }
               _rawPrevTime = rawPrevTime = !suppressEvents || time !== 0 || _rawPrevTime === time ? time : _tinyNum;
            }
         }
         else if(time < 1e-7)
         {
            _totalTime = _time = _cycle = 0;
            ratio = _ease._calcEnd ? _ease.getRatio(0) : 0;
            if(prevTotalTime !== 0 || _duration === 0 && _rawPrevTime > 0 && _rawPrevTime !== _tinyNum)
            {
               callback = "onReverseComplete";
               isComplete = _reversed;
            }
            if(time < 0)
            {
               _active = false;
               if(_duration == 0)
               {
                  if(_rawPrevTime >= 0)
                  {
                     force = true;
                  }
                  _rawPrevTime = rawPrevTime = !suppressEvents || time !== 0 || _rawPrevTime === time ? time : _tinyNum;
               }
            }
            else if(!_initted)
            {
               force = true;
            }
         }
         else
         {
            _totalTime = _time = time;
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
               }
               else if(_time < 0)
               {
                  _time = 0;
               }
            }
            if(Boolean(_easeType))
            {
               r = _time / _duration;
               type = _easeType;
               pow = _easePower;
               if(type == 1 || type == 3 && r >= 0.5)
               {
                  r = 1 - r;
               }
               if(type == 3)
               {
                  r *= 2;
               }
               if(pow == 1)
               {
                  r *= r;
               }
               else if(pow == 2)
               {
                  r *= r * r;
               }
               else if(pow == 3)
               {
                  r *= r * r * r;
               }
               else if(pow == 4)
               {
                  r *= r * r * r * r;
               }
               if(type == 1)
               {
                  ratio = 1 - r;
               }
               else if(type == 2)
               {
                  ratio = r;
               }
               else if(_time / _duration < 0.5)
               {
                  ratio = r / 2;
               }
               else
               {
                  ratio = 1 - r / 2;
               }
            }
            else
            {
               ratio = _ease.getRatio(_time / _duration);
            }
         }
         if(prevTime == _time && !force && _cycle === prevCycle)
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
            _init();
            if(!_initted || _gc)
            {
               return;
            }
            if(Boolean(_time) && !isComplete)
            {
               ratio = _ease.getRatio(_time / _duration);
            }
            else if(isComplete && _ease._calcEnd)
            {
               ratio = _ease.getRatio(_time === 0 ? 0 : 1);
            }
         }
         if(!_active)
         {
            if(!_paused && _time !== prevTime && time >= 0)
            {
               _active = true;
            }
         }
         if(prevTotalTime == 0)
         {
            if(_startAt != null)
            {
               if(time >= 0)
               {
                  _startAt.render(time,suppressEvents,force);
               }
               else if(!callback)
               {
                  callback = "_dummyGS";
               }
            }
            if(_totalTime != 0 || _duration == 0)
            {
               if(!suppressEvents)
               {
                  if(Boolean(vars.onStart))
                  {
                     vars.onStart.apply(null,vars.onStartParams);
                  }
                  if(Boolean(_dispatcher))
                  {
                     _dispatcher.dispatchEvent(new TweenEvent(TweenEvent.START));
                  }
               }
            }
         }
         pt = _firstPT;
         while(Boolean(pt))
         {
            if(pt.f)
            {
               pt.t[pt.p](pt.c * ratio + pt.s);
            }
            else
            {
               pt.t[pt.p] = pt.c * ratio + pt.s;
            }
            pt = pt._next;
         }
         if(_onUpdate != null)
         {
            if(time < 0 && _startAt != null && _startTime != 0)
            {
               _startAt.render(time,suppressEvents,force);
            }
            if(!suppressEvents)
            {
               if(_totalTime !== prevTotalTime || isComplete)
               {
                  _onUpdate.apply(null,vars.onUpdateParams);
               }
            }
         }
         if(_hasUpdateListener)
         {
            if(time < 0 && _startAt != null && _onUpdate == null && _startTime != 0)
            {
               _startAt.render(time,suppressEvents,force);
            }
            if(!suppressEvents)
            {
               _dispatcher.dispatchEvent(new TweenEvent(TweenEvent.UPDATE));
            }
         }
         if(_cycle != prevCycle)
         {
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
         }
         if(Boolean(callback))
         {
            if(!_gc)
            {
               if(time < 0 && _startAt != null && _onUpdate == null && !_hasUpdateListener && _startTime != 0)
               {
                  _startAt.render(time,suppressEvents,true);
               }
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
               if(_duration === 0 && _rawPrevTime === _tinyNum && rawPrevTime !== _tinyNum)
               {
                  _rawPrevTime = 0;
               }
            }
         }
      }
      
      override public function totalProgress(value:Number = NaN, suppressEvents:Boolean = false) : *
      {
         return !arguments.length ? _totalTime / totalDuration() : totalTime(totalDuration() * value,suppressEvents);
      }
      
      public function repeat(value:int = 0) : *
      {
         if(!arguments.length)
         {
            return _repeat;
         }
         _repeat = value;
         return _uncache(true);
      }
      
      public function updateTo(vars:Object, resetDuration:Boolean = false) : *
      {
         var p:String = null;
         var prevTime:Number = NaN;
         var inv:Number = NaN;
         var pt:PropTween = null;
         var endValue:Number = NaN;
         var curRatio:Number = ratio;
         if(resetDuration)
         {
            if(_startTime < _timeline._time)
            {
               _startTime = _timeline._time;
               _uncache(false);
               if(_gc)
               {
                  _enabled(true,false);
               }
               else
               {
                  _timeline.insert(this,_startTime - _delay);
               }
            }
         }
         for(p in vars)
         {
            this.vars[p] = vars[p];
         }
         if(_initted)
         {
            if(resetDuration)
            {
               _initted = false;
            }
            else
            {
               if(_gc)
               {
                  _enabled(true,false);
               }
               if(_notifyPluginsOfEnabled)
               {
                  if(_firstPT != null)
                  {
                     _onPluginEvent("_onDisable",this);
                  }
               }
               if(_time / _duration > 0.998)
               {
                  prevTime = _time;
                  render(0,true,false);
                  _initted = false;
                  render(prevTime,true,false);
               }
               else if(_time > 0)
               {
                  _initted = false;
                  _init();
                  inv = 1 / (1 - curRatio);
                  pt = _firstPT;
                  while(Boolean(pt))
                  {
                     endValue = pt.s + pt.c;
                     pt.c *= inv;
                     pt.s = endValue - pt.c;
                     pt = pt._next;
                  }
               }
            }
         }
         return this;
      }
      
      public function repeatDelay(value:Number = NaN) : *
      {
         if(!arguments.length)
         {
            return _repeatDelay;
         }
         _repeatDelay = value;
         return _uncache(true);
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
      
      override public function totalDuration(value:Number = NaN) : *
      {
         if(!arguments.length)
         {
            if(_dirty)
            {
               _totalDuration = _repeat == -1 ? 999999999999 : _duration * (_repeat + 1) + _repeatDelay * _repeat;
               _dirty = false;
            }
            return _totalDuration;
         }
         return _repeat == -1 ? this : duration((value - _repeat * _repeatDelay) / (_repeat + 1));
      }
      
      public function hasEventListener(type:String) : Boolean
      {
         return _dispatcher == null ? false : _dispatcher.hasEventListener(type);
      }
   }
}

