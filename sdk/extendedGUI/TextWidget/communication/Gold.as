package communication
{
   import events.*;
   import flash.events.*;
   import logging.*;
   
   public class Gold
   {
      private static var _core:EventDispatcher;
      
      public static const UPDATED:String = "gold_updated";
      
      public static var Value:int = 0;
      
      Value = 0;
      
      public function Gold()
      {
         super();
      }
      
      public static function get core() : EventDispatcher
      {
         if(!_core)
         {
            _core = new EventDispatcher();
            Api.self.addEventListener(Api.UPDATE_GOLD,onUpdateGoldHandler);
         }
         return _core;
      }
      
      protected static function onUpdateGoldHandler(param1:ApiEvent) : void
      {
         var _loc2_:* = null;
         var _loc3_:* = 0;
         if(param1.data.answer.new_gold != null)
         {
            _loc3_ = 0;
            while(_loc3_ < Character.list.length)
            {
               Logger.LogToChannel(Logger.DEBUG,"Gold.Update",param1.data.answer.new_gold);
               Value = param1.data.answer.new_gold;
               _loc3_++;
            }
            core.dispatchEvent(new Event(UPDATED));
         }
      }
      
      public static function Init() : void
      {
         core;
      }
   }
}

