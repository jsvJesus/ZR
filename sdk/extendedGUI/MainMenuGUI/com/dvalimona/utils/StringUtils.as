package com.dvalimona.utils
{
   public class StringUtils
   {
      public function StringUtils()
      {
         super();
      }
      
      public static function dropSpaces(arg1:String, arg2:String = "") : String
      {
         return arg1.split(" ").join(arg2);
      }
      
      public static function drop_RN(arg1:String) : String
      {
         var loc1:* = "";
         var loc2:* = 0;
         while(loc2 < arg1.length)
         {
            if(arg1.charCodeAt(loc2) != 10)
            {
               if(arg1.charCodeAt(loc2) != 13)
               {
                  loc1 += arg1.charAt(loc2);
               }
            }
            loc2++;
         }
         return loc1;
      }
      
      public static function validateString(arg1:String) : Boolean
      {
         for(var i:* = 0; i < arg1.length; i++)
         {
            if(arg1.charCodeAt(i) > 126 || arg1.charCodeAt(i) < 33)
            {
               return false;
            }
         }
         return true;
      }
      
      public static function validateEmail(arg1:String) : Boolean
      {
         var find_at:Boolean = false;
         for(var i:* = 0; i < arg1.length; i++)
         {
            if(arg1.charCodeAt(i) == 64)
            {
               find_at = true;
            }
            if(arg1.charCodeAt(i) > 126 || arg1.charCodeAt(i) < 34)
            {
               return false;
            }
         }
         return find_at;
      }
   }
}

