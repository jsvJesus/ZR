package communication
{
   public class ApiResponse
   {
      public var answer:Object;
      
      public var name:String;
      
      public var error:Object;
      
      public function ApiResponse(response:Object)
      {
         super();
         this.answer = response.data;
         this.name = response.event_name;
         this.error = response.error;
      }
   }
}

