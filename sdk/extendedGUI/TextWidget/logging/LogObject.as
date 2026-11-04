package logging
{
   import flash.utils.*;
   
   public class LogObject
   {
      private static var SPLITTER:String = " ";
      
      SPLITTER = " ";
      
      private var _arriveTimer:int;
      
      public var date:Date;
      
      private var args:Array;
      
      public var channel:String;
      
      public function LogObject(param1:Array, param2:String = null)
      {
         super();
         this._arriveTimer = getTimer();
         this.args = param1;
         this.date = new Date();
         this.channel = param2 != null ? param2 : Logger.DEFAULT;
      }
      
      public function get timer() : String
      {
         var _loc1_:* = "";
         var _loc2_:* = this._arriveTimer / 1000;
         var _loc3_:* = Math.floor(_loc2_ / 60);
         var _loc4_:* = _loc2_ - _loc3_ * 60;
         var _loc5_:* = this._arriveTimer - _loc2_ * 1000;
         return (_loc3_ < 10 ? "0" + _loc3_ : _loc3_) + ":" + (_loc4_ < 10 ? "0" + _loc4_ : _loc4_) + ":" + (_loc5_ < 100 ? (_loc5_ < 10 ? "00" + _loc5_ : "0" + _loc5_) : _loc5_);
      }
      
      public function toString() : String
      {
         var _loc1_:* = new String();
         var _loc2_:* = 0;
         while(_loc2_ < this.args.length)
         {
            if(this.args[_loc2_] != null)
            {
               _loc1_ += this.args[_loc2_].toString() + SPLITTER;
            }
            _loc2_++;
         }
         return _loc1_;
      }
   }
}

