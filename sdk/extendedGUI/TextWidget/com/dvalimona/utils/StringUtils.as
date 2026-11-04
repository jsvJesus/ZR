package com.dvalimona.utils
{
   public class StringUtils
   {
      public function StringUtils()
      {
         super();
      }
      
      public static function dropSpaces(param1:String, param2:String = "") : String
      {
         return param1.split(" ").join(param2);
      }
      
      public static function drop_RN(param1:String) : String
      {
         var _loc2_:* = "";
         var _loc3_:* = 0;
         while(_loc3_ < param1.length)
         {
            trace(_loc3_,param1.charAt(_loc3_),param1.charCodeAt(_loc3_));
            if(param1.charCodeAt(_loc3_) != 10)
            {
               if(param1.charCodeAt(_loc3_) != 13)
               {
                  _loc2_ += param1.charAt(_loc3_);
               }
            }
            _loc3_++;
         }
         return _loc2_;
      }
      
      public static function validateString(param1:String) : Boolean
      {
         var _loc2_:* = 0;
         while(_loc2_ < param1.length)
         {
            if(param1.charCodeAt(_loc2_) > 126 || param1.charCodeAt(_loc2_) < 34)
            {
               return false;
            }
            _loc2_++;
         }
         return true;
      }
   }
}

