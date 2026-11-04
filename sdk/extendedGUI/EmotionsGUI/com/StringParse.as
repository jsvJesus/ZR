package com
{
   public class StringParse
   {
      public function StringParse()
      {
         super();
      }
      
      public static function findIn(str1:String, str2:String) : Boolean
      {
         for(var i:* = 0; i < str1.length; i++)
         {
            if(str1.charAt(i).toUpperCase() != str2.charAt(i).toUpperCase())
            {
               return false;
            }
         }
         return true;
      }
      
      public function replaceEnter(str:String) : String
      {
         var tmp:String = "";
         for(var i:* = 0; i < str.length; i++)
         {
            if(str.charAt(i) != "\r")
            {
               tmp += str.charAt(i);
            }
         }
         return tmp;
      }
      
      public function deleteSpaces(str:String) : String
      {
         var firstSymb:Boolean = false;
         var tmp:String = "";
         var tmp1:String = "";
         for(var i:* = 0; i < str.length; i++)
         {
            if(str.charAt(i) != " " && !firstSymb)
            {
               firstSymb = true;
               tmp = str.charAt(i);
            }
            else if(firstSymb)
            {
               tmp += str.charAt(i);
            }
         }
         firstSymb = false;
         for(i = tmp.length - 1; i >= 0; i--)
         {
            if(tmp.charAt(i) != " " && !firstSymb)
            {
               firstSymb = true;
               tmp1 = tmp.charAt(i);
            }
            else if(firstSymb)
            {
               tmp1 = tmp.charAt(i) + tmp1;
            }
         }
         return tmp1;
      }
      
      public function validateMessage(str:String) : Boolean
      {
         var tmp:Boolean = false;
         for(var i:* = 0; i < str.length; i++)
         {
            if(str.charAt(i) != " " && str.charAt(i) != "\r")
            {
               tmp = true;
            }
         }
         return tmp;
      }
   }
}

