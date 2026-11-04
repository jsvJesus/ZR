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
         this.answer = response.answer;
         this.name = response.name;
         this.error = response.error;
      }
   }
}

