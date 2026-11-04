package events
{
   import flash.events.*;
   
   public class ScreenEvent extends Event
   {
      public static const GO_SCREEN:String = "go_screen";
      
      public static const GO_BACK:String = "go_back";
      
      private var _data:String;
      
      public function ScreenEvent(arg1:String, arg2:String, arg3:Boolean = false, arg4:Boolean = false)
      {
         this._data = arg2;
         super(arg1,arg3,arg4);
      }
      
      public function get data() : String
      {
         return this._data;
      }
   }
}

