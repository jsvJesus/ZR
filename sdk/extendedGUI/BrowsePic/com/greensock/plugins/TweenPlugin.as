package com.greensock.plugins
{
   import com.greensock.TweenLite;
   import com.greensock.core.PropTween;
   
   public class TweenPlugin
   {
      public static const version:String = "12.1.5";
      
      public static const API:Number = 2;
      
      public var _priority:int = 0;
      
      public var _overwriteProps:Array;
      
      public var _propName:String;
      
      protected var _firstPT:PropTween;
      
      public function TweenPlugin(props:String = "", priority:int = 0)
      {
         super();
         _overwriteProps = props.split(",");
         _propName = _overwriteProps[0];
         _priority = priority || 0;
      }
      
      public static function activate(plugins:Array) : Boolean
      {
         TweenLite._onPluginEvent = TweenPlugin._onTweenEvent;
         var i:int = int(plugins.length);
         while(--i > -1)
         {
            if(plugins[i].API == TweenPlugin.API)
            {
               TweenLite._plugins[new (plugins[i] as Class)()._propName] = plugins[i];
            }
         }
         return true;
      }
      
      private static function _onTweenEvent(type:String, tween:TweenLite) : Boolean
      {
         var changed:Boolean = false;
         var pt2:PropTween = null;
         var first:PropTween = null;
         var last:PropTween = null;
         var next:PropTween = null;
         var pt:PropTween = tween._firstPT;
         if(type == "_onInitAllProps")
         {
            while(Boolean(pt))
            {
               next = pt._next;
               pt2 = first;
               while(Boolean(pt2) && pt2.pr > pt.pr)
               {
                  pt2 = pt2._next;
               }
               if(Boolean(pt._prev = Boolean(pt2) ? pt2._prev : last))
               {
                  pt._prev._next = pt;
               }
               else
               {
                  first = pt;
               }
               if(Boolean(pt._next = pt2))
               {
                  pt2._prev = pt;
               }
               else
               {
                  last = pt;
               }
               pt = next;
            }
            pt = tween._firstPT = first;
         }
         while(Boolean(pt))
         {
            if(pt.pg)
            {
               if(type in pt.t)
               {
                  if(Boolean(pt.t[type]()))
                  {
                     changed = true;
                  }
               }
            }
            pt = pt._next;
         }
         return changed;
      }
      
      public function _roundProps(lookup:Object, value:Boolean = true) : void
      {
         var pt:PropTween = _firstPT;
         while(Boolean(pt))
         {
            if(_propName in lookup || pt.n != null && pt.n.split(_propName + "_").join("") in lookup)
            {
               pt.r = value;
            }
            pt = pt._next;
         }
      }
      
      public function setRatio(v:Number) : void
      {
         var val:Number = NaN;
         var pt:PropTween = _firstPT;
         while(Boolean(pt))
         {
            val = pt.c * v + pt.s;
            if(pt.r)
            {
               val = val + (val > 0 ? 0.5 : -0.5) | 0;
            }
            if(pt.f)
            {
               pt.t[pt.p](val);
            }
            else
            {
               pt.t[pt.p] = val;
            }
            pt = pt._next;
         }
      }
      
      public function _kill(lookup:Object) : Boolean
      {
         var i:int = 0;
         if(_propName in lookup)
         {
            _overwriteProps = [];
         }
         else
         {
            i = int(_overwriteProps.length);
            while(--i > -1)
            {
               if(_overwriteProps[i] in lookup)
               {
                  _overwriteProps.splice(i,1);
               }
            }
         }
         var pt:PropTween = _firstPT;
         while(Boolean(pt))
         {
            if(pt.n in lookup)
            {
               if(Boolean(pt._next))
               {
                  pt._next._prev = pt._prev;
               }
               if(Boolean(pt._prev))
               {
                  pt._prev._next = pt._next;
                  pt._prev = null;
               }
               else if(_firstPT == pt)
               {
                  _firstPT = pt._next;
               }
            }
            pt = pt._next;
         }
         return false;
      }
      
      protected function _addTween(target:Object, propName:String, start:Number, end:*, overwriteProp:String = null, round:Boolean = false) : PropTween
      {
         var c:Number = end == null ? 0 : (typeof end === "number" || end.charAt(1) !== "=" ? Number(end) - start : int(end.charAt(0) + "1") * Number(end.substr(2)));
         if(c !== 0)
         {
            _firstPT = new PropTween(target,propName,start,c,overwriteProp || propName,false,_firstPT);
            _firstPT.r = round;
            return _firstPT;
         }
         return null;
      }
      
      public function _onInitTween(target:Object, value:*, tween:TweenLite) : Boolean
      {
         return false;
      }
   }
}

