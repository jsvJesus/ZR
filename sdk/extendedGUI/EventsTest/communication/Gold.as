package communication
{
   import events.ApiEvent;
   import flash.events.Event;
   import flash.events.EventDispatcher;
   import logging.Logger;
   
   public class Gold
   {
      private static var _core:EventDispatcher;
      
      public static const UPDATED:String = "gold_updated";
      
      public static var Value:int = 0;
      
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
      
      protected static function onUpdateGoldHandler(event:ApiEvent) : void
      {
         var char:Character = null;
         var i:uint = 0;
         if(event.data.answer.new_gold != null)
         {
            for(i = 0; i < Character.list.length; i++)
            {
               Logger.LogToChannel(Logger.DEBUG,"Gold.Update",event.data.answer.new_gold);
               Value = event.data.answer.new_gold;
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

