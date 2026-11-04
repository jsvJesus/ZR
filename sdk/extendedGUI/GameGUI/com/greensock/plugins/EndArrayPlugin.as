package com.greensock.plugins
{
   import com.greensock.TweenLite;
   
   public class EndArrayPlugin extends TweenPlugin
   {
      public static const API:Number = 2;
      
      protected var _a:Array;
      
      protected var _info:Array = [];
      
      protected var _round:Boolean;
      
      public function EndArrayPlugin()
      {
         super("endArray");
      }
      
      override public function _roundProps(lookup:Object, value:Boolean = true) : void
      {
         if("endArray" in lookup)
         {
            _round = value;
         }
      }
      
      public function _init(start:Array, end:Array) : void
      {
         _a = start;
         var i:int = int(end.length);
         var cnt:int = 0;
         while(--i > -1)
         {
            if(start[i] != end[i] && start[i] != null)
            {
               var _loc5_:* = cnt++;
               _info[_loc5_] = new ArrayTweenInfo(i,_a[i],end[i] - _a[i]);
            }
         }
      }
      
      override public function setRatio(v:Number) : void
      {
         var ti:ArrayTweenInfo = null;
         var val:Number = NaN;
         var i:int = int(_info.length);
         if(_round)
         {
            while(--i > -1)
            {
               ti = _info[i];
               _a[ti.i] = (val = ti.c * v + ti.s) > 0 ? val + 0.5 >> 0 : val - 0.5 >> 0;
            }
         }
         else
         {
            while(--i > -1)
            {
               ti = _info[i];
               _a[ti.i] = ti.c * v + ti.s;
            }
         }
      }
      
      override public function _onInitTween(target:Object, value:*, tween:TweenLite) : Boolean
      {
         if(!(target is Array) || !(value is Array))
         {
            return false;
         }
         _init(target as Array,value);
         return true;
      }
   }
}

class ArrayTweenInfo
{
   public var s:Number;
   
   public var i:uint;
   
   public var c:Number;
   
   public function ArrayTweenInfo(index:uint, start:Number, change:Number)
   {
      super();
      this.i = index;
      this.s = start;
      this.c = change;
   }
}
