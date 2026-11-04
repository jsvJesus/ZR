package com
{
   public class StringParse
   {
      public function StringParse()
      {
         super();
      }
      
      public static function findIn(param1:String, param2:String) : Boolean
      {
         var _loc3_:* = 0;
         while(_loc3_ < param1.length)
         {
            if(param1.charAt(_loc3_).toUpperCase() != param2.charAt(_loc3_).toUpperCase())
            {
               return false;
            }
            _loc3_++;
         }
         return true;
      }
      
      public static function lastIndexOf(param1:String, param2:String) : *
      {
         var _loc3_:int = 0;
         var _loc4_:int = -1;
         while(_loc3_ != -1)
         {
            _loc3_ = int(param2.indexOf(param1,_loc3_));
            if(_loc3_ != -1)
            {
               _loc4_ = _loc3_;
               _loc3_ += param1.length;
            }
         }
         return _loc4_;
      }
      
      public function replaceEnter(param1:String) : String
      {
         var _loc2_:String = "";
         var _loc3_:* = 0;
         while(_loc3_ < param1.length)
         {
            if(param1.charAt(_loc3_) != "\r")
            {
               _loc2_ += param1.charAt(_loc3_);
            }
            _loc3_++;
         }
         return _loc2_;
      }
      
      public function deleteSpaces(param1:String) : String
      {
         var _loc2_:Boolean = false;
         var _loc3_:String = "";
         var _loc4_:String = "";
         var _loc5_:* = 0;
         while(_loc5_ < param1.length)
         {
            if(param1.charAt(_loc5_) != " " && !_loc2_)
            {
               _loc2_ = true;
               _loc3_ = param1.charAt(_loc5_);
            }
            else if(_loc2_)
            {
               _loc3_ += param1.charAt(_loc5_);
            }
            _loc5_++;
         }
         _loc2_ = false;
         _loc5_ = _loc3_.length - 1;
         while(_loc5_ >= 0)
         {
            if(_loc3_.charAt(_loc5_) != " " && !_loc2_)
            {
               _loc2_ = true;
               _loc4_ = _loc3_.charAt(_loc5_);
            }
            else if(_loc2_)
            {
               _loc4_ = _loc3_.charAt(_loc5_) + _loc4_;
            }
            _loc5_--;
         }
         return _loc4_;
      }
      
      public function validateMessage(param1:String) : Boolean
      {
         var _loc2_:Boolean = false;
         var _loc3_:* = 0;
         while(_loc3_ < param1.length)
         {
            if(param1.charAt(_loc3_) != " " && param1.charAt(_loc3_) != "\r")
            {
               _loc2_ = true;
            }
            _loc3_++;
         }
         return _loc2_;
      }
   }
}

