package communication
{
   public class ApiResponse
   {
      public var answer:Object;
      
      public var name:String;
      
      public var error:Object;
      
      public function ApiResponse(param1:Object)
      {
         super();
         this.name = param1.event_name;
         this.answer = param1.data;
         this.error = null;
      }
   }
}

