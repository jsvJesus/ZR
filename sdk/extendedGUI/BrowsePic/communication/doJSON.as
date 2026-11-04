package communication
{
   import com.adobe.serialization.json.JSONDecoder;
   import com.brokenfunction.json.encodeJson;
   
   public class doJSON
   {
      public function doJSON()
      {
         super();
      }
      
      public static function encode(object:Object) : String
      {
         return encodeJson(object);
      }
      
      public static function decode(json:String) : Object
      {
         return new JSONDecoder(json,false).getValue();
      }
   }
}

