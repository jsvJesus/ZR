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
      
      public function LogObject(arg1:Array, arg2:String = null)
      {
         super();
         this._arriveTimer = getTimer();
         this.args = arg1;
         this.date = new Date();
         this.channel = arg2 != null ? arg2 : Logger.DEFAULT;
      }
      
      public function get timer() : String
      {
         var loc1:* = "";
         var loc2:* = this._arriveTimer / 1000;
         var loc3:* = Math.floor(loc2 / 60);
         var loc4:* = loc2 - loc3 * 60;
         var loc5:* = this._arriveTimer - loc2 * 1000;
         return (loc3 < 10 ? "0" + loc3 : loc3) + ":" + (loc4 < 10 ? "0" + loc4 : loc4) + ":" + (loc5 < 100 ? (loc5 < 10 ? "00" + loc5 : "0" + loc5) : loc5);
      }
      
      public function toString() : String
      {
         var loc1:* = new String();
         var loc2:* = 0;
         while(loc2 < this.args.length)
         {
            if(this.args[loc2] != null)
            {
               loc1 += this.args[loc2].toString() + SPLITTER;
            }
            loc2++;
         }
         return loc1;
      }
   }
}

