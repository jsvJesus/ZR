package com.dvalimona.utils
{
   public class StringUtils
   {
      public function StringUtils()
      {
         super();
      }
      
      public static function dropSpaces(string:String, symbol:String = "") : String
      {
         return string.split(" ").join(symbol);
      }
      
      public static function drop_RN(str:String) : String
      {
         var newStr:String = "";
         for(var i:uint = 0; i < str.length; i++)
         {
            trace(i,str.charAt(i),str.charCodeAt(i));
            if(str.charCodeAt(i) != 10)
            {
               if(str.charCodeAt(i) != 13)
               {
                  newStr += str.charAt(i);
               }
            }
         }
         return newStr;
      }
   }
}

