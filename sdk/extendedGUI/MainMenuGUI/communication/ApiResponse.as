package communication
{
   public class ApiResponse
   {
      public var answer:Object;
      
      public var name:String;
      
      public var error:Object;
      
      public function ApiResponse(arg1:Object)
      {
         super();
         this.name = arg1.event_name;
         this.answer = arg1.data;
         this.error = null;
      }
   }
}

