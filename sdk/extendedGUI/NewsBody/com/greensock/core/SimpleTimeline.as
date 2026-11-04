package com.greensock.core
{
   public class SimpleTimeline extends Animation
   {
      public var _first:Animation;
      
      public var autoRemoveChildren:Boolean;
      
      public var _last:Animation;
      
      public var smoothChildTiming:Boolean;
      
      public var _sortChildren:Boolean;
      
      public function SimpleTimeline(vars:Object = null)
      {
         super(0,vars);
         this.autoRemoveChildren = this.smoothChildTiming = true;
      }
      
      public function add(child:*, position:* = "+=0", align:String = "normal", stagger:Number = 0) : *
      {
         var st:Number = NaN;
         child._startTime = Number(position || 0) + child._delay;
         if(Boolean(child._paused))
         {
            if(this != child._timeline)
            {
               child._pauseTime = child._startTime + (rawTime() - child._startTime) / child._timeScale;
            }
         }
         if(Boolean(child.timeline))
         {
            child.timeline._remove(child,true);
         }
         child.timeline = child._timeline = this;
         if(Boolean(child._gc))
         {
            child._enabled(true,true);
         }
         var prevTween:Animation = _last;
         if(_sortChildren)
         {
            st = Number(child._startTime);
            while(Boolean(prevTween) && prevTween._startTime > st)
            {
               prevTween = prevTween._prev;
            }
         }
         if(Boolean(prevTween))
         {
            child._next = prevTween._next;
            prevTween._next = Animation(child);
         }
         else
         {
            child._next = _first;
            _first = Animation(child);
         }
         if(Boolean(child._next))
         {
            child._next._prev = child;
         }
         else
         {
            _last = Animation(child);
         }
         child._prev = prevTween;
         if(Boolean(_timeline))
         {
            _uncache(true);
         }
         return this;
      }
      
      public function _remove(tween:Animation, skipDisable:Boolean = false) : *
      {
         if(tween.timeline == this)
         {
            if(!skipDisable)
            {
               tween._enabled(false,true);
            }
            if(Boolean(tween._prev))
            {
               tween._prev._next = tween._next;
            }
            else if(_first === tween)
            {
               _first = tween._next;
            }
            if(Boolean(tween._next))
            {
               tween._next._prev = tween._prev;
            }
            else if(_last === tween)
            {
               _last = tween._prev;
            }
            tween._next = tween._prev = tween.timeline = null;
            if(Boolean(_timeline))
            {
               _uncache(true);
            }
         }
         return this;
      }
      
      public function rawTime() : Number
      {
         return _totalTime;
      }
      
      override public function render(time:Number, suppressEvents:Boolean = false, force:Boolean = false) : void
      {
         var next:Animation = null;
         var tween:Animation = _first;
         _totalTime = _time = _rawPrevTime = time;
         while(Boolean(tween))
         {
            next = tween._next;
            if(tween._active || time >= tween._startTime && !tween._paused)
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
      
      public function insert(child:*, position:* = 0) : *
      {
         return add(child,position || 0);
      }
   }
}

