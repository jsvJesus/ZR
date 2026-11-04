package communication
{
   import events.*;
   import flash.events.*;
   import logging.*;
   
   public class Gold
   {
      private static var _core:EventDispatcher;
      
      public static const UPDATED:String = "gold_updated";
      
      public static const UPDATED_PLATINUM:String = "platinum_updated";
      
      public static var Value:int = 0;
      
      public static var ValuePlatinum:int = 0;
      
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
            Api.self.addEventListener(Api.UPDATE_PLATINUM,onUpdatePlatinumHandler);
         }
         return _core;
      }
      
      protected static function onUpdateGoldHandler(arg1:ApiEvent) : void
      {
         var loc1:* = null;
         if(arg1.data.answer.new_gold != null)
         {
            Value = arg1.data.answer.new_gold;
            core.dispatchEvent(new Event(UPDATED));
         }
      }
      
      protected static function onUpdatePlatinumHandler(arg1:ApiEvent) : void
      {
         var loc1:* = null;
         if(arg1.data.answer.value != null)
         {
            ValuePlatinum = arg1.data.answer.value;
            core.dispatchEvent(new Event(UPDATED_PLATINUM));
         }
      }
      
      public static function addGoldOpenURL() : *
      {
         Api.call(Api.OPEN_GOLD);
      }
      
      public static function Init() : void
      {
         core;
      }
   }
}

