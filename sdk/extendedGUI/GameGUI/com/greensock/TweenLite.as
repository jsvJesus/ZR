package com.greensock
{
   import com.greensock.core.Animation;
   import com.greensock.core.PropTween;
   import com.greensock.core.SimpleTimeline;
   import com.greensock.easing.Ease;
   import flash.display.Shape;
   import flash.events.Event;
   import flash.utils.Dictionary;
   
   public class TweenLite extends Animation
   {
      public static var _onPluginEvent:Function;
      
      protected static var _overwriteLookup:Object;
      
      public static const version:String = "12.1.5";
      
      public static var defaultEase:Ease = new Ease(null,null,1,1);
      
      public static var defaultOverwrite:String = "auto";
      
      public static var ticker:Shape = Animation.ticker;
      
      public static var _plugins:Object = {};
      
      protected static var _tweenLookup:Dictionary = new Dictionary(false);
      
      protected static var _reservedProps:Object = {
         "ease":1,
         "delay":1,
         "overwrite":1,
         "onComplete":1,
         "onCompleteParams":1,
         "onCompleteScope":1,
         "useFrames":1,
         "runBackwards":1,
         "startAt":1,
         "onUpdate":1,
         "onUpdateParams":1,
         "onUpdateScope":1,
         "onStart":1,
         "onStartParams":1,
         "onStartScope":1,
         "onReverseComplete":1,
         "onReverseCompleteParams":1,
         "onReverseCompleteScope":1,
         "onRepeat":1,
         "onRepeatParams":1,
         "onRepeatScope":1,
         "easeParams":1,
         "yoyo":1,
         "onCompleteListener":1,
         "onUpdateListener":1,
         "onStartListener":1,
         "onReverseCompleteListener":1,
         "onRepeatListener":1,
         "orientToBezier":1,
         "immediateRender":1,
         "repeat":1,
         "repeatDelay":1,
         "data":1,
         "paused":1,
         "reversed":1
      };
      
      protected var _targets:Array;
      
      public var ratio:Number;
      
      protected var _overwrite:int;
      
      public var _ease:Ease;
      
      protected var _siblings:Array;
      
      public var target:Object;
      
      protected var _overwrittenProps:Object;
      
      public var _propLookup:Object;
      
      protected var _easeType:int;
      
      protected var _notifyPluginsOfEnabled:Boolean;
      
      public var _firstPT:PropTween;
      
      protected var _startAt:TweenLite;
      
      protected var _easePower:int;
      
      public function TweenLite(target:Object, duration:Number, vars:Object)
      {
         var i:int = 0;
         super(duration,vars);
         if(target == null)
         {
            throw new Error("Cannot tween a null object. Duration: " + duration + ", data: " + this.data);
         }
         if(!_overwriteLookup)
         {
            _overwriteLookup = {
               "none":0,
               "all":1,
               "auto":2,
               "concurrent":3,
               "allOnStart":4,
               "preexisting":5,
               "true":1,
               "false":0
            };
            ticker.addEventListener("enterFrame",_dumpGarbage,false,-1,true);
         }
         ratio = 0;
         this.target = target;
         _ease = defaultEase;
         _overwrite = !("overwrite" in this.vars) ? int(_overwriteLookup[defaultOverwrite]) : (typeof this.vars.overwrite === "number" ? this.vars.overwrite >> 0 : int(_overwriteLookup[this.vars.overwrite]));
         if(this.target is Array && typeof this.target[0] === "object")
         {
            _targets = this.target.concat();
            _propLookup = [];
            _siblings = [];
            i = int(_targets.length);
            while(--i > -1)
            {
               _siblings[i] = _register(_targets[i],this,false);
               if(_overwrite == 1)
               {
                  if(_siblings[i].length > 1)
                  {
                     _applyOverwrite(_targets[i],this,null,1,_siblings[i]);
                  }
               }
            }
         }
         else
         {
            _propLookup = {};
            _siblings = _tweenLookup[target];
            if(_siblings == null)
            {
               _siblings = _tweenLookup[target] = [this];
            }
            else
            {
               _siblings[_siblings.length] = this;
               if(_overwrite == 1)
               {
                  _applyOverwrite(target,this,null,1,_siblings);
               }
            }
         }
         if(Boolean(this.vars.immediateRender) || duration == 0 && _delay == 0 && this.vars.immediateRender != false)
         {
            render(-_delay,false,true);
         }
      }
      
      public static function fromTo(target:Object, duration:Number, fromVars:Object, toVars:Object) : TweenLite
      {
         toVars = _prepVars(toVars,true);
         fromVars = _prepVars(fromVars);
         toVars.startAt = fromVars;
         toVars.immediateRender = toVars.immediateRender != false && fromVars.immediateRender != false;
         return new TweenLite(target,duration,toVars);
      }
      
      public static function getTweensOf(target:*, onlyActive:Boolean = false) : Array
      {
         var i:int = 0;
         var a:Array = null;
         var j:int = 0;
         var t:TweenLite = null;
         if(target is Array && typeof target[0] != "string" && typeof target[0] != "number")
         {
            i = int(target.length);
            a = [];
            while(--i > -1)
            {
               a = a.concat(getTweensOf(target[i],onlyActive));
            }
            i = int(a.length);
            while(--i > -1)
            {
               t = a[i];
               j = i;
               while(--j > -1)
               {
                  if(t === a[j])
                  {
                     a.splice(i,1);
                  }
               }
            }
         }
         else
         {
            a = _register(target).concat();
            i = int(a.length);
            while(--i > -1)
            {
               if(Boolean(a[i]._gc) || onlyActive && !a[i].isActive())
               {
                  a.splice(i,1);
               }
            }
         }
         return a;
      }
      
      protected static function _register(target:Object, tween:TweenLite = null, scrub:Boolean = false) : Array
      {
         var i:int = 0;
         var a:Array = _tweenLookup[target];
         if(a == null)
         {
            a = _tweenLookup[target] = [];
         }
         if(Boolean(tween))
         {
            i = int(a.length);
            a[i] = tween;
            if(scrub)
            {
               while(--i > -1)
               {
                  if(a[i] === tween)
                  {
                     a.splice(i,1);
                  }
               }
            }
         }
         return a;
      }
      
      protected static function _applyOverwrite(target:Object, tween:TweenLite, props:Object, mode:int, siblings:Array) : Boolean
      {
         var i:int = 0;
         var changed:Boolean = false;
         var curTween:TweenLite = null;
         var globalStart:Number = NaN;
         var l:int = 0;
         if(mode == 1 || mode >= 4)
         {
            l = int(siblings.length);
            for(i = 0; i < l; i++)
            {
               curTween = siblings[i];
               if(curTween != tween)
               {
                  if(!curTween._gc)
                  {
                     if(curTween._enabled(false,false))
                     {
                        changed = true;
                     }
                  }
               }
               else if(mode == 5)
               {
                  break;
               }
            }
            return changed;
         }
         var startTime:Number = tween._startTime + 1e-10;
         var overlaps:Array = [];
         var oCount:int = 0;
         var zeroDur:Boolean = tween._duration == 0;
         i = int(siblings.length);
         while(--i > -1)
         {
            curTween = siblings[i];
            if(!(curTween === tween || curTween._gc || curTween._paused))
            {
               if(curTween._timeline != tween._timeline)
               {
                  globalStart ||= _checkOverlap(tween,0,zeroDur);
                  if(_checkOverlap(curTween,globalStart,zeroDur) === 0)
                  {
                     var _loc15_:* = oCount++;
                     overlaps[_loc15_] = curTween;
                  }
               }
               else if(curTween._startTime <= startTime)
               {
                  if(curTween._startTime + curTween.totalDuration() / curTween._timeScale > startTime)
                  {
                     if(!((zeroDur || !curTween._initted) && startTime - curTween._startTime <= 2e-10))
                     {
                        _loc15_ = oCount++;
                        overlaps[_loc15_] = curTween;
                     }
                  }
               }
            }
         }
         i = oCount;
         while(--i > -1)
         {
            curTween = overlaps[i];
            if(mode == 2)
            {
               if(curTween._kill(props,target))
               {
                  changed = true;
               }
            }
            if(mode !== 2 || !curTween._firstPT && curTween._initted)
            {
               if(curTween._enabled(false,false))
               {
                  changed = true;
               }
            }
         }
         return changed;
      }
      
      public static function killTweensOf(target:*, onlyActive:* = false, vars:Object = null) : void
      {
         if(typeof onlyActive === "object")
         {
            vars = onlyActive;
            onlyActive = false;
         }
         var a:Array = TweenLite.getTweensOf(target,onlyActive);
         var i:int = int(a.length);
         while(--i > -1)
         {
            a[i]._kill(vars,target);
         }
      }
      
      protected static function _prepVars(vars:Object, immediateRender:Boolean = false) : Object
      {
         if(Boolean(vars._isGSVars))
         {
            vars = vars.vars;
         }
         if(immediateRender && !("immediateRender" in vars))
         {
            vars.immediateRender = true;
         }
         return vars;
      }
      
      public static function delayedCall(delay:Number, callback:Function, params:Array = null, useFrames:Boolean = false) : TweenLite
      {
         return new TweenLite(callback,0,{
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
      
      public static function from(target:Object, duration:Number, vars:Object) : TweenLite
      {
         vars = _prepVars(vars,true);
         vars.runBackwards = true;
         return new TweenLite(target,duration,vars);
      }
      
      public static function killDelayedCallsTo(func:Function) : void
      {
         killTweensOf(func);
      }
      
      public static function set(target:Object, vars:Object) : TweenLite
      {
         return new TweenLite(target,0,vars);
      }
      
      private static function _dumpGarbage(event:Event) : void
      {
         var i:int = 0;
         var a:Array = null;
         var tgt:Object = null;
         if(_rootFrame / 60 >> 0 === _rootFrame / 60)
         {
            for(tgt in _tweenLookup)
            {
               a = _tweenLookup[tgt];
               i = int(a.length);
               while(--i > -1)
               {
                  if(Boolean(a[i]._gc))
                  {
                     a.splice(i,1);
                  }
               }
               if(a.length === 0)
               {
                  delete _tweenLookup[tgt];
               }
            }
         }
      }
      
      public static function to(target:Object, duration:Number, vars:Object) : TweenLite
      {
         return new TweenLite(target,duration,vars);
      }
      
      private static function _checkOverlap(tween:Animation, reference:Number, zeroDur:Boolean) : Number
      {
         var tl:SimpleTimeline = tween._timeline;
         var ts:Number = tl._timeScale;
         var t:Number = tween._startTime;
         var min:Number = 1e-10;
         while(Boolean(tl._timeline))
         {
            t += tl._startTime;
            ts *= tl._timeScale;
            if(tl._paused)
            {
               return -100;
            }
            tl = tl._timeline;
         }
         t /= ts;
         t = t + tween.totalDuration() / tween._timeScale / ts;
         return t > reference ? t - reference : (zeroDur && t == reference || !tween._initted && t - reference < 2 * min ? min : (t > reference + min ? 0 : t - reference - min));
      }
      
      protected function _initProps(target:Object, propLookup:Object, siblings:Array, overwrittenProps:Object) : Boolean
      {
         var p:String = null;
         var i:int = 0;
         var initPlugins:Boolean = false;
         var plugin:Object = null;
         var val:Object = null;
         var vars:Object = this.vars;
         if(target == null)
         {
            return false;
         }
         for(p in vars)
         {
            val = vars[p];
            if(p in _reservedProps)
            {
               if(val is Array)
               {
                  if(val.join("").indexOf("{self}") !== -1)
                  {
                     vars[p] = _swapSelfInParams(val as Array);
                  }
               }
            }
            else if(p in _plugins && Boolean((plugin = new _plugins[p]())._onInitTween(target,val,this)))
            {
               _firstPT = new PropTween(plugin,"setRatio",0,1,p,true,_firstPT,plugin._priority);
               i = int(plugin._overwriteProps.length);
               while(--i > -1)
               {
                  propLookup[plugin._overwriteProps[i]] = _firstPT;
               }
               if(Boolean(plugin._priority) || "_onInitAllProps" in plugin)
               {
                  initPlugins = true;
               }
               if("_onDisable" in plugin || "_onEnable" in plugin)
               {
                  _notifyPluginsOfEnabled = true;
               }
            }
            else
            {
               _firstPT = propLookup[p] = new PropTween(target,p,0,1,p,false,_firstPT);
               _firstPT.s = !_firstPT.f ? Number(target[p]) : Number(target[Boolean(p.indexOf("set")) || !("get" + p.substr(3) in target) ? p : "get" + p.substr(3)]());
               _firstPT.c = typeof val === "number" ? Number(val) - _firstPT.s : (typeof val === "string" && val.charAt(1) === "=" ? int(val.charAt(0) + "1") * Number(val.substr(2)) : Number(val) || 0);
            }
         }
         if(Boolean(overwrittenProps))
         {
            if(_kill(overwrittenProps,target))
            {
               return _initProps(target,propLookup,siblings,overwrittenProps);
            }
         }
         if(_overwrite > 1)
         {
            if(_firstPT != null)
            {
               if(siblings.length > 1)
               {
                  if(_applyOverwrite(target,this,propLookup,_overwrite,siblings))
                  {
                     _kill(propLookup,target);
                     return _initProps(target,propLookup,siblings,overwrittenProps);
                  }
               }
            }
         }
         return initPlugins;
      }
      
      override public function _enabled(enabled:Boolean, ignoreTimeline:Boolean = false) : Boolean
      {
         var i:int = 0;
         if(enabled && _gc)
         {
            if(Boolean(_targets))
            {
               i = int(_targets.length);
               while(--i > -1)
               {
                  _siblings[i] = _register(_targets[i],this,true);
               }
            }
            else
            {
               _siblings = _register(target,this,true);
            }
         }
         super._enabled(enabled,ignoreTimeline);
         if(_notifyPluginsOfEnabled)
         {
            if(_firstPT != null)
            {
               return _onPluginEvent(enabled ? "_onEnable" : "_onDisable",this);
            }
         }
         return false;
      }
      
      override public function render(time:Number, suppressEvents:Boolean = false, force:Boolean = false) : void
      {
         var isComplete:Boolean = false;
         var callback:String = null;
         var pt:PropTween = null;
         var rawPrevTime:Number = NaN;
         var r:Number = NaN;
         var prevTime:Number = _time;
         if(time >= _duration)
         {
            _totalTime = _time = _duration;
            ratio = _ease._calcEnd ? _ease.getRatio(1) : 1;
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
                     if(rawPrevTime > 0 && rawPrevTime !== _tinyNum)
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
            _totalTime = _time = 0;
            ratio = _ease._calcEnd ? _ease.getRatio(0) : 0;
            if(prevTime !== 0 || _duration === 0 && _rawPrevTime > 0 && _rawPrevTime !== _tinyNum)
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
            if(Boolean(_easeType))
            {
               r = time / _duration;
               if(_easeType == 1 || _easeType == 3 && r >= 0.5)
               {
                  r = 1 - r;
               }
               if(_easeType == 3)
               {
                  r *= 2;
               }
               if(_easePower == 1)
               {
                  r *= r;
               }
               else if(_easePower == 2)
               {
                  r *= r * r;
               }
               else if(_easePower == 3)
               {
                  r *= r * r * r;
               }
               else if(_easePower == 4)
               {
                  r *= r * r * r * r;
               }
               if(_easeType == 1)
               {
                  ratio = 1 - r;
               }
               else if(_easeType == 2)
               {
                  ratio = r;
               }
               else if(time / _duration < 0.5)
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
               ratio = _ease.getRatio(time / _duration);
            }
         }
         if(_time == prevTime && !force)
         {
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
         if(prevTime == 0)
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
            if(Boolean(vars.onStart))
            {
               if(_time != 0 || _duration == 0)
               {
                  if(!suppressEvents)
                  {
                     vars.onStart.apply(null,vars.onStartParams);
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
               if(_time !== prevTime || isComplete)
               {
                  _onUpdate.apply(null,vars.onUpdateParams);
               }
            }
         }
         if(Boolean(callback))
         {
            if(!_gc)
            {
               if(time < 0 && _startAt != null && _onUpdate == null && _startTime != 0)
               {
                  _startAt.render(time,suppressEvents,force);
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
               }
               if(_duration === 0 && _rawPrevTime === _tinyNum && rawPrevTime !== _tinyNum)
               {
                  _rawPrevTime = 0;
               }
            }
         }
      }
      
      protected function _init() : void
      {
         var i:int = 0;
         var initPlugins:Boolean = false;
         var pt:PropTween = null;
         var p:String = null;
         var copy:Object = null;
         var immediate:Boolean = Boolean(vars.immediateRender);
         if(Boolean(vars.startAt))
         {
            if(_startAt != null)
            {
               _startAt.render(-1,true);
            }
            vars.startAt.overwrite = 0;
            vars.startAt.immediateRender = true;
            _startAt = new TweenLite(target,0,vars.startAt);
            if(immediate)
            {
               if(_time > 0)
               {
                  _startAt = null;
               }
               else if(_duration !== 0)
               {
                  return;
               }
            }
         }
         else if(Boolean(vars.runBackwards) && _duration !== 0)
         {
            if(_startAt != null)
            {
               _startAt.render(-1,true);
               _startAt = null;
            }
            else
            {
               copy = {};
               for(p in vars)
               {
                  if(!(p in _reservedProps))
                  {
                     copy[p] = vars[p];
                  }
               }
               copy.overwrite = 0;
               copy.data = "isFromStart";
               _startAt = TweenLite.to(target,0,copy);
               if(!immediate)
               {
                  _startAt.render(-1,true);
               }
               else if(_time === 0)
               {
                  return;
               }
            }
         }
         if(vars.ease is Ease)
         {
            _ease = vars.easeParams is Array ? vars.ease.config.apply(vars.ease,vars.easeParams) : vars.ease;
         }
         else if(typeof vars.ease === "function")
         {
            _ease = new Ease(vars.ease,vars.easeParams);
         }
         else
         {
            _ease = defaultEase;
         }
         _easeType = _ease._type;
         _easePower = _ease._power;
         _firstPT = null;
         if(Boolean(_targets))
         {
            i = int(_targets.length);
            while(--i > -1)
            {
               if(_initProps(_targets[i],_propLookup[i] = {},_siblings[i],Boolean(_overwrittenProps) ? _overwrittenProps[i] : null))
               {
                  initPlugins = true;
               }
            }
         }
         else
         {
            initPlugins = _initProps(target,_propLookup,_siblings,_overwrittenProps);
         }
         if(initPlugins)
         {
            _onPluginEvent("_onInitAllProps",this);
         }
         if(Boolean(_overwrittenProps))
         {
            if(_firstPT == null)
            {
               if(typeof target !== "function")
               {
                  _enabled(false,false);
               }
            }
         }
         if(Boolean(vars.runBackwards))
         {
            pt = _firstPT;
            while(Boolean(pt))
            {
               pt.s += pt.c;
               pt.c = -pt.c;
               pt = pt._next;
            }
         }
         _onUpdate = vars.onUpdate;
         _initted = true;
      }
      
      override public function invalidate() : *
      {
         if(_notifyPluginsOfEnabled)
         {
            _onPluginEvent("_onDisable",this);
         }
         _firstPT = null;
         _overwrittenProps = null;
         _onUpdate = null;
         _startAt = null;
         _initted = _active = _notifyPluginsOfEnabled = false;
         _propLookup = Boolean(_targets) ? {} : [];
         return this;
      }
      
      override public function _kill(vars:Object = null, target:Object = null) : Boolean
      {
         var i:int = 0;
         var overwrittenProps:Object = null;
         var p:String = null;
         var pt:PropTween = null;
         var propLookup:Object = null;
         var changed:Boolean = false;
         var killProps:Object = null;
         var record:Boolean = false;
         if(vars === "all")
         {
            vars = null;
         }
         if(vars == null)
         {
            if(target == null || target == this.target)
            {
               return _enabled(false,false);
            }
         }
         target = target || _targets || this.target;
         if(target is Array && typeof target[0] === "object")
         {
            i = int(target.length);
            while(--i > -1)
            {
               if(_kill(vars,target[i]))
               {
                  changed = true;
               }
            }
         }
         else
         {
            if(Boolean(_targets))
            {
               i = int(_targets.length);
               while(--i > -1)
               {
                  if(target === _targets[i])
                  {
                     propLookup = _propLookup[i] || {};
                     _overwrittenProps = _overwrittenProps || [];
                     overwrittenProps = _overwrittenProps[i] = Boolean(vars) ? _overwrittenProps[i] || {} : "all";
                     break;
                  }
               }
            }
            else
            {
               if(target !== this.target)
               {
                  return false;
               }
               propLookup = _propLookup;
               overwrittenProps = _overwrittenProps = Boolean(vars) ? _overwrittenProps || {} : "all";
            }
            if(Boolean(propLookup))
            {
               killProps = vars || propLookup;
               record = vars != overwrittenProps && overwrittenProps != "all" && vars != propLookup && (typeof vars != "object" || vars._tempKill != true);
               for(p in killProps)
               {
                  pt = propLookup[p];
                  if(pt != null)
                  {
                     if(pt.pg && Boolean(pt.t._kill(killProps)))
                     {
                        changed = true;
                     }
                     if(!pt.pg || pt.t._overwriteProps.length === 0)
                     {
                        if(Boolean(pt._prev))
                        {
                           pt._prev._next = pt._next;
                        }
                        else if(pt == _firstPT)
                        {
                           _firstPT = pt._next;
                        }
                        if(Boolean(pt._next))
                        {
                           pt._next._prev = pt._prev;
                        }
                        pt._next = pt._prev = null;
                     }
                     delete propLookup[p];
                  }
                  if(record)
                  {
                     overwrittenProps[p] = 1;
                  }
               }
               if(_firstPT == null && _initted)
               {
                  _enabled(false,false);
               }
            }
         }
         return changed;
      }
   }
}

