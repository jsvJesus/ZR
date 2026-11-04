package events
{
   import communication.*;
   import flash.events.*;
   
   public class ApiEvent extends Event
   {
      private var _data:ApiResponse;
      
      public function ApiEvent(arg1:ApiResponse)
      {
         this._data = arg1;
         super(this.data.name,bubbles,cancelable);
      }
      
      public function get data() : ApiResponse
      {
         return this._data;
      }
   }
}

