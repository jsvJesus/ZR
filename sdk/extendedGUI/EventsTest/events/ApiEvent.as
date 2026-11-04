package events
{
   import communication.ApiResponse;
   import flash.events.Event;
   
   public class ApiEvent extends Event
   {
      private var _data:ApiResponse;
      
      public function ApiEvent(response:ApiResponse)
      {
         this._data = response;
         super(this.data.name,bubbles,cancelable);
      }
      
      public function get data() : ApiResponse
      {
         return this._data;
      }
   }
}

