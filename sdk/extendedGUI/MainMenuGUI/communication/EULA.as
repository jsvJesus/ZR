package communication
{
   import events.*;
   import flash.events.*;
   
   public class EULA extends EventDispatcher
   {
      private static var _self:EULA;
      
      public static const STATUS_EVENT:String = "eula_status";
      
      public static var Status:int = -1;
      
      Status = -1;
      
      public function EULA(arg1:IEventDispatcher = null)
      {
         super(arg1);
      }
      
      public static function get self() : EULA
      {
         if(!_self)
         {
            _self = new EULA();
         }
         return _self;
      }
      
      public static function requestStatus() : void
      {
         Api.self.addEventListener(Api.GET_EULA_ACCEPTED,onEulaHandler);
         Api.call(Api.GET_EULA_ACCEPTED);
      }
      
      protected static function onEulaHandler(arg1:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.GET_EULA_ACCEPTED,onEulaHandler);
         Status = arg1.data.answer.eula != 1 ? 0 : 1;
         self.dispatchEvent(new Event(STATUS_EVENT));
      }
   }
}

