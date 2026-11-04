package com.dvalimona.utils
{
   public class NumberUtils
   {
      public static const DECIMAL:String = ".";
      
      public function NumberUtils()
      {
         super();
      }
      
      public static function RoundWithPrecision(param1:Number, param2:uint) : String
      {
         return param1.toFixed(param2);
      }
      
      public static function RoundWithPrecisionAndLeads(param1:Number, param2:uint, param3:uint = 0, param4:uint = 0) : String
      {
         return param1.toFixed(3);
      }
   }
}

