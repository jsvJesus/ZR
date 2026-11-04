package com.communication
{
   public class Localization
   {
      protected static var locale:Object = new Object();
      
      public function Localization()
      {
         super();
      }
      
      public static function getLocal(param1:String) : String
      {
         var _loc4_:String = null;
         var _loc5_:* = undefined;
         var _loc6_:String = null;
         var _loc7_:* = undefined;
         var _loc8_:* = undefined;
         var _loc2_:Array = param1.split(".");
         var _loc3_:Object = locale;
         for each(_loc5_ in _loc2_)
         {
            if(_loc3_ is String)
            {
               return param1;
            }
            _loc8_ = _loc3_[_loc5_];
            if(_loc8_ is String)
            {
               _loc4_ = _loc8_;
            }
            else
            {
               if(!(_loc8_ is Object))
               {
                  return param1;
               }
               _loc3_ = _loc8_;
            }
         }
         _loc6_ = "";
         for each(_loc7_ in _loc4_.split("\r"))
         {
            _loc6_ += _loc7_;
         }
         return _loc6_ || param1;
      }
      
      public static function setLocalData(param1:Object) : *
      {
         locale = param1;
      }
   }
}

