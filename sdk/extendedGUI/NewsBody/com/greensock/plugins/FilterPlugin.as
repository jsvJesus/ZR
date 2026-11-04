package com.greensock.plugins
{
   import com.greensock.TweenLite;
   import flash.filters.BitmapFilter;
   import flash.filters.BlurFilter;
   
   public class FilterPlugin extends TweenPlugin
   {
      public static const API:Number = 2;
      
      protected var _remove:Boolean;
      
      private var _tween:TweenLite;
      
      protected var _target:Object;
      
      protected var _index:int;
      
      protected var _filter:BitmapFilter;
      
      protected var _type:Class;
      
      public function FilterPlugin(props:String = "", priority:Number = 0)
      {
         super(props,priority);
      }
      
      protected function _initFilter(target:*, props:Object, tween:TweenLite, type:Class, defaultFilter:BitmapFilter, propNames:Array) : Boolean
      {
         var p:String = null;
         var i:int = 0;
         var colorTween:HexColorsPlugin = null;
         _target = target;
         _tween = tween;
         _type = type;
         var filters:Array = _target.filters;
         var extras:Object = props is BitmapFilter ? {} : props;
         if(extras.index != null)
         {
            _index = extras.index;
         }
         else
         {
            _index = filters.length;
            if(extras.addFilter != true)
            {
               while(--_index > -1 && !(filters[_index] is _type))
               {
               }
            }
         }
         if(_index < 0 || !(filters[_index] is _type))
         {
            if(_index < 0)
            {
               _index = filters.length;
            }
            if(_index > filters.length)
            {
               i = filters.length - 1;
               while(++i < _index)
               {
                  filters[i] = new BlurFilter(0,0,1);
               }
            }
            filters[_index] = defaultFilter;
            _target.filters = filters;
         }
         _filter = filters[_index];
         _remove = extras.remove == true;
         i = int(propNames.length);
         while(--i > -1)
         {
            p = propNames[i];
            if(p in props && _filter[p] != props[p])
            {
               if(p == "color" || p == "highlightColor" || p == "shadowColor")
               {
                  colorTween = new HexColorsPlugin();
                  colorTween._initColor(_filter,p,props[p]);
                  _addTween(colorTween,"setRatio",0,1,_propName);
               }
               else if(p == "quality" || p == "inner" || p == "knockout" || p == "hideObject")
               {
                  _filter[p] = props[p];
               }
               else
               {
                  _addTween(_filter,p,_filter[p],props[p],_propName);
               }
            }
         }
         return true;
      }
      
      override public function setRatio(v:Number) : void
      {
         super.setRatio(v);
         var filters:Array = _target.filters;
         if(!(filters[_index] is _type))
         {
            _index = filters.length;
            while(--_index > -1 && !(filters[_index] is _type))
            {
            }
            if(_index == -1)
            {
               _index = filters.length;
            }
         }
         if(v == 1 && _remove && _tween._time == _tween._duration && _tween.data != "isFromStart")
         {
            if(_index < filters.length)
            {
               filters.splice(_index,1);
            }
         }
         else
         {
            filters[_index] = _filter;
         }
         _target.filters = filters;
      }
   }
}

