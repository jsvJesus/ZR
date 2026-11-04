package events
{
   import flash.events.*;
   
   public class ScreenEvent extends Event
   {
      public static const GO_SCREEN:String = "go_screen";
      
      public static const GO_BACK:String = "go_back";
      
      private var _data:String;
      
      public function ScreenEvent(param1:String, param2:String, param3:Boolean = false, param4:Boolean = false)
      {
         this._data = param2;
         super(param1,param3,param4);
      }
      
      public function get data() : String
      {
         return this._data;
      }
   }
}

